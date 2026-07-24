<#
.SYNOPSIS
    End-to-end tests for the scheduled-task trigger detection logic in src/StartupScanner.cs
    (StartupTask.GetAutoStartTypes and its timing/repetition helpers).

.DESCRIPTION
    The app has no CLI/headless mode, so instead of unit-testing GetAutoStartTypes directly we
    drive the whole scanner the way a user would and inspect the artifact it leaves behind:

        1. Back up and remove the user's real detection store so the run is a clean *baseline*.
        2. Register a controlled set of scheduled tasks (in a dedicated Task Scheduler folder),
           each crafted to exercise one branch of the trigger-detection logic.
        3. Launch the built exe with --quiet.

           On a baseline run every currently-present autorun item is written to
           %APPDATA%\ThioJoe\Thio's Background App Notifier\detections.json (marked already-alerted),
           so quiet mode saves and exits WITHOUT showing any UI. See Program.Main / DetectionStore.Apply.

        4. Parse detections.json, keep only the items whose IdentityKey points into our test folder,
           and assert:
              - each task we expect to be detected is present, with the right "Detail" string
                (Detail == the joined trigger descriptions: "At Logon", "Daily", "Every 2 days",
                 "Repeats every 1 hour", ...), and
              - each task we expect to be ignored is absent.
        5. Clean up the test tasks and restore the user's real store.

    The scheduled tasks are built via the Schedule.Service COM API (the exact same API the app
    scans through), which sidesteps Task Scheduler XML element-ordering pitfalls and lets us set
    per-trigger Enabled / StartBoundary / EndBoundary / Repetition precisely.

.NOTES
    Run locally from an elevated PowerShell:
        pwsh -File tests/StartupScanner/Run-TriggerDetectionTests.ps1 -ExePath src/bin/Release/Thio-Background-App-Notifier.exe

    Local runs temporarily replace your real detections.json and restore it in a finally block.
#>

[CmdletBinding()]
param(
    # Path to the built app exe.
    [Parameter(Mandatory = $true)]
    [string]$ExePath,

    # Task Scheduler subfolder (under root) that holds the test tasks. Chosen to be unmistakably ours.
    [string]$TaskFolderName = 'ThioBanTest',

    # Where to drop the produced detections.json + results for CI artifact upload.
    [string]$ArtifactDir = (Join-Path $PSScriptRoot '_artifacts'),

    # Debug helpers: leave the test tasks / store in place after the run.
    [switch]$KeepTasks,
    [switch]$KeepStore
)

$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------------------------------------------
# Constants
# ----------------------------------------------------------------------------------------------------

# _TASK_TRIGGER_TYPE2 values (must match TaskScheduler COM enum used by StartupScanner.cs).
$TRIG = @{
    Event              = 0
    Time               = 1   # one-time
    Daily              = 2
    Weekly             = 3
    Monthly            = 4
    Idle               = 6
    Registration       = 7
    Boot               = 8
    Logon              = 9
    SessionStateChange = 11
}
$ACTION_EXEC        = 0
$ACTION_COM         = 5   # TASK_ACTION_COM_HANDLER
$LOGON_SERVICE      = 5   # TASK_LOGON_SERVICE_ACCOUNT (run as SYSTEM, no password, works for every trigger type)
$CREATE_OR_UPDATE   = 6   # TASK_CREATE_OR_UPDATE
$SESSION_UNLOCK     = 8   # TASK_SESSION_UNLOCK
$SYSTEM_SID         = 'S-1-5-18'

# Dummy CLSID for COM-handler actions. Task Scheduler stores the ClassId without verifying that it
# is a registered COM class, which is all these tests need (the task never has to actually run).
$COM_DUMMY_CLSID    = '{DEADBEEF-0000-4000-8000-00000000C0DE}'

