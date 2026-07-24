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
   (`\ThioBanTest`; one case uses a nested subfolder to exercise folder recursion), each crafted
   to hit one branch of the detection logic. Tasks are built
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
| C33_LogonEndZPast | Logon, `EndBoundary` past, UTC `Z` form | ignored | `TryParseBoundary` UTC branch → local conversion |
| C34_LogonEndZFuture | Logon, `EndBoundary` future, UTC `Z` form | detect `At Logon` | UTC boundary still live |
| C35_LogonEndOffsetPast | Logon, `EndBoundary` past, `+hh:mm` offset | ignored | `TryParseBoundary` offset branch |
| C36_LogonEndOffsetFuture | Logon, `EndBoundary` future, offset form | detect `At Logon` | offset boundary still live |
| C37_LogonEndFracSecPast | Logon, `EndBoundary` past with `.123` fraction | ignored | fractional-seconds boundary parse |
| C38_TimeWindowZOngoing | One-time (UTC `Z` start −10m) + PT1M/PT1H | detect `Repeats every 1 minute` | window math on a UTC StartBoundary: open |
| C39_TimeWindowZClosed | One-time (UTC `Z` start −2h) + PT1M/PT30M | ignored | window math on a UTC StartBoundary: closed |
| C40_TimeWindowOffsetOngoing | One-time (offset start −10m) + PT1M/PT1H | detect `Repeats every 1 minute` | window math on an offset StartBoundary |
| C41_IdleExpiredEnd | Idle, `EndBoundary` past | ignored | liveness gating for Idle |
| C42_DailyExpiredEnd | Daily(1), `EndBoundary` past | ignored | liveness gating before the daily branch |
| C43_Daily2ExpiredEnd | Daily(2), `EndBoundary` past | ignored | liveness gating for `Every N days` |
| C44_LogonRepeatExpiredEnd | Logon + PT1H repeat, `EndBoundary` past | ignored | retirement kills base type AND repetition |
| C45_TimeRepeatExpiredEnd | One-time (past) + indefinite PT1M repeat, end past | ignored | retirement beats indefinite repetition |
| C46_MixedLiveExpired | Boot(end past) + Logon(live) | detect `At Logon` only | per-trigger liveness filtering |
| C47_OnceRepeatNoDuration | One-time (start 2020) + PT1M, no duration | detect `Repeats every 1 minute` | no `Duration` = repeat forever, window never closes |
| C48_OnceFutureNoDuration | One-time (start +1d) + PT2M, no duration | detect `Repeats every 2 minutes` | duration-absent path skips the window check |
| C49_WeeklyStaleWindow | Weekly (start −2h) + PT1M/PT30M | detect `Repeats every 1 minute` | non-TIME trigger reopens its window (contrast C25) |
| C50_OnceWindowJustOpen | One-time (start −20m) + PT1M/PT30M | detect `Repeats every 1 minute` | window open with ~10 min to spare |
| C51_OnceWindowJustClosed | One-time (start −40m) + PT1M/PT30M | ignored | window closed ~10 min ago |
| C52_DurationEqualsInterval | One-time (start −10m) + PT30M/PT30M | detect `Repeats every 30 minutes` | `Duration == Interval` is not dropped (guard is strict `<`) |
| C53_DurationDayFormat | One-time (start −2h) + PT1H/P1D | detect `Repeats every 1 hour` | day-denominated `Duration` parse |
| C54_DeadWindowFutureEnd | One-time (start −2h) + PT1M/PT30M, end +10y | ignored | window closure kills it even when not retired |
| C55_IdleRepeatWithDuration | Idle + PT10M/PT1H | detect `Whenever Idle` + `Repeats every 10 minutes` | considered type + windowed repetition |
| C56_EventRepeat | Event + PT1H repeat | detect `Repeats every 1 hour` | repetition rescues an Event trigger |
| C57_UnlockRepeat | Session unlock + PT45M repeat | detect `Repeats every 45 minutes` | repetition rescues a session-state trigger |
| C58_RegistrationRepeat | Registration + PT1H repeat | detect `Repeats every 1 hour` | repetition rescues a Registration trigger |
| C59_MonthlyRepeat | Monthly + PT20M repeat | detect `Repeats every 20 minutes` | repetition rescues a Monthly trigger |
| C60_Repeat23h | Logon + PT23H | detect `Repeats every 23 hours` | hours branch, just below the days flip |
| C61_Repeat25hQuirk | Logon + PT25H | detect `Repeats every 1 days` | days-branch flip at >24h; documents current rounding/pluralization quirk |
| C62_Repeat26h | Logon + PT26H | detect `Repeats every 1.1 days` | days branch rounding |
| C63_Repeat36h | Logon + P1DT12H | detect `Repeats every 1.5 days` | mixed day+hour interval spelling |
| C64_Repeat48hAsHours | Logon + PT48H | detect `Repeats every 2 days` | unit spelling irrelevant after normalization |
| C65_Repeat61mQuirk | Logon + PT61M | detect `Repeats every 1 hours` | hours-branch flip at >60m; documents current quirk |
| C66_Repeat59m | Logon + PT59M | detect `Repeats every 59 minutes` | minutes branch, just below the hours flip |
| C67_Repeat60mIsOneHour | Logon + PT60M | detect `Repeats every 1 hour` | exactly 60m is singular `1 hour` |
| C68_Repeat105m | Logon + PT1H45M | detect `Repeats every 1.8 hours` | hours branch rounding up |
| C69_Repeat90s | Logon + PT1M30S | detect `Repeats every 1.5 minutes` | seconds-granular interval above the 1-minute floor |
| C70_TwoLogonsDedup | Logon + Logon | `At Logon` exactly once | `.Distinct()` on base-type names |
| C71_TwoDailyIntervals | Daily(2) + Daily(3) | detect `Every 2 days, Every 3 days` | distinct multi-day intervals both reported |
| C72_DupEveryNDaysDedup | Daily(2) + Daily(2) | `Every 2 days` exactly once | `.Distinct()` on `Every N days` strings |
| C73_TwoRepeatIntervals | Logon(PT1H) + Boot(PT30M) | detect all four descriptions | different repeat intervals coexist |
| C74_SpellingDedup | Logon(PT60M) + Boot(PT1H) | `Repeats every 1 hour` exactly once | dedup across equivalent interval spellings |
| C75_NamesBeforeRepeats | Weekly(PT1H) created first + Logon | detect `At Logon, Repeats every 1 hour` | base names always precede repeat strings |
| C76_MultiDayDailyWithRep | Daily(2) + PT1H repeat on same trigger | detect `Every 2 days, Repeats every 1 hour` | one trigger contributes two descriptions |
| C77_DisabledRepNoLeak | Logon(on) + Boot(off, PT1M) | detect `At Logon` only | disabled trigger's repetition doesn't leak |
| C78_ExpiredPlusLiveRepeat | One-time(−10m, PT1M/PT1H) + Logon(end past) | detect `Repeats every 1 minute` only | retired sibling doesn't suppress live repetition |
| C79_KitchenSink | disabled + expired + dead-window + live Daily/Weekly reps | detect `Daily, Repeats every 1 hour, Repeats every 30 minutes` | all filters at once |
| C80_HiddenTask | Logon, task `Hidden=true` | detect `At Logon` | `GetTasks(TASK_ENUM_HIDDEN)` |
| C81_NestedFolder | Logon, in `\ThioBanTest\Nested` | detect `At Logon` | `ProcessTaskFolder` subfolder recursion |
| C82_ComActionOnly | Logon, only a COM-handler action | ignored | `hasExecAction` filter |
| C83_ComPlusExecAction | Logon, COM action then exec action | detect `At Logon` | exec-action scan skips non-exec actions |
| C84_TwoExecActions | Logon, two exec actions | detect once | one item per task regardless of action count |
| C85_DefaultEnabled | Logon, `Enabled` never set | detect `At Logon` | trigger default-enabled path |
| C86_BootDelay | Boot + `Delay=PT5M` | detect `At Boot` | delay doesn't change classification |
| C87_DailyRandomDelay | Daily(1) + `RandomDelay=PT1H` | detect `Daily` | random delay doesn't change classification |
| C88_DailyFutureStart | Daily(1), `StartBoundary` +1d | detect `Daily` | future-start calendar task already counts |
| C89_Daily365 | Daily, interval 365 | detect `Every 365 days` | large `DaysInterval` |
| C90_OncePastNoRep | One-time (start 2020), no repetition | ignored | stale one-shot is not autostart via any path |

