# StartupScanner trigger-detection tests

End-to-end tests for the scheduled-task trigger/timing logic in
[`src/StartupScanner.cs`](../../src/StartupScanner.cs) — specifically
`StartupTask.GetAutoStartTypes` and its helpers
(`CheckRepititionInteval`, `IsRepetitionWindowLive`, `IsTriggerLive`,
`TryParseBoundary`, `MakeFriendlyRepeatString`).

## How it works

The app has no CLI/headless mode, so the tests exercise the real scanner and inspect the
artifact it leaves behind:

1. The user's real detection store is backed up and removed so the run starts from a clean
   **baseline**.
2. A controlled set of scheduled tasks is registered in a dedicated Task Scheduler folder
   (`\ThioBanTest`), each crafted to hit one branch of the detection logic. Tasks are built
   through the `Schedule.Service` COM API — the same API the app scans through — which avoids
   Task Scheduler XML element-ordering pitfalls and lets each trigger's `Enabled`,
   `StartBoundary`, `EndBoundary`, and `Repetition` be set exactly.
3. The built exe is launched with `--quiet`. On a baseline run, `DetectionStore.Apply` records
   **every** currently-present autorun item into
   `%APPDATA%\ThioJoe\Thio's Background App Notifier\detections.json` (all pre-marked
   *alerted*), so quiet mode saves the store and exits **without showing any UI**
   (`Program.Main`). That's what makes this runnable unattended on CI.
4. The test parses `detections.json`, keeps only the items whose `IdentityKey` points into
   `\ThioBanTest`, and asserts detection presence/absence plus the exact `Detail` string
   (`Detail` == the joined trigger descriptions, e.g. `At Logon`, `Daily`, `Every 2 days`,
   `Repeats every 1 hour`).
5. The test tasks are removed and the user's real store is restored.

Because it writes to the same store path as an installed copy of the app, a **local** run
temporarily replaces your real `detections.json` and restores it in a `finally` block. On CI
there is no pre-existing store.

## Running

On CI: the [`StartupScanner Trigger Detection Tests`](../../.github/workflows/startup-scanner-tests.yml)
workflow runs on `windows-latest` (manual dispatch, or on push/PR touching the scanner or these
tests).

Locally, from an **elevated** PowerShell (registering scheduled tasks needs admin):

```powershell
# build first (Visual Studio, or msbuild):
msbuild src\New-Startup-App-Notifier.csproj /p:Configuration=Release

pwsh -File tests/StartupScanner/Run-TriggerDetectionTests.ps1 `
     -ExePath src/bin/Release/Thio-Background-App-Notifier.exe
