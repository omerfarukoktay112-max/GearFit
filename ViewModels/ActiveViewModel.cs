using CommunityToolkit.Mvvm.Input;
using GearFit.Localization;
using GearFit.Services;

namespace GearFit.ViewModels;

public partial class ActiveViewModel
{
    readonly SelectionService _selection;

    public ActiveViewModel(ActiveWorkoutService session, SelectionService selection)
    {
        Session = session;
        _selection = selection;
    }

    public ActiveWorkoutService Session { get; }

    [RelayCommand]
    void ToggleSet(ActiveSetVM set) => Session.ToggleSet(set);

    [RelayCommand]
    void AddSet(ActiveExerciseVM exercise) => Session.AddSet(exercise);

    [RelayCommand]
    void SkipRest() => Session.SkipRest();

    [RelayCommand]
    void AdjustRest(string delta) => Session.AdjustRest(int.Parse(delta));

    [RelayCommand]
    async Task ExerciseMenu(ActiveExerciseVM exercise)
    {
        var lastSet = Loc.T("remove_last_set");
        var removeEx = Loc.T("remove_exercise");
        var choice = await Shell.Current.DisplayActionSheetAsync(exercise.Name, Loc.T("cancel"), null, lastSet, removeEx);
        if (choice == lastSet) Session.RemoveLastSet(exercise);
        else if (choice == removeEx) Session.RemoveExercise(exercise);
    }

    [RelayCommand]
    async Task AddExercise()
    {
        _selection.Exclude.Clear();
        foreach (var e in Session.Exercises) _selection.Exclude.Add(e.Exercise.Id);
        _selection.OnPicked = list => Session.AddExercisesAsync(list);
        await Shell.Current.GoToAsync("library");
    }

    [RelayCommand]
    async Task Back() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    async Task Finish()
    {
        var done = Session.CompletedSetCount;
        if (done == 0)
        {
            var discard = await Shell.Current.DisplayAlertAsync(Loc.T("no_sets_title"),
                Loc.T("no_sets_msg"), Loc.T("delete"), Loc.T("continue"));
            if (!discard) return;
            Session.Discard();
            await Shell.Current.GoToAsync("//main");
            return;
        }

        var finish = await Shell.Current.DisplayAlertAsync(Loc.T("finish_title"),
            Loc.T("finish_msg", done), Loc.T("finish"), Loc.T("cancel"));
        if (!finish) return;

        var id = await Session.FinishAsync();
        await Shell.Current.GoToAsync("//history");
        if (id > 0) await Shell.Current.GoToAsync($"detail?id={id}");
    }
}