### Documented gaps (unreachable via a live task)

Four defensive branches can't be covered because Task Scheduler rejects the required
configuration at registration time (or a healthy registered task can never produce it). They're
listed in the script as `SKIP` so the coverage report stays honest:

- **S01 — `Duration < Interval`** in `CheckRepititionInteval` (repetition dropped because the
  window closes before the first repeat). Task Scheduler refuses to register a repetition whose
  interval exceeds its duration.
- **S02 — sub-minute interval** (the seconds branch of `MakeFriendlyRepeatString`). Task
  Scheduler enforces a 1-minute minimum repetition interval.
- **S03 — malformed boundary strings** (the parse-failure fallbacks in `TryParseBoundary`).
  The Task Scheduler service validates boundary date formats at registration, so a live task
  cannot carry a garbage `StartBoundary`/`EndBoundary`.
- **S04 — COM property reads throwing** (the `catch` paths around `EndBoundary` /
  `StartBoundary` / `Repetition` reads). A healthy registered task never throws on those reads.

## Assumptions / notes

- Runs on a Windows runner where the process has administrator rights (GitHub `windows-latest`
  does). Reading SYSTEM-owned tasks and registering tasks both require this.
- Friendly repeat strings assume invariant/en-US number formatting (e.g. `1.5 hours`), matching
  the default runner culture.
- The "new item" delta / quiet-popup path is intentionally **not** tested here: surfacing new
  (non-baseline) items in quiet mode shows a modal dialog that would block on a headless runner.
  This suite validates the trigger *classification*, which is the logic under test.
