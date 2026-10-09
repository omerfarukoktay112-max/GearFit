using System.Globalization;
using System.Text;
using GearFit.Models;
using SQLite;

namespace GearFit.Services;

public class AppDb
{
    static readonly (string Group, string[] Names)[] SeedExercises =
    {
        ("Göğüs", new[] { "Bench Press", "Incline Dumbbell Press", "Cable Fly", "Push-up", "Dips", "Chest Press Machine" }),
        ("Sırt", new[] { "Lat Pulldown", "Barbell Row", "Pull-up", "Seated Cable Row", "Deadlift", "T-Bar Row" }),
        ("Omuz", new[] { "Omuz Press", "Lateral Raise", "Face Pull", "Arnold Press", "Rear Delt Fly" }),
        ("Biceps", new[] { "Biceps Curl", "Hammer Curl", "Preacher Curl", "Incline Curl" }),
        ("Triceps", new[] { "Triceps Pushdown", "Skull Crusher", "Overhead Triceps Extension", "Close-Grip Bench Press" }),
        ("Bacak", new[] { "Squat", "Romanian Deadlift", "Leg Press", "Leg Extension", "Leg Curl", "Lunge", "Calf Raise" }),
        ("Karın", new[] { "Plank", "Crunch", "Hanging Leg Raise", "Cable Crunch" }),
    };

    static readonly (string Name, string[] Exercises)[] SeedRoutines =
    {
        ("Push", new[] { "Bench Press", "Omuz Press", "Triceps Pushdown" }),
        ("Pull", new[] { "Lat Pulldown", "Barbell Row", "Biceps Curl" }),
        ("Bacak", new[] { "Squat", "Romanian Deadlift", "Leg Press" }),
    };

    public static readonly string[] MuscleGroups = SeedExercises.Select(s => s.Group).ToArray();

    readonly SemaphoreSlim _gate = new(1, 1);
    SQLiteAsyncConnection? _db;

