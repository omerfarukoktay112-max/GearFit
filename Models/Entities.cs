using SQLite;

namespace GearFit.Models;

public class Exercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    public bool IsCustom { get; set; }

    [Ignore] public string GroupDisplay => GearFit.Localization.LocalizationService.Instance.Group(MuscleGroup);
}

public class Routine
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class RoutineExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int RoutineId { get; set; }
    public int ExerciseId { get; set; }
    public int Position { get; set; }
}

public class Workout
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    [Indexed] public DateTime StartedAt { get; set; }
    public int DurationSeconds { get; set; }
    public double VolumeKg { get; set; }
    public int SetCount { get; set; }
}

public class WorkoutExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int WorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public int Position { get; set; }
}

public class WorkoutSet
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int WorkoutExerciseId { get; set; }
    public int Number { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public bool IsPr { get; set; }
}

public class BodyWeightEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public DateTime Date { get; set; }
    public double Kg { get; set; }
}

public record RoutineSummary(int Id, string Name, string Summary);

public record SeriesPoint(DateTime Date, double BestWeightKg, double Best1RmKg);

public record WorkoutExerciseDetail(WorkoutExercise Exercise, List<WorkoutSet> Sets);

public record WorkoutDetail(Workout Workout, List<WorkoutExerciseDetail> Exercises);

public record RecordSet(double WeightKg, int Reps, DateTime Date);

public static class Formulas
{
    /// <summary>Epley tahmini 1RM.</summary>
    public static double OneRm(double kg, int reps) => reps <= 0 ? 0 : kg * (1 + reps / 30.0);
}
