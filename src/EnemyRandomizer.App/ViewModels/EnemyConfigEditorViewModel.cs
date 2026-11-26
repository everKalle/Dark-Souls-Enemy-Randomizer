using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnemyRandomizer.App.Services;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.EnemyConfigs;
using EnemyRandomizer.Core.Settings;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.App.ViewModels;

/// <summary>Enemy config editor: enable/disable enemies and save them as custom configs (legacy <c>randomizer.py:1121-1479</c>).</summary>
public sealed partial class EnemyConfigEditorViewModel : ObservableObject
{
    private readonly IEnemyConfigRepository _repository;
    private readonly IDialogService _dialogs;
    private readonly IUiThread _ui;
    private readonly MessageSettings _messages;
    private readonly HashSet<string> _excludedFromEnableAll;
    private bool _suppressSelectionHandling;

    public EnemyConfigEditorViewModel(
        IEnemyConfigRepository repository,
        IOptions<ReplacementRuleSettings> rules,
        IOptions<MessageSettings> messages,
        IDialogService dialogs,
        IUiThread ui)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(messages);

        _repository = repository;
        _dialogs = dialogs;
        _ui = ui;
        _messages = messages.Value;
        _excludedFromEnableAll = rules.Value.ExcludedFromEnableAll.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in _repository.ListNames())
        {
            ConfigNames.Add(name);
        }

        SelectedConfig = RandomizerSettings.DefaultEnemyConfig;
    }

    public ObservableCollection<string> ConfigNames { get; } = [];

    public ObservableCollection<EnemyConfigRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    public partial string SelectedConfig { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(StartSaveAsCommand))]
    public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty]
    public partial bool IsSaveAsMode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmSaveAsCommand))]
    [NotifyPropertyChangedFor(nameof(NameError), nameof(NameWarning))]
    public partial string NewConfigName { get; set; } = "";

    /// <summary>Why the typed name cannot be used; empty while the name is acceptable.</summary>
    public string NameError =>
        string.IsNullOrWhiteSpace(NewConfigName) || _repository.IsValidName(NewConfigName.Trim())
            ? ""
            : _messages.Get(MessageKeys.EnemyConfigInvalidName, NewConfigName.Trim());

    /// <summary>Hint that saving replaces an existing config.</summary>
    public string NameWarning =>
        NameError.Length == 0 && !string.IsNullOrWhiteSpace(NewConfigName) && _repository.Exists(NewConfigName.Trim())
            ? _messages.Get(MessageKeys.EnemyConfigReplaceHint)
            : "";

    /// <summary>True when the loaded config has no rows, so the grid can explain itself.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    partial void OnSelectedConfigChanged(string oldValue, string newValue)
    {
        if (_suppressSelectionHandling)
        {
            return;
        }

        if (oldValue is not null && HasUnsavedChanges
            && !_dialogs.Confirm("Unsaved changes", _messages.Get(MessageKeys.UnsavedEnemyConfig)))
        {
            // Undo after the ComboBox finished its own update.
            _ui.Post(() => SelectWithoutLoading(oldValue));
            return;
        }

        Load(newValue);
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void EnableAll() => SetAll(row => !_excludedFromEnableAll.Contains(row.Id), true);

    [RelayCommand]
    private void DisableAll() => SetAll(_ => true, false);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save() => SaveAs(SelectedConfig);

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private void StartSaveAs()
    {
        NewConfigName = "";
        IsSaveAsMode = true;
    }

    [RelayCommand]
    private void CancelSaveAs() => IsSaveAsMode = false;

    [RelayCommand(CanExecute = nameof(CanConfirmSaveAs))]
    private void ConfirmSaveAs()
    {
        var name = NewConfigName.Trim();
        if (!_repository.IsValidName(name))
        {
            _dialogs.ShowError("Save As", _messages.Get(MessageKeys.EnemyConfigInvalidName, name));
            return;
        }

        if (_repository.Exists(name)
            && !_dialogs.Confirm("Config already exists", _messages.Get(MessageKeys.EnemyConfigExists, name)))
        {
            return;
        }

        SaveAs(name);
    }

    private bool CanSave() =>
        HasUnsavedChanges && !string.Equals(SelectedConfig, RandomizerSettings.DefaultEnemyConfig, StringComparison.OrdinalIgnoreCase);

    private bool CanConfirmSaveAs() => !string.IsNullOrWhiteSpace(NewConfigName) && NameError.Length == 0;

    private void SaveAs(string name)
    {
        try
        {
            var file = _repository.Save(name, Rows.Select(r => r.IsEnabled).ToList());
            HasUnsavedChanges = false;
            IsSaveAsMode = false;

            ConfigNames.Clear();
            foreach (var config in _repository.ListNames())
            {
                ConfigNames.Add(config);
            }

            SelectWithoutLoading(name); // rows already hold the saved state
            SaveCommand.NotifyCanExecuteChanged();

            _dialogs.ShowInfo("Config saved", _messages.Get(MessageKeys.EnemyConfigSaved, file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _dialogs.ShowError("Failed to save config", _messages.Get(MessageKeys.EnemyConfigSaveFailed, ex.Message));
        }
    }

    private void SelectWithoutLoading(string name)
    {
        _suppressSelectionHandling = true;
        SelectedConfig = name;
        _suppressSelectionHandling = false;
    }

    private void Load(string name)
    {
        IReadOnlyList<EnemyConfigEntry> entries;
        try
        {
            entries = _repository.Load(name);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            _dialogs.ShowError("Enemy config", ex.Message);
            return;
        }

        foreach (var row in Rows)
        {
            row.PropertyChanged -= OnRowChanged;
        }

        Rows.Clear();
        foreach (var entry in entries)
        {
            var row = new EnemyConfigRowViewModel(entry);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }

        IsEmpty = Rows.Count == 0;
        HasUnsavedChanges = false;
    }

    private void SetAll(Func<EnemyConfigRowViewModel, bool> filter, bool enabled)
    {
        foreach (var row in Rows.Where(filter))
        {
            row.IsEnabled = enabled;
        }

        HasUnsavedChanges = true;
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => HasUnsavedChanges = true;
}

/// <summary>One enemy row in the editor.</summary>
public sealed partial class EnemyConfigRowViewModel(EnemyConfigEntry entry) : ObservableObject
{
    public string Id => entry.Id;

    public string Name => entry.Name;

    public string Comment => entry.Comment;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = entry.Enabled;
}