    async Task<SQLiteAsyncConnection> Db()
    {
        if (_db != null) return _db;
        await _gate.WaitAsync();
        try
        {
            if (_db != null) return _db;
            var db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "gearfit.db3"));
            await db.CreateTableAsync<Exercise>();
            await db.CreateTableAsync<Routine>();
            await db.CreateTableAsync<RoutineExercise>();
            await db.CreateTableAsync<Workout>();
            await db.CreateTableAsync<WorkoutExercise>();
            await db.CreateTableAsync<WorkoutSet>();
            await db.CreateTableAsync<BodyWeightEntry>();
            if (await db.Table<Exercise>().CountAsync() == 0) await Seed(db);
            _db = db;
            return db;
        }
        finally { _gate.Release(); }
    }

    static async Task Seed(SQLiteAsyncConnection db)
    {
        foreach (var (group, names) in SeedExercises)
            foreach (var n in names)
                await db.InsertAsync(new Exercise { Name = n, MuscleGroup = group });

        var all = await db.Table<Exercise>().ToListAsync();
        var order = 0;
        foreach (var (name, exercises) in SeedRoutines)
        {
            var r = new Routine { Name = name, SortOrder = order++ };
            await db.InsertAsync(r);
            for (var i = 0; i < exercises.Length; i++)
                await db.InsertAsync(new RoutineExercise
                {
                    RoutineId = r.Id,
                    ExerciseId = all.First(e => e.Name == exercises[i]).Id,
                    Position = i
                });
        }
    }

    // ---------- Egzersizler ----------

    public async Task<List<Exercise>> GetExercisesAsync()
    {
        var db = await Db();
        return await db.Table<Exercise>().OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Exercise> AddExerciseAsync(string name, string group)
    {
        var db = await Db();
        var existing = (await db.Table<Exercise>().ToListAsync())
            .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing != null) return existing;
        var ex = new Exercise { Name = name.Trim(), MuscleGroup = group, IsCustom = true };
        await db.InsertAsync(ex);
        return ex;
    }

    // ---------- Rutinler ----------

    public async Task<List<RoutineSummary>> GetRoutinesAsync()
    {
        var db = await Db();
        var routines = await db.Table<Routine>().OrderBy(r => r.SortOrder).ToListAsync();
        var result = new List<RoutineSummary>();
        foreach (var r in routines)
        {
            var names = (await GetRoutineExercisesAsync(r.Id)).Select(e => e.Name);
            result.Add(new RoutineSummary(r.Id, r.Name, string.Join(" · ", names)));
        }
        return result;
    }

    public async Task<string> GetRoutineNameAsync(int id)
    {
        var db = await Db();
        return (await db.FindAsync<Routine>(id))?.Name ?? "";
    }

    public async Task<List<Exercise>> GetRoutineExercisesAsync(int routineId)
    {
        var db = await Db();
        return await db.QueryAsync<Exercise>(
            "SELECT e.* FROM Exercise e JOIN RoutineExercise re ON re.ExerciseId = e.Id " +
            "WHERE re.RoutineId = ? ORDER BY re.Position", routineId);
    }

    public async Task<int> SaveRoutineAsync(int id, string name, IEnumerable<int> exerciseIds)
    {
        var db = await Db();
        var ids = exerciseIds.ToList();
        var routineId = id;
        await db.RunInTransactionAsync(conn =>
        {
            if (routineId == 0)
            {
                var order = conn.Table<Routine>().Count();
                var r = new Routine { Name = name, SortOrder = order };
                conn.Insert(r);
                routineId = r.Id;
            }
            else
            {
                conn.Execute("UPDATE Routine SET Name = ? WHERE Id = ?", name, routineId);
                conn.Execute("DELETE FROM RoutineExercise WHERE RoutineId = ?", routineId);
            }
            for (var i = 0; i < ids.Count; i++)
                conn.Insert(new RoutineExercise { RoutineId = routineId, ExerciseId = ids[i], Position = i });
        });
        return routineId;
    }

    public async Task DeleteRoutineAsync(int id)
    {
        var db = await Db();
        await db.ExecuteAsync("DELETE FROM RoutineExercise WHERE RoutineId = ?", id);
        await db.ExecuteAsync("DELETE FROM Routine WHERE Id = ?", id);
    }

    // ---------- Antrenmanlar ----------

    public async Task<List<WorkoutSet>> GetPreviousSetsAsync(int exerciseId)
    {
        var db = await Db();
        return await db.QueryAsync<WorkoutSet>(
            "SELECT ws.* FROM WorkoutSet ws JOIN WorkoutExercise we ON ws.WorkoutExerciseId = we.Id " +
            "WHERE we.Id = (SELECT we2.Id FROM WorkoutExercise we2 JOIN Workout w2 ON we2.WorkoutId = w2.Id " +
            "WHERE we2.ExerciseId = ? ORDER BY w2.StartedAt DESC LIMIT 1) ORDER BY ws.Number", exerciseId);
    }

    /// <summary>Bir egzersiz için geçmişteki en iyi tahmini 1RM (kg).</summary>
    public async Task<double> GetBest1RmAsync(int exerciseId)
    {
        var db = await Db();
        var sets = await db.QueryAsync<WorkoutSet>(
            "SELECT ws.* FROM WorkoutSet ws JOIN WorkoutExercise we ON ws.WorkoutExerciseId = we.Id WHERE we.ExerciseId = ?",
            exerciseId);
        return sets.Count == 0 ? 0 : sets.Max(s => Formulas.OneRm(s.WeightKg, s.Reps));
    }

    public async Task<int> SaveWorkoutAsync(Workout workout, List<WorkoutExerciseDetail> exercises)
    {
        var db = await Db();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Insert(workout);
            foreach (var ed in exercises)
            {
                ed.Exercise.WorkoutId = workout.Id;
                conn.Insert(ed.Exercise);
                foreach (var s in ed.Sets)
                {
                    s.WorkoutExerciseId = ed.Exercise.Id;
                    conn.Insert(s);
                }
            }
        });
        return workout.Id;
    }

    public async Task<List<Workout>> GetWorkoutsAsync()
    {
        var db = await Db();
        return await db.Table<Workout>().OrderByDescending(w => w.StartedAt).ToListAsync();
    }

    public async Task<WorkoutDetail?> GetWorkoutDetailAsync(int id)
    {
        var db = await Db();
        var w = await db.FindAsync<Workout>(id);
        if (w == null) return null;
        var exercises = await db.Table<WorkoutExercise>().Where(e => e.WorkoutId == id).OrderBy(e => e.Position).ToListAsync();
        var list = new List<WorkoutExerciseDetail>();
        foreach (var e in exercises)
        {
            var sets = await db.Table<WorkoutSet>().Where(s => s.WorkoutExerciseId == e.Id).OrderBy(s => s.Number).ToListAsync();
            list.Add(new WorkoutExerciseDetail(e, sets));
        }
        return new WorkoutDetail(w, list);
    }

    public async Task DeleteWorkoutAsync(int id)
    {
        var db = await Db();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute("DELETE FROM WorkoutSet WHERE WorkoutExerciseId IN (SELECT Id FROM WorkoutExercise WHERE WorkoutId = ?)", id);
            conn.Execute("DELETE FROM WorkoutExercise WHERE WorkoutId = ?", id);
            conn.Execute("DELETE FROM Workout WHERE Id = ?", id);
        });
    }

    public async Task RenameWorkoutAsync(int id, string name)
    {
        var db = await Db();
        await db.ExecuteAsync("UPDATE Workout SET Name = ? WHERE Id = ?", name, id);
    }

    // ---------- İlerleme ----------

    public async Task<List<Exercise>> GetExercisesWithHistoryAsync()
    {
        var db = await Db();
        return await db.QueryAsync<Exercise>(
            "SELECT * FROM Exercise WHERE Id IN (SELECT DISTINCT ExerciseId FROM WorkoutExercise) ORDER BY Name");
    }

    public async Task<List<(SeriesPoint Point, List<WorkoutSet> Sets)>> GetExerciseHistoryAsync(int exerciseId)
    {
        var db = await Db();
        var rows = await db.QueryAsync<WorkoutExercise>(
            "SELECT we.* FROM WorkoutExercise we JOIN Workout w ON we.WorkoutId = w.Id " +
            "WHERE we.ExerciseId = ? ORDER BY w.StartedAt", exerciseId);
        var result = new List<(SeriesPoint, List<WorkoutSet>)>();
        foreach (var we in rows)
        {
            var w = await db.FindAsync<Workout>(we.WorkoutId);
            var sets = await db.Table<WorkoutSet>().Where(s => s.WorkoutExerciseId == we.Id).ToListAsync();
            if (w == null || sets.Count == 0) continue;
            result.Add((new SeriesPoint(w.StartedAt, sets.Max(s => s.WeightKg),
                sets.Max(s => Formulas.OneRm(s.WeightKg, s.Reps))), sets));
        }
        return result;
    }

    // ---------- Vücut ağırlığı ----------

    public async Task<List<BodyWeightEntry>> GetBodyWeightsAsync()
    {
        var db = await Db();
        return await db.Table<BodyWeightEntry>().OrderBy(b => b.Date).ToListAsync();
    }

    public async Task SaveBodyWeightAsync(double kg)
    {
        var db = await Db();
        var today = DateTime.Today;
        var existing = (await db.Table<BodyWeightEntry>().ToListAsync()).FirstOrDefault(b => b.Date.Date == today);
        if (existing != null)
        {
            existing.Kg = kg;
            await db.UpdateAsync(existing);
        }
        else await db.InsertAsync(new BodyWeightEntry { Date = today, Kg = kg });
    }

    // ---------- Dışa aktarma ----------

    public async Task<string> ExportCsvAsync()
    {
        var db = await Db();
        var rows = await db.QueryAsync<ExportRow>(
            "SELECT w.StartedAt AS Date, w.Name AS Workout, we.ExerciseName AS Exercise, ws.Number AS SetNo, " +
            "ws.WeightKg AS WeightKg, ws.Reps AS Reps FROM WorkoutSet ws " +
            "JOIN WorkoutExercise we ON ws.WorkoutExerciseId = we.Id JOIN Workout w ON we.WorkoutId = w.Id " +
            "ORDER BY w.StartedAt, we.Position, ws.Number");
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder("Tarih,Antrenman,Egzersiz,Set,Kg,Tekrar\n");
        foreach (var r in rows)
            sb.Append(r.Date.ToString("yyyy-MM-dd HH:mm", inv)).Append(',')
              .Append(Csv(r.Workout)).Append(',').Append(Csv(r.Exercise)).Append(',')
              .Append(r.SetNo).Append(',').Append(r.WeightKg.ToString("0.##", inv)).Append(',').Append(r.Reps).Append('\n');
        return sb.ToString();
    }

    static string Csv(string s) => s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    class ExportRow
    {
        public DateTime Date { get; set; }
        public string Workout { get; set; } = "";
        public string Exercise { get; set; } = "";
        public int SetNo { get; set; }
        public double WeightKg { get; set; }
        public int Reps { get; set; }
    }
}