$TaskFolderPath = '\' + $TaskFolderName            # e.g. \ThioBanTest
$KeyPrefix      = ('task:' + $TaskFolderPath + '\').ToLowerInvariant()   # detections.json IdentityKey prefix

$StoreDir  = Join-Path $env:APPDATA "ThioJoe\Thio's Background App Notifier"
$StorePath = Join-Path $StoreDir 'detections.json'
$StoreBak  = "$StorePath.thiotestbak"

# ----------------------------------------------------------------------------------------------------
# Time context for boundary-sensitive cases (local wall-clock ISO strings, no offset = local time,
# which is exactly how StartupScanner.TryParseBoundary treats an unqualified boundary).
# ----------------------------------------------------------------------------------------------------

$fmt    = 'yyyy-MM-ddTHH:mm:ss'
$fmtOff = 'yyyy-MM-ddTHH:mm:sszzz'                  # explicit +hh:mm offset form of the same instant
$now = Get-Date
$utc = $now.ToUniversalTime()
$ctx = @{
    DailyStart = $now.AddDays(-1).ToString($fmt)    # calendar triggers need a StartBoundary; past is fine
    Future1d   = $now.AddDays(1).ToString($fmt)
    Past10m    = $now.AddMinutes(-10).ToString($fmt)
    Past20m    = $now.AddMinutes(-20).ToString($fmt)
    Past40m    = $now.AddMinutes(-40).ToString($fmt)
    Past2h     = $now.AddHours(-2).ToString($fmt)
    PastStart  = '2020-01-01T00:00:00'
    PastEnd    = '2020-06-01T00:00:00'              # in the past -> trigger retired
    FarFuture  = $now.AddYears(10).ToString($fmt)

    # Same instants expressed in the other boundary spellings TryParseBoundary must handle:
    # UTC ('Z' suffix), explicit +hh:mm offset, and fractional seconds.
    PastStartZ      = '2020-01-01T00:00:00Z'
    PastEndZ        = '2020-06-01T00:00:00Z'
    PastEndOffset   = '2020-06-01T00:00:00+00:00'
    PastEndFrac     = '2020-06-01T00:00:00.123'
    FarFutureZ      = $utc.AddYears(10).ToString($fmt) + 'Z'
    FarFutureOffset = $now.AddYears(10).ToString($fmtOff)
    Past10mZ        = $utc.AddMinutes(-10).ToString($fmt) + 'Z'
    Past2hZ         = $utc.AddHours(-2).ToString($fmt) + 'Z'
    Past10mOffset   = $now.AddMinutes(-10).ToString($fmtOff)
}

# ----------------------------------------------------------------------------------------------------
# Test cases.
#
# Each case exercises a specific branch of StartupTask.GetAutoStartTypes / CheckRepititionInteval /
# IsRepetitionWindowLive / IsTriggerLive. "Build" configures a fresh task definition (a standard
# SYSTEM principal + "cmd.exe /c exit" exec action are already attached before Build runs).
#
# Expectation fields (all optional except Detect):
#   Detect            [bool]    - should this task appear in detections.json at all?
#   DetailEquals      [string]  - Detail must equal this exactly
#   DetailContains    [string[]]- Detail must contain each of these substrings
#   DetailNotContains [string[]]- Detail must contain none of these substrings
#   DetailContainsOnce[string[]]- Detail must contain each substring exactly once (proves .Distinct())
#   Subfolder         [string]  - register under \<TaskFolder>\<Subfolder> instead of the test root
#                                 (exercises ProcessTaskFolder's recursion into subfolders)
#   Skip / SkipReason - documented branch that Task Scheduler will not let us register (see bottom)
# ----------------------------------------------------------------------------------------------------

$cases = @(

    # ---- 1. Considered base trigger types -> detected with their friendly name --------------------
    @{ Name='C01_Logon'; Why='Logon trigger is an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C02_Boot'; Why='Boot trigger is an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Boot); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Boot' }

    @{ Name='C03_Idle'; Why='Idle trigger is an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Idle); $t.Enabled=$true };
       Detect=$true; DetailEquals='Whenever Idle' }

    @{ Name='C04_Daily1'; Why='Daily with DaysInterval=1 counts as normal "Daily"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=1 };
       Detect=$true; DetailEquals='Daily' }

    # ---- 2. Multi-day daily -> "Every N days", NOT "Daily" ----------------------------------------
    @{ Name='C05_Daily2'; Why='DaysInterval>1 is described as "Every N days" (otherDescriptions path)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=2 };
       Detect=$true; DetailEquals='Every 2 days'; DetailNotContains=@('Daily') }

    @{ Name='C06_Daily3'; Why='DaysInterval=3 -> "Every 3 days"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=3 };
       Detect=$true; DetailEquals='Every 3 days' }

    # ---- 3. Non-considered base types with no repetition -> ignored -------------------------------
    @{ Name='C07_Weekly'; Why='Weekly is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Weekly); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysOfWeek=2; $t.WeeksInterval=1 };
       Detect=$false }

    @{ Name='C08_Monthly'; Why='Monthly is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Monthly); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysOfMonth=1; $t.MonthsOfYear=4095 };
       Detect=$false }

    @{ Name='C09_OnceFuture'; Why='One-time trigger with no repetition is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Future1d };
       Detect=$false }

    @{ Name='C10_Event'; Why='Event trigger is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Event); $t.Enabled=$true;
               $t.Subscription='<QueryList><Query Id="0" Path="System"><Select Path="System">*[System[(Level=4 or Level=0)]]</Select></Query></QueryList>' };
       Detect=$false }

    @{ Name='C11_Registration'; Why='Registration trigger is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Registration); $t.Enabled=$true };
       Detect=$false }

    @{ Name='C12_SessionUnlock'; Why='Session-state-change (unlock) is not an autostart type';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.SessionStateChange); $t.Enabled=$true; $t.StateChange=$SESSION_UNLOCK };
       Detect=$false }

    @{ Name='C13_NoTriggers'; Why='A task with an exec action but zero triggers is not autostart';
       Build={ param($d,$c) };  # no triggers at all
       Detect=$false }

    # ---- 4. Enabled / disabled gating -------------------------------------------------------------
    @{ Name='C14_TriggerDisabled'; Why='trigger.Enabled=false is skipped';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$false };
       Detect=$false }

    @{ Name='C15_TaskDisabled'; Why='task.Enabled=false is skipped in ProcessTaskFolder';
       Build={ param($d,$c) $d.Settings.Enabled=$false; $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$false }

    @{ Name='C16_MixedDisabledEnabled'; Why='Only the enabled trigger of a mix is reported';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$false; $b=$d.Triggers.Create($TRIG.Boot); $b.Enabled=$true };
       Detect=$true; DetailEquals='At Boot'; DetailNotContains=@('At Logon') }

    # ---- 5. EndBoundary liveness (IsTriggerLive applies to EVERY trigger type) ---------------------
    @{ Name='C17_LogonExpiredEnd'; Why='EndBoundary in the past retires even a Logon trigger';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd };
       Detect=$false }

    @{ Name='C18_LogonFutureEnd'; Why='EndBoundary in the future is still live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.EndBoundary=$c.FarFuture };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C19_BootExpiredEnd'; Why='EndBoundary in the past retires a Boot trigger too';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Boot); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd };
       Detect=$false }

    # ---- 6. Repetition tacked onto a considered trigger -> extra "Repeats every X" ----------------
    @{ Name='C20_DailyRepeatHourly'; Why='Chrome-updater shape: daily schedule that also repeats hourly';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=1; $t.Repetition.Interval='PT1H' };
       Detect=$true; DetailContains=@('Daily','Repeats every 1 hour') }

    @{ Name='C21_LogonRepeat30m'; Why='Logon trigger repeating every 30 minutes';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT30M' };
       Detect=$true; DetailContains=@('At Logon','Repeats every 30 minutes') }

    @{ Name='C22_BootRepeat15m'; Why='Boot trigger repeating every 15 minutes';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Boot); $t.Enabled=$true; $t.Repetition.Interval='PT15M' };
       Detect=$true; DetailContains=@('At Boot','Repeats every 15 minutes') }

    # ---- 7. Repetition on a NON-considered trigger is what makes it detectable ---------------------
    #     IsRepetitionWindowLive: ongoing / not-yet-started / already-ended for a one-shot TIME trigger.
    @{ Name='C23_OnceRepeatOngoing'; Why='One-time trigger, window currently open -> detected via repetition only';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past10m; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C24_OnceRepeatFuture'; Why='One-time trigger, window entirely in the future -> still live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Future1d; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C25_OnceRepeatDeadWindow'; Why='One-time trigger whose repetition window already closed -> ignored';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past2h; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$false }

    @{ Name='C26_WeeklyRepeatHourly'; Why='Weekly base is not named, but its hourly repetition is detected';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Weekly); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysOfWeek=2; $t.WeeksInterval=1; $t.Repetition.Interval='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 hour' }

    # ---- 8. Friendly repeat-string formatting / rounding (MakeFriendlyRepeatString) ----------------
    @{ Name='C27_Repeat24h'; Why='24h interval renders as "24 hours", not "1 day" (TotalDays==1 falls to the hours branch)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT24H' };
       Detect=$true; DetailContains=@('Repeats every 24 hours') }

    @{ Name='C28_Repeat2d'; Why='Multi-day interval renders in days';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='P2D' };
       Detect=$true; DetailContains=@('Repeats every 2 days') }

    @{ Name='C29_Repeat90m'; Why='90-minute interval renders as rounded "1.5 hours"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT1H30M' };
       Detect=$true; DetailContains=@('Repeats every 1.5 hours') }

    @{ Name='C30_Repeat1m'; Why='1-minute interval renders singular "1 minute"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT1M' };
       Detect=$true; DetailContains=@('Repeats every 1 minute') }

    # ---- 9. Combinations / distinctness -----------------------------------------------------------
    @{ Name='C31_BootAndLogon'; Why='Two considered triggers on one task -> both reported';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Boot); $a.Enabled=$true; $b=$d.Triggers.Create($TRIG.Logon); $b.Enabled=$true };
       Detect=$true; DetailContains=@('At Boot','At Logon') }

    @{ Name='C32_DistinctRepeat'; Why='Duplicate repeat descriptions are collapsed by .Distinct()';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$true; $a.Repetition.Interval='PT1H'; $b=$d.Triggers.Create($TRIG.Boot); $b.Enabled=$true; $b.Repetition.Interval='PT1H' };
       Detect=$true; DetailContains=@('At Logon','At Boot'); DetailContainsOnce=@('Repeats every 1 hour') }

    # ================================================================================================
    # Extended edge-case suite (C33+). Each case pins one boundary of GetAutoStartTypes /
    # CheckRepititionInteval / IsRepetitionWindowLive / IsTriggerLive / TryParseBoundary /
    # MakeFriendlyRepeatString, or one structural behavior of ProcessTaskFolder.
    # ================================================================================================

    # ---- 10. Boundary string formats (TryParseBoundary must normalize all of these to local time) --
    @{ Name='C33_LogonEndZPast'; Why='UTC (Z-suffixed) EndBoundary in the past retires the trigger (UTC branch of TryParseBoundary)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.StartBoundary=$c.PastStartZ; $t.EndBoundary=$c.PastEndZ };
       Detect=$false }

    @{ Name='C34_LogonEndZFuture'; Why='UTC (Z-suffixed) EndBoundary in the future is still live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.EndBoundary=$c.FarFutureZ };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C35_LogonEndOffsetPast'; Why='Explicit +hh:mm-offset EndBoundary in the past retires the trigger (offset branch of TryParseBoundary)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEndOffset };
       Detect=$false }

    @{ Name='C36_LogonEndOffsetFuture'; Why='Explicit-offset EndBoundary in the future is still live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.EndBoundary=$c.FarFutureOffset };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C37_LogonEndFracSecPast'; Why='Fractional-seconds EndBoundary still parses and retires the trigger';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEndFrac };
       Detect=$false }

    @{ Name='C38_TimeWindowZOngoing'; Why='One-shot repetition window judged correctly when StartBoundary is UTC (Z): still open';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past10mZ; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C39_TimeWindowZClosed'; Why='One-shot repetition window judged correctly when StartBoundary is UTC (Z): already closed';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past2hZ; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$false }

    @{ Name='C40_TimeWindowOffsetOngoing'; Why='One-shot repetition window judged correctly with an explicit-offset StartBoundary';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past10mOffset; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    # ---- 11. EndBoundary retirement sweeps every path that could resurrect a trigger ---------------
    @{ Name='C41_IdleExpiredEnd'; Why='EndBoundary in the past retires an Idle trigger';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Idle); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd };
       Detect=$false }

    @{ Name='C42_DailyExpiredEnd'; Why='EndBoundary in the past retires a Daily trigger before the daily branch runs';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd; $t.DaysInterval=1 };
       Detect=$false }

    @{ Name='C43_Daily2ExpiredEnd'; Why='EndBoundary in the past retires a multi-day Daily trigger (the "Every N days" path is gated too)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd; $t.DaysInterval=2 };
       Detect=$false }

    @{ Name='C44_LogonRepeatExpiredEnd'; Why='A retired trigger reports neither its base type NOR its repetition';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd; $t.Repetition.Interval='PT1H' };
       Detect=$false }

    @{ Name='C45_TimeRepeatExpiredEnd'; Why='Even indefinite (no-Duration) repetition dies with its retired one-shot trigger';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.EndBoundary=$c.PastEnd; $t.Repetition.Interval='PT1M' };
       Detect=$false }

    @{ Name='C46_MixedLiveExpired'; Why='Per-trigger liveness: an expired Boot sibling neither hides nor leaks into the live Logon';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Boot); $a.Enabled=$true; $a.StartBoundary=$c.PastStart; $a.EndBoundary=$c.PastEnd; $b=$d.Triggers.Create($TRIG.Logon); $b.Enabled=$true };
       Detect=$true; DetailEquals='At Logon'; DetailNotContains=@('At Boot') }

    # ---- 12. Repetition-window boundaries (CheckRepititionInteval / IsRepetitionWindowLive) --------
    @{ Name='C47_OnceRepeatNoDuration'; Why='No Duration = repeat forever: a years-old one-shot with indefinite repetition is still live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.PastStart; $t.Repetition.Interval='PT1M' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C48_OnceFutureNoDuration'; Why='Duration-absent path skips the window check entirely, even for a future one-shot';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Future1d; $t.Repetition.Interval='PT2M' };
       Detect=$true; DetailEquals='Repeats every 2 minutes' }

    @{ Name='C49_WeeklyStaleWindow'; Why='Recurring (non-TIME) trigger reopens its window: the exact numbers that kill C25 stay live on a Weekly';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Weekly); $t.Enabled=$true; $t.StartBoundary=$c.Past2h; $t.DaysOfWeek=2; $t.WeeksInterval=1; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C50_OnceWindowJustOpen'; Why='One-shot window still open with only ~10 minutes to spare';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past20m; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C51_OnceWindowJustClosed'; Why='One-shot window that closed only ~10 minutes ago is already dead';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past40m; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$false }

    @{ Name='C52_DurationEqualsInterval'; Why='Duration == Interval is NOT dropped (the guard is strictly Duration < Interval)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past10m; $t.Repetition.Interval='PT30M'; $t.Repetition.Duration='PT30M' };
       Detect=$true; DetailEquals='Repeats every 30 minutes' }

    @{ Name='C53_DurationDayFormat'; Why='Day-denominated Duration (P1D) parses; window spans nearly a day so it is open';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past2h; $t.Repetition.Interval='PT1H'; $t.Repetition.Duration='P1D'; $t.Repetition.StopAtDurationEnd=$true };
       Detect=$true; DetailEquals='Repeats every 1 hour' }

    @{ Name='C54_DeadWindowFutureEnd'; Why='A closed one-shot window stays dead even with a far-future EndBoundary (window closure, not retirement, is what kills it)';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.Past2h; $t.EndBoundary=$c.FarFuture; $t.Repetition.Interval='PT1M'; $t.Repetition.Duration='PT30M' };
       Detect=$false }

    @{ Name='C55_IdleRepeatWithDuration'; Why='Considered trigger + windowed repetition: non-TIME type keeps the window live';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Idle); $t.Enabled=$true; $t.Repetition.Interval='PT10M'; $t.Repetition.Duration='PT1H' };
       Detect=$true; DetailEquals='Whenever Idle, Repeats every 10 minutes' }

    # ---- 13. Repetition rescues every remaining non-considered trigger type ------------------------
    @{ Name='C56_EventRepeat'; Why='Event trigger becomes detectable purely via its repetition';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Event); $t.Enabled=$true;
               $t.Subscription='<QueryList><Query Id="0" Path="System"><Select Path="System">*[System[(Level=4 or Level=0)]]</Select></Query></QueryList>';
               $t.Repetition.Interval='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 hour' }

    @{ Name='C57_UnlockRepeat'; Why='Session-unlock trigger becomes detectable purely via its repetition';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.SessionStateChange); $t.Enabled=$true; $t.StateChange=$SESSION_UNLOCK; $t.Repetition.Interval='PT45M' };
       Detect=$true; DetailEquals='Repeats every 45 minutes' }

    @{ Name='C58_RegistrationRepeat'; Why='Registration trigger becomes detectable purely via its repetition';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Registration); $t.Enabled=$true; $t.Repetition.Interval='PT1H' };
       Detect=$true; DetailEquals='Repeats every 1 hour' }

    @{ Name='C59_MonthlyRepeat'; Why='Monthly trigger becomes detectable purely via its repetition';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Monthly); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysOfMonth=1; $t.MonthsOfYear=4095; $t.Repetition.Interval='PT20M' };
       Detect=$true; DetailEquals='Repeats every 20 minutes' }

    # ---- 14. MakeFriendlyRepeatString unit-branch flips, rounding, and pluralization ---------------
    #      The days/hours/minutes branches flip at >24h and >=1h; two cases below pin current
    #      pluralization quirks (rounding lands on "1" but the unrounded value pluralizes).
    @{ Name='C60_Repeat23h'; Why='23h stays in the hours branch, just below the days flip';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT23H' };
       Detect=$true; DetailEquals='At Logon, Repeats every 23 hours' }

    @{ Name='C61_Repeat25hQuirk'; Why='25h flips to the days branch; 1.04 rounds to "1" but still pluralizes (current behavior: "1 days")';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT25H' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1 days' }

    @{ Name='C62_Repeat26h'; Why='26h renders as rounded "1.1 days"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT26H' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1.1 days' }

    @{ Name='C63_Repeat36h'; Why='Mixed day+hour spelling (P1DT12H) renders as "1.5 days"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='P1DT12H' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1.5 days' }

    @{ Name='C64_Repeat48hAsHours'; Why='48h spelled in hours still lands in the days branch: unit spelling does not matter';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT48H' };
       Detect=$true; DetailEquals='At Logon, Repeats every 2 days' }

    @{ Name='C65_Repeat61mQuirk'; Why='61m flips to the hours branch; 1.02 rounds to "1" but still pluralizes (current behavior: "1 hours")';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT61M' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1 hours' }

    @{ Name='C66_Repeat59m'; Why='59m stays in the minutes branch, just below the hours flip';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT59M' };
       Detect=$true; DetailEquals='At Logon, Repeats every 59 minutes' }

    @{ Name='C67_Repeat60mIsOneHour'; Why='Exactly 60m lands in the hours branch as singular "1 hour"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT60M' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1 hour' }

    @{ Name='C68_Repeat105m'; Why='105m (1.75h) rounds up to "1.8 hours"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT1H45M' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1.8 hours' }

    @{ Name='C69_Repeat90s'; Why='Seconds-granular interval (PT1M30S, above the 1-minute floor) renders as rounded "1.5 minutes"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true; $t.Repetition.Interval='PT1M30S' };
       Detect=$true; DetailEquals='At Logon, Repeats every 1.5 minutes' }

    # ---- 15. Multi-trigger combinations: ordering, dedup, and non-interference ---------------------
    @{ Name='C70_TwoLogonsDedup'; Why='Two Logon triggers collapse to a single "At Logon" (.Distinct() on base names)';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$true; $b=$d.Triggers.Create($TRIG.Logon); $b.Enabled=$true };
       Detect=$true; DetailEquals='At Logon'; DetailContainsOnce=@('At Logon') }

    @{ Name='C71_TwoDailyIntervals'; Why='Different multi-day intervals are both reported';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Daily); $a.Enabled=$true; $a.StartBoundary=$c.DailyStart; $a.DaysInterval=2; $b=$d.Triggers.Create($TRIG.Daily); $b.Enabled=$true; $b.StartBoundary=$c.DailyStart; $b.DaysInterval=3 };
       Detect=$true; DetailEquals='Every 2 days, Every 3 days' }

    @{ Name='C72_DupEveryNDaysDedup'; Why='Identical "Every N days" descriptions are collapsed by .Distinct()';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Daily); $a.Enabled=$true; $a.StartBoundary=$c.DailyStart; $a.DaysInterval=2; $b=$d.Triggers.Create($TRIG.Daily); $b.Enabled=$true; $b.StartBoundary=$c.DailyStart; $b.DaysInterval=2 };
       Detect=$true; DetailEquals='Every 2 days'; DetailContainsOnce=@('Every 2 days') }

    @{ Name='C73_TwoRepeatIntervals'; Why='Different repeat intervals on different triggers are both reported, after the base names';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$true; $a.Repetition.Interval='PT1H'; $b=$d.Triggers.Create($TRIG.Boot); $b.Enabled=$true; $b.Repetition.Interval='PT30M' };
       Detect=$true; DetailEquals='At Logon, At Boot, Repeats every 1 hour, Repeats every 30 minutes' }

    @{ Name='C74_SpellingDedup'; Why='PT60M and PT1H normalize to the same friendly string and dedup to one entry';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$true; $a.Repetition.Interval='PT60M'; $b=$d.Triggers.Create($TRIG.Boot); $b.Enabled=$true; $b.Repetition.Interval='PT1H' };
       Detect=$true; DetailEquals='At Logon, At Boot, Repeats every 1 hour'; DetailContainsOnce=@('Repeats every 1 hour') }

    @{ Name='C75_NamesBeforeRepeats'; Why='Base-type names always precede repeat strings, regardless of trigger creation order';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Weekly); $a.Enabled=$true; $a.StartBoundary=$c.DailyStart; $a.DaysOfWeek=2; $a.WeeksInterval=1; $a.Repetition.Interval='PT1H'; $b=$d.Triggers.Create($TRIG.Logon); $b.Enabled=$true };
       Detect=$true; DetailEquals='At Logon, Repeats every 1 hour' }

    @{ Name='C76_MultiDayDailyWithRep'; Why='A single trigger can contribute both "Every N days" AND its repetition string';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=2; $t.Repetition.Interval='PT1H' };
       Detect=$true; DetailEquals='Every 2 days, Repeats every 1 hour' }

    @{ Name='C77_DisabledRepNoLeak'; Why='A disabled trigger''s repetition does not leak into the report';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Logon); $a.Enabled=$true; $b=$d.Triggers.Create($TRIG.Boot); $b.Enabled=$false; $b.Repetition.Interval='PT1M' };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C78_ExpiredPlusLiveRepeat'; Why='A retired Logon sibling does not suppress a live repetition-only detection';
       Build={ param($d,$c) $a=$d.Triggers.Create($TRIG.Time); $a.Enabled=$true; $a.StartBoundary=$c.Past10m; $a.Repetition.Interval='PT1M'; $a.Repetition.Duration='PT1H'; $b=$d.Triggers.Create($TRIG.Logon); $b.Enabled=$true; $b.StartBoundary=$c.PastStart; $b.EndBoundary=$c.PastEnd };
       Detect=$true; DetailEquals='Repeats every 1 minute' }

    @{ Name='C79_KitchenSink'; Why='Disabled + expired + dead-window triggers all filtered while live Daily/Weekly repetitions survive';
       Build={ param($d,$c)
               $t1=$d.Triggers.Create($TRIG.Logon);  $t1.Enabled=$false
               $t2=$d.Triggers.Create($TRIG.Boot);   $t2.Enabled=$true; $t2.StartBoundary=$c.PastStart; $t2.EndBoundary=$c.PastEnd
               $t3=$d.Triggers.Create($TRIG.Daily);  $t3.Enabled=$true; $t3.StartBoundary=$c.DailyStart; $t3.DaysInterval=1; $t3.Repetition.Interval='PT1H'
               $t4=$d.Triggers.Create($TRIG.Weekly); $t4.Enabled=$true; $t4.StartBoundary=$c.DailyStart; $t4.DaysOfWeek=2; $t4.WeeksInterval=1; $t4.Repetition.Interval='PT30M'
               $t5=$d.Triggers.Create($TRIG.Time);   $t5.Enabled=$true; $t5.StartBoundary=$c.Past2h; $t5.Repetition.Interval='PT1M'; $t5.Repetition.Duration='PT30M' };
       Detect=$true; DetailEquals='Daily, Repeats every 1 hour, Repeats every 30 minutes';
       DetailNotContains=@('At Logon','At Boot','Repeats every 1 minute') }

    # ---- 16. Scanner mechanics: enumeration, folders, actions, trigger metadata --------------------
    @{ Name='C80_HiddenTask'; Why='Hidden tasks are still scanned (GetTasks(TASK_ENUM_HIDDEN))';
       Build={ param($d,$c) $d.Settings.Hidden=$true; $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C81_NestedFolder'; Why='ProcessTaskFolder recurses into subfolders'; Subfolder='Nested';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C82_ComActionOnly'; Why='A task whose only action is a COM handler (no exec action) is not reported';
       Build={ param($d,$c) $d.Actions.Clear(); $a=$d.Actions.Create($ACTION_COM); $a.ClassId=$COM_DUMMY_CLSID; $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$false }

    @{ Name='C83_ComPlusExecAction'; Why='The exec-action check scans past a leading COM action to find the exec action';
       Build={ param($d,$c) $d.Actions.Clear(); $a=$d.Actions.Create($ACTION_COM); $a.ClassId=$COM_DUMMY_CLSID; $e=$d.Actions.Create($ACTION_EXEC); $e.Path='cmd.exe'; $e.Arguments='/c exit'; $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C84_TwoExecActions'; Why='A task with multiple exec actions is reported exactly once';
       Build={ param($d,$c) $e=$d.Actions.Create($ACTION_EXEC); $e.Path='cmd.exe'; $e.Arguments='/c ver'; $t=$d.Triggers.Create($TRIG.Logon); $t.Enabled=$true };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C85_DefaultEnabled'; Why='A trigger created without touching Enabled defaults to enabled and is reported';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Logon) };
       Detect=$true; DetailEquals='At Logon' }

    @{ Name='C86_BootDelay'; Why='A boot Delay does not change the classification';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Boot); $t.Enabled=$true; $t.Delay='PT5M' };
       Detect=$true; DetailEquals='At Boot' }

    @{ Name='C87_DailyRandomDelay'; Why='A RandomDelay does not change the classification';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=1; $t.RandomDelay='PT1H' };
       Detect=$true; DetailEquals='Daily' }

    @{ Name='C88_DailyFutureStart'; Why='A Daily trigger whose StartBoundary is still in the future already counts as autostart';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.Future1d; $t.DaysInterval=1 };
       Detect=$true; DetailEquals='Daily' }

    @{ Name='C89_Daily365'; Why='Large DaysInterval renders verbatim as "Every 365 days"';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Daily); $t.Enabled=$true; $t.StartBoundary=$c.DailyStart; $t.DaysInterval=365 };
       Detect=$true; DetailEquals='Every 365 days' }

    @{ Name='C90_OncePastNoRep'; Why='A stale one-shot with no repetition is not autostart via any path';
       Build={ param($d,$c) $t=$d.Triggers.Create($TRIG.Time); $t.Enabled=$true; $t.StartBoundary=$c.PastStart };
       Detect=$false }

    # ---- Documented, deliberately-unregisterable defensive branches -------------------------------
    #   Task Scheduler itself rejects these at registration, so they can't be covered by a live task.
    #   They are pure defensive guards in CheckRepititionInteval / MakeFriendlyRepeatString /
    #   TryParseBoundary / IsTriggerLive.
    @{ Name='S01_DurationLtInterval'; Skip=$true;
       SkipReason='CheckRepititionInteval drops repetition when Duration < Interval, but Task Scheduler refuses to register interval > duration, so no live task can reach that guard.' }

    @{ Name='S02_SubMinuteInterval'; Skip=$true;
       SkipReason='MakeFriendlyRepeatString has a seconds branch, but Task Scheduler enforces a 1-minute minimum repetition interval, so a sub-minute interval cannot be registered.' }

    @{ Name='S03_MalformedBoundary'; Skip=$true;
       SkipReason='TryParseBoundary has parse-failure fallbacks (assume live / assume window open), but the Task Scheduler service validates boundary date formats at registration, so a live task cannot carry a malformed StartBoundary/EndBoundary.' }

    @{ Name='S04_UnreadableTriggerProps'; Skip=$true;
       SkipReason='The catch-paths around COM property reads (EndBoundary / StartBoundary / Repetition throwing) assume live / no-repetition, but a healthy registered task never throws on those reads, so they are unreachable end-to-end.' }
)

