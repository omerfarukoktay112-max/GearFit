# 🏋️ GearFit — Your Pocket Workout Tracker

**Log sets. Crush PRs. Watch your progress grow.**
GearFit is a fast, beautiful, offline-first strength-training tracker built with **.NET MAUI** (net10.0) for **Windows** and **Android**.

## ✨ Features

- **Routines** — build your own workouts; long-press to edit or delete
- **Live workout logging** — record sets, reps and weight with a built-in rest timer
- **Automatic PR detection** — estimated 1RM (Epley formula) flags your personal records instantly
- **History** — browse past workouts as a list or a calendar
- **Progress charts** — per-exercise strength curves and body-weight tracking
- **Weekly goals & streaks** — stay consistent and keep the chain alive
- **kg / lb** units, **dark / light / system** themes
- **Multi-language** UI
- **CSV export** — your data is yours
- 100% local storage (SQLite), no account, no cloud

## 📥 Download (Windows)

Grab the latest `GearFit-win-x64.zip` from the [Releases](../../releases) page, extract it and run `GearFit.exe`. No installation or .NET runtime required.

## 🛠️ Build from source

```powershell
dotnet build -f net10.0-android             # Android
dotnet build -f net10.0-windows10.0.19041.0 # Windows
```

Or open `GearFit.csproj` in Visual Studio 2026 and pick *Android Emulator* or *Windows Machine*.

Publish a standalone Windows build:

```powershell
dotnet publish -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0 -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o publish
```

## 🧱 Project structure

| Folder | Contents |
|---|---|
| `Models/` | SQLite tables (exercises, routines, workouts, sets, body weight) |
| `Services/` | Data access, active-workout engine (timers, PRs), settings, theming |
| `ViewModels/` | Screen logic using CommunityToolkit.Mvvm |
| `Pages/` | App screens and routine editor |
| `Controls/` | Custom icons, segmented control, bottom tabs, charts, progress rings |
| `Localization/` | Translations |

## 📄 License

MIT