```

Useful switches: `-KeepTasks` and `-KeepStore` leave the test tasks / store in place for
inspection. The produced `detections.json` and a `results.json` are written to `_artifacts/`.

Exit code is non-zero if any case fails.

## Case matrix

| Case | Trigger setup | Expected | Branch exercised |
|------|---------------|----------|------------------|
| C01_Logon | Logon | detect `At Logon` | considered type: `TASK_TRIGGER_LOGON` |
| C02_Boot | Boot | detect `At Boot` | considered type: `TASK_TRIGGER_BOOT` |
| C03_Idle | Idle | detect `Whenever Idle` | considered type: `TASK_TRIGGER_IDLE` |
| C04_Daily1 | Daily, interval 1 | detect `Daily` | daily with `DaysInterval <= 1` |
| C05_Daily2 | Daily, interval 2 | detect `Every 2 days`, not `Daily` | daily `DaysInterval > 1` → `otherDescriptions` |
| C06_Daily3 | Daily, interval 3 | detect `Every 3 days` | daily `DaysInterval > 1` |
| C07_Weekly | Weekly | ignored | non-considered base type |
| C08_Monthly | Monthly | ignored | non-considered base type |
| C09_OnceFuture | One-time, no repetition | ignored | non-considered base type |
| C10_Event | Event | ignored | non-considered base type |
| C11_Registration | Registration | ignored | non-considered base type |
| C12_SessionUnlock | Session unlock | ignored | non-considered base type |
| C13_NoTriggers | exec action, no triggers | ignored | empty trigger set |
| C14_TriggerDisabled | Logon, `Enabled=false` | ignored | `trigger.Enabled != true` skip |
| C15_TaskDisabled | Logon, task `Enabled=false` | ignored | `task.Enabled` skip in `ProcessTaskFolder` |
| C16_MixedDisabledEnabled | Logon(off) + Boot(on) | detect `At Boot` only | per-trigger enabled filtering |
| C17_LogonExpiredEnd | Logon, `EndBoundary` past | ignored | `IsTriggerLive` retires by `EndBoundary` |
| C18_LogonFutureEnd | Logon, `EndBoundary` future | detect `At Logon` | `IsTriggerLive` still live |
| C19_BootExpiredEnd | Boot, `EndBoundary` past | ignored | `IsTriggerLive` applies to all types |
| C20_DailyRepeatHourly | Daily(1) + repeat PT1H | detect `Daily` + `Repeats every 1 hour` | repetition on a considered trigger |
| C21_LogonRepeat30m | Logon + repeat PT30M | detect `At Logon` + `Repeats every 30 minutes` | repetition + minutes formatting |
| C22_BootRepeat15m | Boot + repeat PT15M | detect `At Boot` + `Repeats every 15 minutes` | repetition + minutes formatting |
| C23_OnceRepeatOngoing | One-time (start −10m) + repeat PT1M/PT1H | detect `Repeats every 1 minute` | `IsRepetitionWindowLive`: window open |
| C24_OnceRepeatFuture | One-time (start +1d) + repeat PT1M/PT30M | detect `Repeats every 1 minute` | `IsRepetitionWindowLive`: not yet started |
| C25_OnceRepeatDeadWindow | One-time (start −2h) + repeat PT1M/PT30M | ignored | `IsRepetitionWindowLive`: window closed |
| C26_WeeklyRepeatHourly | Weekly + repeat PT1H | detect `Repeats every 1 hour` | repetition makes a non-considered type detectable |
| C27_Repeat24h | Logon + repeat PT24H | detect `Repeats every 24 hours` | `TotalDays == 1` falls to the hours branch |
| C28_Repeat2d | Logon + repeat P2D | detect `Repeats every 2 days` | days branch |
| C29_Repeat90m | Logon + repeat PT1H30M | detect `Repeats every 1.5 hours` | hours branch + rounding |
| C30_Repeat1m | Logon + repeat PT1M | detect `Repeats every 1 minute` | minutes branch, singular |
| C31_BootAndLogon | Boot + Logon | detect `At Boot` + `At Logon` | multiple considered triggers |
| C32_DistinctRepeat | Logon(PT1H) + Boot(PT1H) | `Repeats every 1 hour` appears once | `.Distinct()` de-duplication |

### Documented gaps (unreachable via a live task)

Two defensive branches can't be covered because Task Scheduler rejects the required
configuration at registration time. They're listed in the script as `SKIP` so the coverage
report stays honest:

- **S01 — `Duration < Interval`** in `CheckRepititionInteval` (repetition dropped because the
  window closes before the first repeat). Task Scheduler refuses to register a repetition whose
  interval exceeds its duration.
- **S02 — sub-minute interval** (the seconds branch of `MakeFriendlyRepeatString`). Task
  Scheduler enforces a 1-minute minimum repetition interval.

## Assumptions / notes

- Runs on a Windows runner where the process has administrator rights (GitHub `windows-latest`
  does). Reading SYSTEM-owned tasks and registering tasks both require this.
- Friendly repeat strings assume invariant/en-US number formatting (e.g. `1.5 hours`), matching
  the default runner culture.
- The "new item" delta / quiet-popup path is intentionally **not** tested here: surfacing new
  (non-baseline) items in quiet mode shows a modal dialog that would block on a headless runner.
  This suite validates the trigger *classification*, which is the logic under test.