# ----------------------------------------------------------------------------------------------------
# Helpers
# ----------------------------------------------------------------------------------------------------

function Write-Section([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

function New-BaseDefinition($svc) {
    $d = $svc.NewTask(0)
    $d.RegistrationInfo.Description = 'Thio BAN trigger-detection test task (safe to delete)'
    $d.Principal.UserId    = $SYSTEM_SID
    $d.Principal.LogonType = $LOGON_SERVICE
    $d.Principal.RunLevel  = 0
    $d.Settings.Enabled                    = $true
    $d.Settings.StartWhenAvailable         = $true
    $d.Settings.AllowDemandStart           = $true
    $d.Settings.DisallowStartIfOnBatteries = $false
    $d.Settings.StopIfGoingOnBatteries     = $false
    $d.Settings.ExecutionTimeLimit         = 'PT1H'
    $a = $d.Actions.Create($ACTION_EXEC)
    $a.Path      = 'cmd.exe'
    $a.Arguments = '/c exit'
    return $d
}

function Get-DetailOccurrences([string]$haystack, [string]$needle) {
    return ([regex]::Matches($haystack, [regex]::Escape($needle))).Count
}

# Deletes every task in the given Task Scheduler folder and all of its subfolders (depth-first),
# leaving the top folder itself for the caller to delete. Snapshots each COM collection into an
# array before deleting so we never mutate a collection while enumerating it.
function Remove-TestTaskFolderTree($scheduleService, [string]$folderPath) {
    try { $f = $scheduleService.GetFolder($folderPath) } catch { return }
    foreach ($t in @($f.GetTasks(1))) { $f.DeleteTask($t.Name, 0) }
    foreach ($sub in @($f.GetFolders(0))) {
        Remove-TestTaskFolderTree $scheduleService $sub.Path
        $f.DeleteFolder($sub.Name, 0)
    }
}

# ----------------------------------------------------------------------------------------------------
# Main
# ----------------------------------------------------------------------------------------------------

$ExePath = (Resolve-Path -LiteralPath $ExePath).Path
if (-not (Test-Path -LiteralPath $ExePath)) { throw "App exe not found: $ExePath" }

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

$svc = New-Object -ComObject Schedule.Service
$svc.Connect()
$root = $svc.GetFolder('\')

$results = New-Object System.Collections.Generic.List[object]
$storeBackedUp = $false

try {
    # -- Preserve the user's real store, then start from a clean baseline --------------------------
    # On a fresh CI runner there is no store, so this is a no-op and the first --quiet run creates
    # the baseline on its own. We still do it as a guard: it keeps the suite re-runnable, stops a
    # leftover store (re-used runner / earlier run) from making our test tasks look "new" and popping
    # the blocking quiet-mode dialog, and keeps a LOCAL run from clobbering the developer's real store.
    Write-Section 'Preparing clean baseline store'
    if (Test-Path -LiteralPath $StorePath) {
        Copy-Item -LiteralPath $StorePath -Destination $StoreBak -Force
        $storeBackedUp = $true
        Write-Host "Backed up existing store -> $StoreBak"
    }
    Remove-Item -LiteralPath $StorePath -Force -ErrorAction SilentlyContinue

    # -- (Re)create a pristine test folder --------------------------------------------------------
    Write-Section "Registering test tasks under $TaskFolderPath"
    try { $existing = $svc.GetFolder($TaskFolderPath) } catch { $existing = $null }
    if ($existing) {
        Remove-TestTaskFolderTree $svc $TaskFolderPath
        $root.DeleteFolder($TaskFolderPath, 0)
    }
    $folder = $root.CreateFolder($TaskFolderPath)

    # -- Register each (non-skipped) case ---------------------------------------------------------
    foreach ($case in $cases) {
        if ($case.Skip) {
            $results.Add([pscustomobject]@{ Name=$case.Name; Status='SKIP'; Detail=''; Message=$case.SkipReason })
            Write-Host ("  [skip] {0} - {1}" -f $case.Name, $case.SkipReason) -ForegroundColor DarkGray
            continue
        }
        try {
            $def = New-BaseDefinition $svc
            & $case.Build $def $ctx
            $targetFolder = $folder
            if ($case.Subfolder) {
                $subPath = $TaskFolderPath + '\' + $case.Subfolder
                try   { $targetFolder = $svc.GetFolder($subPath) }
                catch { $targetFolder = $folder.CreateFolder($case.Subfolder) }
            }
            $targetFolder.RegisterTaskDefinition($case.Name, $def, $CREATE_OR_UPDATE, $SYSTEM_SID, $null, $LOGON_SERVICE, $null) | Out-Null
            Write-Host ("  [ok]   {0}" -f $case.Name) -ForegroundColor DarkGreen
        }
        catch {
            # A registration failure for a case we intended to register is itself a test failure.
            $results.Add([pscustomobject]@{ Name=$case.Name; Status='ERROR'; Detail=''; Message="Registration failed: $($_.Exception.Message)" })
            Write-Host ("  [ERR]  {0} - registration failed: {1}" -f $case.Name, $_.Exception.Message) -ForegroundColor Red
        }
    }

    # -- Run the scanner (baseline, quiet: writes the store and exits with no UI) ------------------
    # Use System.Diagnostics.Process directly (not Start-Process) so we keep the handle and can both
    # wait with a timeout and read ExitCode reliably. The 60s timeout is a safety net: a baseline
    # quiet run never shows UI, so anything that blocks is a regression worth failing on.
    Write-Section 'Running scanner (--quiet baseline)'
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName        = $ExePath
    $psi.Arguments       = '--quiet'
    $psi.UseShellExecute = $false
    $proc = [System.Diagnostics.Process]::Start($psi)
    if (-not $proc.WaitForExit(60000)) {
        try { $proc.Kill() } catch {}
        throw "App did not exit within 60s under --quiet (unexpected UI on a baseline run?)."
    }
    Write-Host "App exited with code $($proc.ExitCode)."

    if (-not (Test-Path -LiteralPath $StorePath)) {
        throw "detections.json was not created at $StorePath - the scan produced no store."
    }

    # Preserve the produced store for CI artifacts before we touch anything else.
    Copy-Item -LiteralPath $StorePath -Destination (Join-Path $ArtifactDir 'detections.json') -Force

    # -- Parse the store and index our test items --------------------------------------------------
    Write-Section 'Evaluating detections.json'
    $data  = Get-Content -LiteralPath $StorePath -Raw | ConvertFrom-Json
    $items = @($data.Items)

    $found = @{}   # lower-case task name -> Detail
    foreach ($it in $items) {
        if ($it.IdentityKey -and $it.IdentityKey.ToLowerInvariant().StartsWith($KeyPrefix)) {
            $name = $it.IdentityKey.Substring($KeyPrefix.Length)   # already lower-case
            $found[$name] = [string]$it.Detail
        }
    }
    Write-Host ("Store lists {0} total item(s); {1} belong to our test folder." -f $items.Count, $found.Count)

    # -- Assert each non-skipped case --------------------------------------------------------------
    foreach ($case in $cases) {
        if ($case.Skip) { continue }
        if ($results | Where-Object { $_.Name -eq $case.Name -and $_.Status -eq 'ERROR' }) { continue } # already failed to register

        $key       = if ($case.Subfolder) { ($case.Subfolder + '\' + $case.Name).ToLowerInvariant() } else { $case.Name.ToLowerInvariant() }
        $wasFound  = $found.ContainsKey($key)
        $detail    = if ($wasFound) { $found[$key] } else { '' }
        $problems  = New-Object System.Collections.Generic.List[string]

        if ($case.Detect -and -not $wasFound) {
            $problems.Add('expected to be DETECTED but is absent from the store')
        }
        elseif (-not $case.Detect -and $wasFound) {
            $problems.Add("expected to be IGNORED but was detected (Detail='$detail')")
        }
        elseif ($case.Detect -and $wasFound) {
            if ($case.ContainsKey('DetailEquals') -and $detail -cne $case.DetailEquals) {
                $problems.Add("Detail should equal '$($case.DetailEquals)' but was '$detail'")
            }
            if ($case.ContainsKey('DetailContains')) {
                foreach ($s in $case.DetailContains) {
                    if ($detail -cnotmatch [regex]::Escape($s)) { $problems.Add("Detail should contain '$s' but was '$detail'") }
                }
            }
            if ($case.ContainsKey('DetailNotContains')) {
                foreach ($s in $case.DetailNotContains) {
                    if ($detail -cmatch [regex]::Escape($s)) { $problems.Add("Detail should NOT contain '$s' but was '$detail'") }
                }
            }
            if ($case.ContainsKey('DetailContainsOnce')) {
                foreach ($s in $case.DetailContainsOnce) {
                    $n = Get-DetailOccurrences $detail $s
                    if ($n -ne 1) { $problems.Add("Detail should contain '$s' exactly once but found $n time(s): '$detail'") }
                }
            }
        }

        if ($problems.Count -eq 0) {
            $results.Add([pscustomobject]@{ Name=$case.Name; Status='PASS'; Detail=$detail; Message=$case.Why })
        }
        else {
            $results.Add([pscustomobject]@{ Name=$case.Name; Status='FAIL'; Detail=$detail; Message=($problems -join '; ') })
        }
    }
}
finally {
    # -- Clean up test tasks (unless asked to keep them) ------------------------------------------
    if (-not $KeepTasks) {
        try {
            Remove-TestTaskFolderTree $svc $TaskFolderPath
            $root.DeleteFolder($TaskFolderPath, 0)
            Write-Host "`nRemoved test task folder $TaskFolderPath"
        } catch { Write-Host "`nNote: could not fully remove $TaskFolderPath - $($_.Exception.Message)" -ForegroundColor Yellow }
    }

    # -- Restore the user's real store (unless asked to keep the test one) -------------------------
    if (-not $KeepStore) {
        if ($storeBackedUp) {
            Move-Item -LiteralPath $StoreBak -Destination $StorePath -Force
            Write-Host "Restored original detection store."
        }
        elseif (Test-Path -LiteralPath $StorePath) {
            Remove-Item -LiteralPath $StorePath -Force -ErrorAction SilentlyContinue
        }
    }
}

# ----------------------------------------------------------------------------------------------------
# Report
# ----------------------------------------------------------------------------------------------------

Write-Section 'Results'
$order = @{ FAIL=0; ERROR=1; PASS=2; SKIP=3 }
$results |
    Sort-Object @{ Expression = { $order[$_.Status] } }, Name |
    Format-Table -AutoSize Name, Status, Detail, Message | Out-String -Width 4096 | Write-Host

$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $ArtifactDir 'results.json') -Encoding UTF8

$pass  = ($results | Where-Object Status -eq 'PASS').Count
$fail  = ($results | Where-Object Status -eq 'FAIL').Count
$err   = ($results | Where-Object Status -eq 'ERROR').Count
$skip  = ($results | Where-Object Status -eq 'SKIP').Count

Write-Host ("`nPASS={0}  FAIL={1}  ERROR={2}  SKIP(documented)={3}" -f $pass, $fail, $err, $skip) `
    -ForegroundColor $(if ($fail + $err -gt 0) { 'Red' } else { 'Green' })

if ($fail + $err -gt 0) { exit 1 } else { exit 0 }
