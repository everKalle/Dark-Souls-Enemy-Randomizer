using EnemyRandomizer.App.Services;
using EnemyRandomizer.App.ViewModels;
using EnemyRandomizer.App.ViewModels.Options;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnemyRandomizer.App.Tests;

public class MainViewModelTests
{
    private readonly FakeDialogs _dialogs = new();
    private readonly FakeLastConfigStore _store = new();
    private readonly FakeEnemyConfigs _configs = new();
    private readonly FakeRandomizer _randomizer = new();
    private readonly FakeShell _shell = new();
    private readonly FakePreferences _preferences = new();

    private MainViewModel Create(FakeInstallationStatus? status = null) => new(
        ShippedConfig.Get<AppSettings>(),
        ShippedConfig.Get<RandomizerOptionSettings>(),
        ShippedConfig.Get<MessageSettings>(),
        new OptionViewModelFactory(),
        _store,
        _configs,
        _randomizer,
        _dialogs,
        new FakePaths(),
        status ?? FakeInstallationStatus.Ok(),
        _preferences,
        _shell,
        NullLogger<MainViewModel>.Instance);

    private async Task<MainViewModel> Started(FakeInstallationStatus? status = null)
    {
        var vm = Create(status);
        await vm.InitializeAsync();
        return vm;
    }

    [Fact]
    public void Commands_are_disabled_until_startup_checks_finished() =>
        Assert.False(Create().RandomizeCommand.CanExecute(null));

    [Fact]
    public async Task Startup_problems_are_shown_and_block_running()
    {
        var report = new InstallationReport(GameEdition.Ptde, ExeState.NotUnpacked,
        [
            new InstallationProblem(MessageKeys.ExeNotUnpacked),
            new InstallationProblem(MessageKeys.FilesMissingInFolder, 2, 18, ".msb", "map/MapStudio"),
        ]);

        var vm = await Started(new FakeInstallationStatus(report));

        Assert.True(vm.HasStartupErrors);
        Assert.Contains("UDSFM", vm.StartupErrors[0], StringComparison.Ordinal);
        Assert.Equal("2/18 .msb files missing from map/MapStudio/", vm.StartupErrors[1]);
        Assert.False(vm.RandomizeCommand.CanExecute(null));
        Assert.False(vm.RevertCommand.CanExecute(null));
        Assert.False(vm.CopyPlacementCommand.CanExecute(null));
    }

