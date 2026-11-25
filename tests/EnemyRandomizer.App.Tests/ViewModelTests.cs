using EnemyRandomizer.App.ViewModels;
using EnemyRandomizer.App.ViewModels.Options;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.App.Tests;

public class MainViewModelTests
{
    private readonly FakeDialogs _dialogs = new();
    private readonly FakeLastConfigStore _store = new();
    private readonly FakeEnemyConfigs _configs = new();
    private readonly FakeRandomizer _randomizer = new();

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
        status ?? FakeInstallationStatus.Ok());

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
        Assert.Equal(2, _dialogs.ProgressRuns); // startup checks + randomize
        Assert.Equal(["done"], _dialogs.Infos);
        Assert.False(vm.IsBusy);
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
        var vm = new TextConfigViewModel(host, options, ShippedConfig.Get<MessageSettings>(), new FakeDialogs());

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
        var dialogs = new FakeDialogs();
        var vm = new TextConfigViewModel(host, options, ShippedConfig.Get<MessageSettings>(), dialogs) { ConfigText = "nonsense" };

        vm.ApplyCommand.Execute(null);

        Assert.Single(dialogs.Errors);
        Assert.Same(original, host.Current);
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
