using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Models;
using GearFit.Services;

namespace GearFit.ViewModels;

public partial class LibraryItem : ObservableObject
{
    public LibraryItem(Exercise exercise, bool selected)
    {
        Exercise = exercise;
        isSelected = selected;
    }

    public Exercise Exercise { get; }
    public string Name => Exercise.Name;
    public string Group => LocalizationService.Instance.Group(Exercise.MuscleGroup);

    [ObservableProperty] bool isSelected;
}

/// <summary>Name = DB'deki kanonik ad ("*" = tümü), Display = seçili dilde görünen ad.</summary>
public record GroupChip(string Name, bool Selected, string Display);

public partial class LibraryViewModel : ObservableObject
{
    readonly AppDb _db;
    readonly SelectionService _selection;
    readonly List<LibraryItem> _all = new();
    readonly HashSet<int> _selectedIds = new();

    public LibraryViewModel(AppDb db, SelectionService selection)
    {
        _db = db;
        _selection = selection;
        Groups = new[] { "*" }.Concat(AppDb.MuscleGroups).ToArray();
        NewGroups = AppDb.MuscleGroups;
    }

    public string[] Groups { get; }
    public string[] NewGroups { get; }
    public ObservableCollection<LibraryItem> Items { get; } = new();
    public ObservableCollection<GroupChip> Chips { get; } = new();
    public ObservableCollection<GroupChip> NewChips { get; } = new();

    [ObservableProperty] string searchText = "";
    [ObservableProperty] int groupIndex;
    [ObservableProperty] bool isSheetOpen;
    [ObservableProperty] string newName = "";
    [ObservableProperty] int newGroupIndex = -1;

    public string AddButtonText => _selectedIds.Count > 0 ? Loc.T("add_count", _selectedIds.Count) : Loc.T("add");
    public bool CanAdd => _selectedIds.Count > 0;
    public bool CanSaveNew => !string.IsNullOrWhiteSpace(NewName) && NewGroupIndex >= 0;
    public bool IsEmpty => Items.Count == 0;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnGroupIndexChanged(int value)
    {
        ApplyFilter();
        RebuildChips();
    }
    partial void OnNewNameChanged(string value) => OnPropertyChanged(nameof(CanSaveNew));
    partial void OnNewGroupIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanSaveNew));
        RebuildNewChips();
    }

    public async Task LoadAsync()
    {
        var exercises = await _db.GetExercisesAsync();
        _all.Clear();
        foreach (var e in exercises.Where(e => !_selection.Exclude.Contains(e.Id)))
            _all.Add(new LibraryItem(e, _selectedIds.Contains(e.Id)));

        if (_selection.PendingNew != null)
        {
            _selectedIds.Add(_selection.PendingNew.Id);
            var created = _all.FirstOrDefault(i => i.Exercise.Id == _selection.PendingNew.Id);
            if (created != null) created.IsSelected = true;
            _selection.PendingNew = null;
        }
        ApplyFilter();
        RebuildChips();
        RebuildNewChips();
        NotifySelection();
    }

    void ApplyFilter()
    {
        var q = SearchText?.Trim() ?? "";
        var group = GroupIndex > 0 ? Groups[GroupIndex] : null;
        var filtered = _all.Where(i =>
            (group == null || i.Group == group) &&
            (q.Length == 0 || i.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase)));
        Items.Clear();
        foreach (var i in filtered) Items.Add(i);
        OnPropertyChanged(nameof(IsEmpty));
    }

    void RebuildChips()
    {
        Chips.Clear();
        for (var i = 0; i < Groups.Length; i++)
            Chips.Add(new GroupChip(Groups[i], i == GroupIndex, i == 0 ? Loc.T("all") : Loc.Group(Groups[i])));
    }

    void RebuildNewChips()
    {
        NewChips.Clear();
        for (var i = 0; i < NewGroups.Length; i++)
            NewChips.Add(new GroupChip(NewGroups[i], i == NewGroupIndex, Loc.Group(NewGroups[i])));
    }

    void NotifySelection()
    {
        OnPropertyChanged(nameof(AddButtonText));
        OnPropertyChanged(nameof(CanAdd));
    }

    [RelayCommand]
    void SelectGroup(GroupChip chip) => GroupIndex = Array.IndexOf(Groups, chip.Name);

    [RelayCommand]
    void SelectNewGroup(GroupChip chip) => NewGroupIndex = Array.IndexOf(NewGroups, chip.Name);

    [RelayCommand]
    void Toggle(LibraryItem item)
    {
        item.IsSelected = !item.IsSelected;
        if (item.IsSelected) _selectedIds.Add(item.Exercise.Id);
        else _selectedIds.Remove(item.Exercise.Id);
        NotifySelection();
    }

    [RelayCommand]
    void OpenSheet()
    {
        NewName = SearchText ?? "";
        NewGroupIndex = GroupIndex > 0 ? GroupIndex - 1 : -1;
        RebuildNewChips();
        IsSheetOpen = true;
    }

    [RelayCommand]
    void CloseSheet() => IsSheetOpen = false;

    [RelayCommand]
    async Task SaveNew()
    {
        if (!CanSaveNew) return;
        var created = await _db.AddExerciseAsync(NewName.Trim(), NewGroups[NewGroupIndex]);
        _selection.PendingNew = created;
        IsSheetOpen = false;
        SearchText = "";
        GroupIndex = 0;
        await LoadAsync();
    }

    [RelayCommand]
    async Task Close()
    {
        _selection.OnPicked = null;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Confirm()
    {
        var picked = _all.Where(i => _selectedIds.Contains(i.Exercise.Id)).Select(i => i.Exercise).ToList();
        var callback = _selection.OnPicked;
        _selection.OnPicked = null;
        if (callback != null && picked.Count > 0) await callback(picked);
        await Shell.Current.GoToAsync("..");
    }
}