    [Fact]
    public async Task Unknown_exe_warns_once_but_allows_running()
    {
        var vm = await Started(FakeInstallationStatus.Ok(ExeState.Unknown));

        Assert.Single(_dialogs.Warnings);
        Assert.True(vm.RandomizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Effect_write_failure_is_a_warning_only()
    {
        var report = new InstallationReport(GameEdition.Remastered, ExeState.Remastered, [])
        {
            Warnings = [new InstallationProblem(MessageKeys.EffectsWriteFailedText, ".dcx")],
        };

        var vm = await Started(new FakeInstallationStatus(report));

        Assert.Contains("CommonEffects.ffxbnd.dcx", Assert.Single(_dialogs.Warnings), StringComparison.Ordinal);
        Assert.True(vm.RandomizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Failed_copies_ask_to_retry_until_they_succeed()
    {
        var failed = new InstallationReport(GameEdition.Ptde, ExeState.Patched, [new InstallationProblem(MessageKeys.CopiesMissing, 1, 18, ".msb", "x")])
        {
            CopiesFailed = true,
        };
        var status = new FakeInstallationStatus(failed, failed, new InstallationReport(GameEdition.Ptde, ExeState.Patched, []));

        var vm = await Started(status);

        Assert.Equal(3, status.Calls);
        Assert.False(vm.HasStartupErrors);
        Assert.True(vm.RandomizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Declining_the_retry_keeps_the_copy_problem()
    {
        var failed = new InstallationReport(GameEdition.Ptde, ExeState.Patched, [new InstallationProblem(MessageKeys.CopiesMissing, 1, 18, ".msb", "x")])
        {
            CopiesFailed = true,
        };
        _dialogs.ConfirmAnswer = false;

        var vm = await Started(new FakeInstallationStatus(failed));

        Assert.True(vm.HasStartupErrors);
        Assert.False(vm.RandomizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Title_shows_current_mode() =>
        Assert.EndsWith("[Current Mode: REMASTERED]", (await Started(FakeInstallationStatus.Ok(ExeState.Remastered))).Title, StringComparison.Ordinal);

    private static OptionViewModel Option(MainViewModel vm, string key) =>
        vm.Tabs.SelectMany(t => t.Options).Single(o => o.Key == key);

    [Fact]
    public async Task Starts_with_defaults_when_nothing_saved()
    {
        var vm = await Started();

        Assert.Equal(3, vm.Tabs.Count);
        Assert.Equal(21, vm.Tabs.Sum(t => t.Options.Count));
        Assert.Equal(1, vm.Current["BossMode"]);
        Assert.Equal(3, vm.Current["DifficultyMode"]);
        Assert.Contains("PTDE", vm.EditionText, StringComparison.Ordinal);
    }

    [Fact]
    public void Starts_with_last_saved_settings()
    {
        _store.Saved = RandomizerSettings.CreateDefault(ShippedConfig.Get<RandomizerOptionSettings>().Value) with { Seed = "abc" };
        Assert.Equal("abc", Create().Seed);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(1, false)]
    public void Boss_chance_for_bosses_needs_boss_mode_3(int bossMode, bool enabled)
    {
        var vm = Create();
        Option(vm, "BossMode").Value = bossMode;

        Assert.Equal(enabled, Option(vm, "BossChanceBosses").IsEnabled);
        Assert.Equal(enabled, !Option(vm, "BossChanceBosses").DisplayTitle.StartsWith("[No effect]", StringComparison.Ordinal));
    }

    [Fact]
    public void Boss_chance_for_normals_is_enabled_by_normal_or_npc_mode()
    {
        var vm = Create();
        Option(vm, "NormalMode").Value = 2;
        Option(vm, "NpcMode").Value = 0;
        Assert.False(Option(vm, "BossChanceNormals").IsEnabled);

        Option(vm, "NpcMode").Value = 3;
        Assert.True(Option(vm, "BossChanceNormals").IsEnabled);
    }

    [Fact]
    public void Restore_defaults_keeps_enemy_config()
    {
        _configs.Names.Add("Mine");
        var vm = Create();
        vm.SelectedEnemyConfig = "Mine";
        Option(vm, "BossMode").Value = 0;

        vm.RestoreDefaultsCommand.Execute(null);

        Assert.Equal(1, vm.Current["BossMode"]);
        Assert.Equal("Mine", vm.SelectedEnemyConfig);
    }

    [Fact]
    public async Task Randomize_saves_settings_and_reports_result()
    {
        var vm = await Started();
        vm.Seed = " seed ";

        await vm.RandomizeCommand.ExecuteAsync(null);

        Assert.Equal("seed", _store.Saved!.Seed);
        Assert.Equal("seed", _randomizer.LastSettings!.Seed);
        Assert.Equal(NoticeKind.Success, vm.Notice!.Kind);
        Assert.Equal("done", vm.Notice.Message);
        Assert.Null(vm.ActiveProgress);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Failed_run_shows_an_error_notice_with_the_log_folder()
    {
        var vm = await Started();
        _randomizer.RandomizeOverride = _ => throw new InvalidOperationException("boom");

        await vm.RandomizeCommand.ExecuteAsync(null);

        Assert.Equal(NoticeKind.Error, vm.Notice!.Kind);
        Assert.Equal("boom", vm.Notice.Message);
        Assert.Equal("C:/game/data/logs", vm.Notice.FolderPath);
        Assert.False(vm.IsBusy);
        Assert.True(vm.RandomizeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancelling_a_run_shows_a_warning_and_unlocks_the_window()
    {
        var vm = await Started();
        var started = new TaskCompletionSource();
        _randomizer.RandomizeOverride = async ct =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return OperationResult.Ok("x", "x");
        };

        var run = vm.RandomizeCommand.ExecuteAsync(null);
        await started.Task;

        Assert.True(vm.IsBusy);
        Assert.True(vm.ActiveProgress!.CanCancel);
        Assert.False(vm.RandomizeCommand.CanExecute(null)); // no second run while busy
        vm.ActiveProgress.CancelCommand.Execute(null);
        await run;

        Assert.Equal(NoticeKind.Warning, vm.Notice!.Kind);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.ActiveProgress);
    }

    [Fact]
    public async Task Revert_is_not_cancellable_and_can_be_aborted_at_the_question()
    {
        var vm = await Started();

        _dialogs.ConfirmOrCancelAnswer = null;
        await vm.RevertCommand.ExecuteAsync(null);
        Assert.Null(vm.Notice);

        _dialogs.ConfirmOrCancelAnswer = false;
        await vm.RevertCommand.ExecuteAsync(null);
        Assert.Equal("done", vm.Notice!.Message);
    }

    [Fact]
    public void Corrupt_settings_do_not_break_saving()
    {
        var vm = Create();
        _store.ThrowOnSave = true;

        vm.Persist(); // must not throw

        Assert.Null(_store.Saved);
    }

    [Fact]
    public void Persist_saves_the_current_settings()
    {
        var vm = Create();
        vm.Seed = "abc";

        vm.Persist();

        Assert.Equal("abc", _store.Saved!.Seed);
    }

    [Fact]
    public async Task Choosing_a_game_folder_stores_it_and_restarts()
    {
        var vm = await Started();
        _shell.PickedFolder = "D:/DS";

        vm.ChooseGameFolderCommand.Execute(null);

        Assert.Equal("D:/DS", _preferences.Current.GameDirectory);
        Assert.Equal(1, _shell.Restarts);
    }

    [Fact]
    public async Task Cancelling_the_folder_picker_changes_nothing()
    {
        var vm = await Started();
        _shell.PickedFolder = null;

        vm.ChooseGameFolderCommand.Execute(null);

        Assert.Equal("", _preferences.Current.GameDirectory);
        Assert.Equal(0, _shell.Restarts);
    }

    [Fact]
    public async Task Declining_the_restart_keeps_the_old_folder()
    {
        var vm = await Started();
        _shell.PickedFolder = "D:/DS";
        _dialogs.ConfirmAnswer = false;

        vm.ChooseGameFolderCommand.Execute(null);

        Assert.Equal("", _preferences.Current.GameDirectory);
        Assert.Equal(0, _shell.Restarts);
    }

    [Fact]
    public void Theme_choice_is_applied_and_remembered()
    {
        var vm = Create();

        vm.SetThemeCommand.Execute(AppTheme.Dark);

        Assert.Equal(AppTheme.Dark, _shell.Theme);
        Assert.Equal(AppTheme.Dark, _preferences.Current.Theme);
    }

    [Fact]
    public async Task Notice_actions_open_the_folder_copy_details_and_dismiss()
    {
        var vm = await Started();
        await vm.RandomizeCommand.ExecuteAsync(null);

        vm.OpenNoticeFolderCommand.Execute(null);
        vm.CopyNoticeDetailsCommand.Execute(null);
        Assert.Equal(["C:/game/data/refs"], _shell.Opened);
        Assert.Contains("done", Assert.Single(_shell.Copied), StringComparison.Ordinal);

        vm.DismissNoticeCommand.Execute(null);
        Assert.Null(vm.Notice);
    }

    [Fact]
    public void New_enemy_config_appears_after_save()
    {
        var vm = Create();
        _configs.Save("Fresh", [true, false]);
        Assert.Contains("Fresh", vm.EnemyConfigNames);
    }
}

public class OptionTabViewModelTests
{
    private static (OptionTabViewModel Tab, ChoiceOptionViewModel Boss, SliderOptionViewModel Chance) Create()
    {
        var options = ShippedConfig.Get<RandomizerOptionSettings>().Value;
        var factory = new OptionViewModelFactory();
        var boss = (ChoiceOptionViewModel)factory.Create(options.Find("BossMode")!);
        var chance = (SliderOptionViewModel)factory.Create(options.Find("ReplaceChance")!);
        return (new OptionTabViewModel("Main", [boss, chance]), boss, chance);
    }

    [Fact]
    public void Hovering_other_value_previews_its_description()
    {
        var (_, boss, chance) = Create();
        var other = boss.Choices.First(c => !c.IsSelected);

        boss.Hover(other);

        Assert.Equal(DescriptionState.Preview, boss.State);
        Assert.Equal(DescriptionState.Faded, chance.State);
        Assert.Equal(other.Description, boss.DisplayedDescription);
    }

    [Fact]
    public void Selecting_the_previewed_value_turns_into_hovered()
    {
        var (_, boss, _) = Create();
        var other = boss.Choices.First(c => !c.IsSelected);
        boss.Hover(other);

        other.IsSelected = true;

        Assert.Equal(DescriptionState.Hovered, boss.State);
        Assert.Single(boss.Choices, c => c.IsSelected);
    }

    [Fact]
    public void Leaving_resets_all_states()
    {
        var (_, boss, chance) = Create();
        chance.Hover();
        chance.ClearHover();

        Assert.Equal(DescriptionState.Normal, boss.State);
        Assert.Equal(DescriptionState.Normal, chance.State);
        Assert.Equal(boss.Description, boss.DisplayedDescription);
    }
}

public class TextConfigViewModelTests
{
    private sealed class Host(RandomizerSettings settings) : ISettingsHost
    {
        public RandomizerSettings Current { get; private set; } = settings;
        public void Apply(RandomizerSettings settings) => Current = settings;
    }

    [Fact]
    public void Generate_produces_legacy_default_string()
    {
        var options = ShippedConfig.Get<RandomizerOptionSettings>();
        var host = new Host(RandomizerSettings.CreateDefault(options.Value));
        var vm = new TextConfigViewModel(host, options, ShippedConfig.Get<MessageSettings>(), new FakeShell());

        vm.GenerateCommand.Execute(null);

        // randomizer.py:1501 RestoreDefaultSettings
        Assert.Equal("1/-/2/-/0/-/1/-/0/-/3/-/100/-/10/-/90/-/1/-/1/-/0/-/50/-/1/-/1/-/1/-/1/-/0/-/0/-/0/-/1/-/''''''", vm.ConfigText);
    }

    [Fact]
    public void Apply_invalid_text_shows_error_and_keeps_settings()
    {
        var options = ShippedConfig.Get<RandomizerOptionSettings>();
        var original = RandomizerSettings.CreateDefault(options.Value);
        var host = new Host(original);
        var vm = new TextConfigViewModel(host, options, ShippedConfig.Get<MessageSettings>(), new FakeShell()) { ConfigText = "nonsense" };

        vm.ApplyCommand.Execute(null);

        Assert.NotEmpty(vm.ErrorText);
        Assert.Same(original, host.Current);

        vm.ConfigText = "still nonsense"; // editing clears the stale error
        Assert.Empty(vm.ErrorText);
    }

    [Fact]
    public void Apply_and_copy_need_text()
    {
        var options = ShippedConfig.Get<RandomizerOptionSettings>();
        var shell = new FakeShell();
        var vm = new TextConfigViewModel(new Host(RandomizerSettings.CreateDefault(options.Value)), options, ShippedConfig.Get<MessageSettings>(), shell);

        Assert.False(vm.ApplyCommand.CanExecute(null));
        Assert.False(vm.CopyCommand.CanExecute(null));

        vm.GenerateCommand.Execute(null);
        Assert.True(vm.ApplyCommand.CanExecute(null));

        vm.CopyCommand.Execute(null);
        Assert.Equal([vm.ConfigText], shell.Copied);
    }

    [Fact]
    public void Applying_a_valid_text_updates_the_settings()
    {
        var options = ShippedConfig.Get<RandomizerOptionSettings>();
        var original = RandomizerSettings.CreateDefault(options.Value);
        var host = new Host(original);
        var vm = new TextConfigViewModel(host, options, ShippedConfig.Get<MessageSettings>(), new FakeShell());
        vm.GenerateCommand.Execute(null);

        vm.ApplyCommand.Execute(null);

        Assert.Empty(vm.ErrorText);
        Assert.NotEmpty(vm.StatusText);
        Assert.NotSame(original, host.Current);
    }
}

public class EnemyConfigEditorViewModelTests
{
    private readonly FakeEnemyConfigs _configs = new();
    private readonly FakeDialogs _dialogs = new();

    private EnemyConfigEditorViewModel Create() => new(
        _configs,
        ShippedConfig.Get<ReplacementRuleSettings>(),
        ShippedConfig.Get<MessageSettings>(),
        _dialogs,
        _dialogs);

    [Fact]
    public void Loads_default_config_without_changes()
    {
        var vm = Create();

        Assert.Equal(2, vm.Rows.Count);
        Assert.False(vm.HasUnsavedChanges);
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Enable_all_skips_excluded_enemies()
    {
        var vm = Create();
        vm.DisableAllCommand.Execute(null);

        vm.EnableAllCommand.Execute(null);

        Assert.True(vm.Rows.Single(r => r.Id == "c2500").IsEnabled);
        Assert.False(vm.Rows.Single(r => r.Id == "c5201").IsEnabled);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void Default_config_can_only_be_saved_as()
    {
        var vm = Create();
        vm.Rows[0].IsEnabled = false;

        Assert.False(vm.SaveCommand.CanExecute(null));
        Assert.True(vm.StartSaveAsCommand.CanExecute(null));
    }

    [Fact]
    public void Save_as_writes_flags_and_selects_new_config()
    {
        var vm = Create();
        vm.Rows[0].IsEnabled = false;
        vm.StartSaveAsCommand.Execute(null);
        vm.NewConfigName = "Mine";

        vm.ConfirmSaveAsCommand.Execute(null);

        Assert.Equal([false, false], _configs.LastSaved);
        Assert.Equal("Mine", vm.SelectedConfig);
        Assert.Contains("Mine", vm.ConfigNames);
        Assert.False(vm.HasUnsavedChanges);
        Assert.False(vm.IsSaveAsMode);
        Assert.False(vm.Rows[0].IsEnabled); // not reloaded
    }

    [Fact]
    public void Invalid_names_are_explained_before_saving()
    {
        var vm = Create();
        vm.Rows[0].IsEnabled = false;
        vm.StartSaveAsCommand.Execute(null);

        vm.NewConfigName = "Default"; // reserved, rejected by the repository

        Assert.NotEmpty(vm.NameError);
        Assert.False(vm.ConfirmSaveAsCommand.CanExecute(null));

        vm.NewConfigName = "Mine";
        Assert.Empty(vm.NameError);
        Assert.True(vm.ConfirmSaveAsCommand.CanExecute(null));
    }

    [Fact]
    public void Existing_names_show_a_replace_hint()
    {
        _configs.Names.Add("Mine");
        var vm = Create();

        vm.NewConfigName = "Mine";

        Assert.Empty(vm.NameError);
        Assert.NotEmpty(vm.NameWarning);
    }

    [Fact]
    public void Declining_to_discard_changes_keeps_selection()
    {
        _configs.Names.Add("Other");
        var vm = Create();
        vm.Rows[0].IsEnabled = false;
        _dialogs.ConfirmAnswer = false;

        vm.SelectedConfig = "Other";

        Assert.Equal("Default", vm.SelectedConfig);
        Assert.False(vm.Rows[0].IsEnabled);
        Assert.True(vm.HasUnsavedChanges);
    }
}
