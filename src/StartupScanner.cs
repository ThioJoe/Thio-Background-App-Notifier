// StartupScanner.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using TaskScheduler;
using static Thio_Background_App_Notifier.StartupTask;

#nullable enable

namespace Thio_Background_App_Notifier
{
    public enum StartupItemType
    {
        Service,
        ScheduledTask
    }

    public interface IStartupItem
    {
        string Name { get; }
        string Path { get; }
        StartupItemType Type { get; } // "Service" or "ScheduledTask"

        /// <summary>
        /// A stable string that uniquely identifies this startup item across runs.
        /// Used by the detection log to decide whether an item is genuinely new.
        /// </summary>
        string IdentityKey { get; }

        /// <summary>
        /// The current run is the first time this startup item is detected.
        /// </summary>
        bool IsFirstDetection { get; set; }
        DateTime FirstDetectionTime { get; set; }

        /// <summary>
        /// A list of dictionaries, each representing a column header and contents, for additional columns and data to show for specific startup types.
        /// </summary>
        List<Dictionary<string, string>> TypeSpecificDetails { get; set; }
    }

    public class StartupService : IStartupItem
    {
        enum ServiceStartType: int
        {
            Boot = 0,   // Only for device Drivers
            System = 1, // Only for device drivers
            Automatic = 2, // Also includes delayed start
            Manual = 3,
            Disabled = 4
        }

        public string Name { get; init; }
        public string ServiceName { get; init; } // The (unique) registry classIdkey name of the service
        public string ExecPath { get; init; }
        public string RegPath { get; init; }
        public int StartupType { get; init; }
        public int ServiceType { get; init; } // Registry "Type" value (driver vs Win32 service)
        public StartupItemType Type { get; } = StartupItemType.Service;

        /// <summary>
        /// True when this registry entry is a driver rather than a normal Win32 service.
        /// Driver type values: 1 = kernel, 2 = file system, 4 = adapter, 8 = recognizer
        /// (Win32 services are 16 / 32).
        /// </summary>
        public bool IsDriver => ServiceType == 1 || ServiceType == 2 || ServiceType == 4 || ServiceType == 8;

        public bool IsFirstDetection { get; set; }
        public DateTime FirstDetectionTime { get; set; }

        // Explicit interface implementation - maps to the existing ExecPath property
        string IStartupItem.Path => ExecPath;

        public List<Dictionary<string, string>> TypeSpecificDetails { get; set; } = []; // None at this time

        /// <summary>
        /// Identify a service by its executable path (per the design notes: track the ImagePath,
        /// not just the service name, so re-registering apps aren't seen as new every boot).
        /// Falls back to the registry classIdkey name if no path is available.
        /// </summary>
        public string IdentityKey
        {
            get
            {
                string normalizedPath = Utils.NormalizePathForKey(ExecPath);
                if (!string.IsNullOrEmpty(normalizedPath))
                    return "svc:" + normalizedPath;

                return "svc-name:" + (ServiceName ?? string.Empty).ToLowerInvariant();
            }
        }

        // Constructor
        public StartupService(string rawNameString, string serviceName, string path, string regPath, int startType, int serviceType)
        {
            Name = Utils.DeriveFriendlyName(rawNameString);
            ServiceName = serviceName;
            ExecPath = path;
            RegPath = regPath;
            StartupType = startType;
            ServiceType = serviceType;
        }

    }

    public class StartupTask : IStartupItem
    {
        private IRegisteredTask _taskObj;
        private XDocument? _XmlObj;

        public string Name { get; set; }
        public string TaskSchedulerPath { get; set; }
        public List<string> TriggerDescription { get; set; }
        public string TaskXml { get; init; } = string.Empty;
        public StartupItemType Type { get; } = StartupItemType.ScheduledTask;
        public bool IsFirstDetection { get; set; }
        public DateTime FirstDetectionTime { get; set; }

        // Explicit interface implementation - joins the exec action paths (with args) used to start the task
        string IStartupItem.Path => string.Join("; ", ExecActionPathsWithArgs);

        /// <summary>
        /// Identify a scheduled task by its full Task Scheduler path (folder + name), which is unique
        /// and stable across runs.
        /// </summary>
        public string IdentityKey => "task:" + (TaskSchedulerPath ?? string.Empty).ToLowerInvariant();

        public List<string> StartupTaskTypes { get; init; }
        public List<string> Triggers { get; init; }
        public List<IExecAction2> ExecActions { get; init; }
        public List<string> ExecActionPaths { get; init; }
        public List<string> ExecActionPathsWithArgs { get; init; }
        public List<_TASK_ACTION_TYPE> ActionTypes { get; init; }
        public List<IComHandlerAction> ComHandlerActions { get; init; } = [];
        public ComHandlerGroup? ComHandlers { get; init; } = null;

        public List<Dictionary<string, string>> TypeSpecificDetails { get; set; } = [];

        // Constructor
        public StartupTask(IRegisteredTask task, ComHandlerGroup? comHandlerGroup = null, List<IComHandlerAction>? comHandlerActions = null)
        {
            // Private
            _taskObj = task; // The original object
            _XmlObj = GetTaskXmlDocument(task.Xml);

            (List<_TASK_TRIGGER_TYPE2> normalTypes, List<string> otherDescriptions) autoStartTypes = GetAutoStartTypes(task);

            List<string> triggerStringList = [];
            foreach (_TASK_TRIGGER_TYPE2 triggerType in autoStartTypes.normalTypes)
            {
                // Fetch the friendlyname
                triggerStringList.Add(GetTriggerName(triggerType));
            }

            foreach (string otherDescription in autoStartTypes.otherDescriptions)
            {
                triggerStringList.Add(otherDescription);
            }

            // Public
            Name = task.Name;
            TaskSchedulerPath = task.Path;
            TaskXml = task.Xml;
            Triggers = triggerStringList;
            StartupTaskTypes = triggerStringList;
            ActionTypes = GetActionTypes(task);

            if (comHandlerGroup != null)
            {
                ComHandlers = comHandlerGroup;
            }

            if (comHandlerActions != null)
            {
                ComHandlerActions = comHandlerActions;
            }

            List<IExecAction2> execActionsList = GetExecActions(task);

            // Use com objects
            if (execActionsList.Count == 0 && comHandlerGroup != null)
            {
                ExecActions = execActionsList; // Empty
                // Just use the com object path as the paths
                ExecActionPaths = comHandlerGroup.UniqueExecutablePaths;
                ExecActionPathsWithArgs = comHandlerGroup.UniqueExecutablePaths; // No args for com objects

                // TODO: Add extra details column for app name and stuff retrieved com object app name
            }
            else
            {
                ExecActions = execActionsList;
                ExecActionPaths = GetExecActionPaths(execActionsList, includeArgs: false);
                ExecActionPathsWithArgs = GetExecActionPaths(execActionsList, includeArgs: true);
            }



            TriggerDescription = autoStartTypes.otherDescriptions;

            // Creates a column for task scheduler path in the all autorun tasks form
            TypeSpecificDetails = [
                new Dictionary<string, string> {
                    ["Task Scheduler Path"] = TaskSchedulerPath,
                    //["Special Triggers"] = string.Join(", ", TriggerDescription)
                }
            ];
        }

        // Constant list
        private static List<_TASK_TRIGGER_TYPE2> consideredAutostartTriggers = new()
        {
            _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT,
            _TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON,
            _TASK_TRIGGER_TYPE2.TASK_TRIGGER_IDLE
            //_TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY // We handle this separately to determine how many days

            // TODO: Investigate TASK_TRIGGER_CUSTOM_TRIGGER_01
        };

        // Convert XML to intellisense object
        private static XDocument? GetTaskXmlDocument(string taskXml)
        {
            try
            {
                return XDocument.Parse(taskXml);
            }
            catch
            {
                return null;
            }
        }

        public class ComHandlerGroup
        {
            public List<ComHandlerDetails> Handlers;
            public List<string> UniqueExecutablePaths;

            public ComHandlerGroup(List<ComHandlerDetails> handlers)
            {
                Handlers = handlers;
                UniqueExecutablePaths = GetUniqueExecutables(handlers);
            }

            // Constructor for a single handler item
            public ComHandlerGroup(ComHandlerDetails handler)
            {
                Handlers = [handler];
                UniqueExecutablePaths = [handler.Executable];
            }

            // Merge a list of groups
            public ComHandlerGroup(List<ComHandlerGroup> existingGroups)
            {
                Handlers = new List<ComHandlerDetails>();
                foreach (ComHandlerGroup group in existingGroups)
                {
                    Handlers.AddRange(group.Handlers);
                }
                UniqueExecutablePaths = GetUniqueExecutables(Handlers);
            }


            // ----------- Private Methods -----------
            private List<string> GetUniqueExecutables(List<ComHandlerDetails> comList)
            {
                List<string> comExecutableList = [];
                // Most of the com objects probably refer to the same executable so we'll use distinct
                foreach (ComHandlerDetails comObj in comList)
                {
                    if (!comExecutableList.Contains(comObj.Executable))
                    {
                        comExecutableList.Add(comObj.Executable);
                    }
                }
                return comExecutableList;
            }
        }

        public class ComHandlerDetails
        {
            /// <summary>
            /// Com server from the reference to the Class ID classIdkey within the matching subkey in HKEY_CLASSES_ROOT\PackagedCom\Package\
            /// Example:  HKEY_CLASSES_ROOT\PackagedCom\Package\MicrosoftWindows.Client.CBS_1000.26100.344.0_x64__cw5n1h2txyewy\Class\{F576B2F9-7850-4226-ADB0-E5993FED4F02}
            ///           HKEY_CLASSES_ROOT\PackagedCom\Package\MicrosoftWindows.Client.CBS_1000.26100.334.0_x64__cw5n1h2txyewy\Server\1
            /// </summary>
            /// <param name="serverRegKey"></param>
            public ComHandlerDetails(RegistryKey serverRegKey)
            {
                // Get the values named ApplicationDisplayName, ApplicationId, DisplayName, and Executable
                _applicationDisplayName = serverRegKey.GetValue("ApplicationDisplayName") as string ?? string.Empty;
                ApplicationID = serverRegKey.GetValue("ApplicationId") as string ?? string.Empty;
                DisplayName = serverRegKey.GetValue("DisplayName") as string ?? string.Empty;
                Executable = serverRegKey.GetValue("Executable") as string ?? string.Empty;
                
                if (!String.IsNullOrEmpty(_applicationDisplayName))
                    ApplicationDisplayName = WindowsUtils.ResolveIndirectString(_applicationDisplayName);
                else
                    ApplicationDisplayName = "Unknown Application Name";

            }

            public ComHandlerDetails(string? displayName, string executablePath)
            {
                if (displayName == null || displayName == "")
                {
                    displayName = "Unknown App";
                }

                ApplicationDisplayName = displayName;
                Executable = executablePath;
            }

            private readonly string? _applicationDisplayName; // Resource string

            public string? DisplayName;
            
            public string ApplicationDisplayName; // Resolved
            public string? ApplicationID;
            public string Executable;

        }

        private static string GetTriggerName(_TASK_TRIGGER_TYPE2 trigger)
        {
            switch (trigger)
            {
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT: return "At Boot";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON: return "At Logon";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_IDLE: return "Whenever Idle";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY: return "Daily";
                default: return trigger.ToString();
            }
        }

        private List<_TASK_ACTION_TYPE> GetActionTypes(IRegisteredTask task)
        {
            IActionCollection actionCollection = task.Definition.Actions;
            List<_TASK_ACTION_TYPE> actionTypes = [];
            foreach (IAction action in actionCollection)
            {
                if (!actionTypes.Contains(action.Type)) // Make sure only one instance
                {
                    actionTypes.Add(action.Type);
                }
            }
            return actionTypes;
        }

        // Get list of apps executed when the task runs
        private List<IExecAction2> GetExecActions(IRegisteredTask task)
        {
            IActionCollection action = task.Definition.Actions;
            List<IExecAction2> actionList = [];

            foreach (IAction act in action)
            {
                if (act.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC)
                {
                    IExecAction2 execAction = (IExecAction2)act; // Cast to IExecAction2 instead of IExecAction for all available info from extended interface
                    actionList.Add(execAction);
                }
            }

            return actionList;
        }

        private List<string> GetExecActionPaths(List<IExecAction2> actionList, bool includeArgs)
        {
            List<string> actionPathList = [];

            foreach (IExecAction2 act in actionList)
            {
                string rawPath = act.Path;
                string workingDir = act.WorkingDirectory;
                string truePath;

                // Check if the path is a relative path, if so use working directory to find absolute
                if (!string.IsNullOrEmpty(workingDir) && !System.IO.Path.IsPathRooted(rawPath))
                {
                    truePath = System.IO.Path.Combine(workingDir, rawPath);
                }
                else
                {
                    truePath = rawPath;
                }

                if (includeArgs)
                {
                    string args = act.Arguments;
                    if (!string.IsNullOrEmpty(args))
                    {
                        truePath += " " + args;
                    }
                }

                actionPathList.Add(truePath);
            }

            return actionPathList;
        }

        /// <summary>
        /// Checks for multiple types of "autostart" triggers. Not necessarily just at login or boot. Also for example daily on a timer, when system idle, etc.
        /// Does not include weekly, monthly, or single time scheduled tasks, etc.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>List of enabled triggers considered auto start</returns>
        public static (List<_TASK_TRIGGER_TYPE2> normalTypes, List<string> otherDescriptions) GetAutoStartTypes(IRegisteredTask task)
        {
            List<_TASK_TRIGGER_TYPE2> normalAutoStartTypes = [];
            List<string> otherTypeDescriptions = [];

            foreach (ITrigger trigger in task.Definition.Triggers)
            {
                // Skip disabled and never-active-again triggers
                if (trigger.Enabled != true) continue;
                if (!IsTriggerLive(trigger)) continue;

                if (consideredAutostartTriggers.Contains(trigger.Type))
                {
                    normalAutoStartTypes.Add(trigger.Type);
                }
                // Check exactly how many days between
                else if (trigger.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY)
                {
                    IDailyTrigger dailyTrigger = (IDailyTrigger)trigger;
                    short dayInterval = dailyTrigger.DaysInterval;

                    if (dayInterval > 1)
                        otherTypeDescriptions.Add($"Every {dayInterval} days");
                    else
                        normalAutoStartTypes.Add(trigger.Type); // Daily Schedule
                }

                // Repetitions are determined separately because they might be way more frequent than the base type suggests
                // Even though the previous ones would always cause it to be added anyway, this way it will be more accurate
                // For example Google Chrome update is on daily scheduled, but then repeats every single hour. Now shows daily schedule and the hourly repeat.
                if (CheckRepititionInteval(trigger) is TimeSpan repeatInterval 
                    //&& repeatInterval.TotalHours <= 26 // Removing this for now, would rather user be aware of any repetition and let them decide what they care about
                ){
                    otherTypeDescriptions.Add(MakeFriendlyRepeatString(repeatInterval));
                }
            }
            return (normalAutoStartTypes, otherTypeDescriptions);
        }

        private static string MakeFriendlyRepeatString(TimeSpan interval)
        {
            string s = ""; // For pluralization

            if (interval.TotalDays > 1) // If 24 hours or less use hours
            {
                if (interval.TotalDays != 1) { s = "s"; }
                return $"Repeats every {Math.Round(interval.TotalDays, 1)} day{s}";
            }
            else if (interval.TotalHours >= 1)
            {
                if (interval.TotalHours != 1) { s = "s"; }
                return $"Repeats every {Math.Round(interval.TotalHours, 1)} hour{s}";
            }
            else if (interval.TotalMinutes >= 1)
            {
                if (interval.TotalMinutes != 1) { s = "s"; }
                return $"Repeats every {Math.Round(interval.TotalMinutes, 1)} minute{s}";
            }
            else
            {
                if (interval.TotalSeconds != 1) { s = "s"; }
                return $"Repeats every {Math.Round(interval.TotalSeconds, 1)} second{s}";
            }
        }

        /// <summary>
        /// Other types of triggers can have "reptition" triggers tagged on and can repeat regardless of original type apparently. So need to check those too.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns>A timespan for how often it repeats if it does, otherwise null</returns>
        public static TimeSpan? CheckRepititionInteval(ITrigger trigger)
        {
            string interval;
            string duration;

            try
            {
                IRepetitionPattern repetition = trigger.Repetition;
                if (repetition == null) 
                    return null;

                interval = repetition.Interval;
                duration = repetition.Duration;
            }
            catch
            {
                return null;
            }

            // No usable interval means there is no repetition pattern to speak of.
            if (!TryGetTimeSpan(interval, out TimeSpan intervalTimeSpan) || intervalTimeSpan <= TimeSpan.Zero)
                return null;

            // A duration confines repetition to a window that reopens each time the parent trigger fires.
            // Absent or zero means "repeat indefinitely" - nothing can expire.
            if (TryGetTimeSpan(duration, out TimeSpan durationTimeSpan) && durationTimeSpan > TimeSpan.Zero)
            {
                if (!IsRepetitionWindowLive(trigger, durationTimeSpan))
                    return null;

                // The window must outlast the interval, or the trigger fires once and the window closes before a repeat ever comes due.
                // Such as Interval PT1H / Duration PT30M.
                if (durationTimeSpan < intervalTimeSpan)
                    return null;
            }

            return intervalTimeSpan;
        }

        /// <summary>
        /// Determines whether a trigger's repetition window can still occur now or in the future.
        /// </summary>
        private static bool IsRepetitionWindowLive(ITrigger trigger, TimeSpan duration)
        {
            // Retired trigger - dead regardless of type or repetition.
            if (!IsTriggerLive(trigger)) 
                return false;

            // Any recurring/event trigger fires again, opening a fresh window each time.
            if (trigger.Type != _TASK_TRIGGER_TYPE2.TASK_TRIGGER_TIME) 
                return true;

            // One-shot: the window is [StartBoundary, StartBoundary + Duration].
            string startBoundary;
            try 
                { startBoundary = trigger.StartBoundary; }
            catch 
                { return true; }

            if (!TryParseBoundary(startBoundary, out DateTime start)) 
                return true;

            return DateTime.Now <= start + duration; // ongoing OR not yet started
        }

        /// <summary>
        /// Boundaries are ISO-8601 strings, usually local wall-clock with no offset, but they can carry a 'Z' or a +hh:mm offset.
        /// </summary>
        private static bool TryParseBoundary(string value, out DateTime localTime)
        {
            localTime = default;
            if (string.IsNullOrWhiteSpace(value)) 
                return false;

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
                return false;

            localTime = parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime()
                      : parsed.Kind == DateTimeKind.Local ? parsed
                      : DateTime.SpecifyKind(parsed, DateTimeKind.Local); // Unspecified = local
            return true;
        }

        /// <summary>
        /// Reads an ISO-8601 duration string ("PT20M") without throwing on malformed input.
        /// </summary>
        private static bool TryGetTimeSpan(string value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(value)) 
                return false;

            try
            {
                result = System.Xml.XmlConvert.ToTimeSpan(value);
                return true;
            }
            catch (FormatException) 
                { return false; }
            catch (OverflowException) 
                { return false; }
        }

        /// <summary>
        /// True if the trigger has not been retired by its EndBoundary.
        /// Applies to every trigger type - a Logon or Boot trigger past its EndBoundary will never fire again either.
        /// </summary>
        public static bool IsTriggerLive(ITrigger trigger)
        {
            string endBoundary;
            try 
                { endBoundary = trigger.EndBoundary; }
            catch 
                { return true; } // Unreadable - assume live rather than dropping the task

            return !(TryParseBoundary(endBoundary, out DateTime end) && end <= DateTime.Now);
        }



    } // ---- End StartupTask Class ----

    // ------------------------------------------------------------

    public class StartupScanner
    {

        /// <summary>
        /// Opens a subkey under HKEY_CLASSES_ROOT, checking both the 64-bit and 32-bit registry views.
        /// This is necessary because CLSID entries can be registered in only one view (e.g. a 64-bit COM
        /// server's CLSID key won't be visible to a 32-bit process via the default Registry.ClassesRoot view),
        /// and vice versa.
        /// </summary>
        private static RegistryKey? OpenClassesRootSubKey(string subKeyPath)
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
                RegistryKey? key = baseKey.OpenSubKey(subKeyPath);
                if (key != null)
                {
                    return key;
                }
            }

            return null;
        }

        private static ComHandlerGroup? FindComHandlerSource(string comClass)
        {
            List<ComHandlerDetails> comList = new();

            string CLSIDPath = $@"CLSID\{comClass}";

            // Get the list of subkeys in that classIdkey
            List<string> clsidSubkeys = new List<string>();

            using (RegistryKey? classIdkey = OpenClassesRootSubKey(CLSIDPath))
            {
                if (classIdkey != null)
                {
                    clsidSubkeys.AddRange(classIdkey.GetSubKeyNames());
                }

                string? defaultName = classIdkey?.GetValue("") as string;

                // Easier. The InProcServer32 Default value is likely a dll path directly.
                if (clsidSubkeys.Contains("InProcServer32", StringComparer.OrdinalIgnoreCase))
                {
                    using (RegistryKey? inProcKey = classIdkey?.OpenSubKey("InProcServer32"))
                    {
                        if (inProcKey != null)
                        {
                            string? inProcPath = inProcKey.GetValue("") as string;
                            if (inProcPath != null && inProcPath != "")
                            {
                                // We might not necessarily have the name but we'll have the path at least
                                ComHandlerDetails comObj = new(displayName: defaultName, executablePath: inProcPath);
                                comList.Add(comObj);
                            }
                        }
                    }
                }
            }

            // ----- POSSIBLE EARLY RETURN IF ALREADY HAVE IT -----
            if (comList.Count > 0)
            {
                return new ComHandlerGroup(comList);
            }

            // ---------- ADDITIONAL PROCESSING FOR CERTAIN TYPES WITH MULTIPLE REFERENCES -----------

            // Check in HKEY_CLASSES_ROOT\PackagedCom\ClassIndex\
            string registryPath = $@"PackagedCom\ClassIndex\{comClass}";

            // Get the list of subkeys in that classIdkey
            List<string> subkeys = new List<string>();

            using (RegistryKey? classIdkey = OpenClassesRootSubKey(registryPath))
            {
                if (classIdkey != null)
                {
                    subkeys.AddRange(classIdkey.GetSubKeyNames());
                }
            }

            //TODO - Need to be able to handle when there's no PackagedCom and there's a CLSID in AppID value
            // Such as: HKEY_CLASSES_ROOT\CLSID\{D0582E3B-3126-4CAA-9155-AC37C912A489}

            // Cross reference the subkey names with matching classIdkey names in HKEY_CLASSES_ROOT\PackagedCom\Package\
            foreach (string subkey in subkeys)
            {
                string packagePath = $@"PackagedCom\Package\{subkey}";
                using (RegistryKey? packageKey = OpenClassesRootSubKey(packagePath))
                {
                    string? serverId = null;

                    if (packageKey != null)
                    {
                        // Dig into /Class/ then the comClass
                        using (RegistryKey? classKey = packageKey.OpenSubKey("Class"))
                        {
                            if (classKey != null)
                            {
                                foreach (string classSubkey in classKey.GetSubKeyNames())
                                {
                                    if (string.Equals(classSubkey, comClass, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Get the "ServerId" value. It's probably a number like 0, 1, 2
                                        // The value may be stored as REG_DWORD (int) or REG_SZ (string), so convert rather than casting.
                                        object? serverIdValue = classKey.OpenSubKey(classSubkey)?.GetValue("ServerId");
                                        serverId = serverIdValue != null ? Convert.ToString(serverIdValue) : string.Empty;

                                        break; // Found a match, no need to check other class subkeys
                                    }
                                }
                            }
                        }

                        // ------ Now look for the Server Subkey ------
                        using (RegistryKey? serverGroupKey = packageKey.OpenSubKey("Server"))
                        {
                            // Server subkeys are also numbers like 0, 1, 2. 
                            using (RegistryKey? matchingServerKey = serverGroupKey?.OpenSubKey(serverId))
                            {
                                if (matchingServerKey != null)
                                {
                                    ComHandlerDetails comHandlerItem = new(matchingServerKey);
                                    comList.Add(comHandlerItem);
                                }
                            }
                        }
                    }
                }
            }

            if (comList.Count > 0)
            {
                // If all of the executable paths are empty just return null
                bool allEmpty = comList.All(com => string.IsNullOrEmpty(com.Executable));
                if (allEmpty)
                    return null;
                else
                    return new ComHandlerGroup(comList);
            }
            else
            {
                return null;
            }

        }


        public static List<StartupService> GetStartupServices()
        {
            var items = new List<StartupService>();
            string registryPath = @"SYSTEM\CurrentControlSet\Services";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key == null) return items;

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    using (RegistryKey serviceKey = key.OpenSubKey(subKeyName))
                    {
                        if (serviceKey == null) continue;

                        object startValue = serviceKey.GetValue("Start");
                        if (startValue is int start)
                        {
                            // 0 = Boot, 1 = System, 2 = Automatic
                            if (start == 0 || start == 1 || start == 2)
                            {
                                string displayName = serviceKey.GetValue("DisplayName") as string ?? subKeyName;
                                string imagePath = serviceKey.GetValue("ImagePath") as string ?? string.Empty;
                                string regPath = $@"HKEY_LOCAL_MACHINE\{registryPath}\{subKeyName}";
                                int serviceType = serviceKey.GetValue("Type") is int typeVal ? typeVal : 0;

                                items.Add(new StartupService
                                    (
                                        rawNameString: displayName,
                                        serviceName: subKeyName,
                                        path: imagePath,
                                        regPath: regPath,
                                        startType: start,
                                        serviceType: serviceType
                                    )
                                );
                            }
                        }
                    }
                }
            }
            return items;
        }

        public static List<StartupTask> GetStartupScheduledTasks()
        {
            var taskItems = new List<StartupTask>();

            try
            {
                // Connect to the native Task Scheduler COM object
                Type type = Type.GetTypeFromProgID("Schedule.Service");
                if (type == null) return taskItems;

                ITaskService taskService = new TaskScheduler.TaskScheduler();
                taskService.Connect();

                // Start searching from the root folder
                ITaskFolder rootFolder = taskService.GetFolder("\\");
                ProcessTaskFolder(rootFolder, taskItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching scheduled tasks: " + ex.Message);
            }

            return taskItems;
        }

        private static void ProcessTaskFolder(ITaskFolder folder, List<StartupTask> taskItems)
        {
            try
            {
                // Get tasks (1 = TASK_ENUM_HIDDEN)
                IRegisteredTaskCollection tasks = folder.GetTasks(1);

                #if DEBUG
                    // Create a list of the task objects thats easy to look through
                    List<IRegisteredTask> allTasksList = new();
                    foreach (IRegisteredTask task in tasks)
                    {
                        allTasksList.Add(task);
                    }
                #endif

                foreach (IRegisteredTask task in tasks)
                {
                    try
                    {
                        #if DEBUG
                            if (task.Name.Contains("BackgroundDownload"))
                            {
                                ITriggerCollection triggers = task.Definition.Triggers;
                                List<object?> triggerList = [];
                                ITrigger testCast;
                                foreach (object? trigger in triggers)
                                {
                                    triggerList.Add(trigger);
                                    testCast = (ITrigger)trigger;
                                    string? interval = testCast.Repetition?.Interval;
                                    Console.WriteLine("Hello");
                                }
                                Console.WriteLine("Hello");
                            }
                        #endif

                        if (task.Enabled == true)
                        {
                            (List<_TASK_TRIGGER_TYPE2> normalTypes, List<string> otherDescriptions) typesResult = StartupTask.GetAutoStartTypes(task);

                            if (typesResult.normalTypes.Count > 0 || typesResult.otherDescriptions.Count > 0)
                            {
                                List<ComHandlerGroup> pendingHandlerGroups = []; // We'll merge this at the end
                                List<IComHandlerAction> comHandlerActions = [];
                                List<IExecAction2> execActions = [];

                                // Add each such task exactly once (a task can have multiple exec actions).
                                bool hasActionToFlag = false;
                                foreach (IAction action in task.Definition.Actions)
                                {
                                    #if DEBUG
                                        if (task.Name == "SoftLandingCreativeManagementTask")
                                            Console.WriteLine("Hello");
                                    #endif

                                    if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC)
                                    {
                                        execActions.Add((IExecAction2)action);
                                        hasActionToFlag = true;
                                        break;
                                    }
                                    else if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_COM_HANDLER)
                                    {
                                        // Inspect the com class
                                        IComHandlerAction comAction = (IComHandlerAction)action;
                                        string comClass = comAction.ClassId;

                                        // Check in HKEY_CLASSES_ROOT\PackagedCom\ClassIndex\
                                        string registryPath = $@"PackagedCom\ClassIndex\{comClass}";

                                        // Get the com handlers
                                        ComHandlerGroup? comHandlersReturned = FindComHandlerSource(comClass);
                                        if (comHandlersReturned is ComHandlerGroup handlers)
                                        {
                                            if (handlers.UniqueExecutablePaths.Count > 0)
                                            {
                                                hasActionToFlag = true;
                                                pendingHandlerGroups.Add(handlers);
                                                comHandlerActions.Add(comAction);
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (hasActionToFlag)
                                {

                                    if (pendingHandlerGroups.Count > 0)
                                    {

                                        ComHandlerGroup handlerGroup = new(pendingHandlerGroups);
                                        StartupTask taskToAdd = new StartupTask(task, handlerGroup, comHandlerActions);
                                        taskItems.Add(taskToAdd); // It sets its own path property
                                    } 
                                    else
                                    {
                                        taskItems.Add(new StartupTask(task));
                                    }

                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore tasks that we lack permission to read completely
                    }
                }

                // Recursively process subfolders (0 = default/reserved)
                ITaskFolderCollection subFolders = folder.GetFolders(0);
                foreach (ITaskFolder subFolder in subFolders)
                {
                    ProcessTaskFolder(subFolder, taskItems);
                }
            }
            catch
            {
                // Ignore folders we lack permission to enter
                Debug.WriteLine("Unable to access task folder: " + folder.Path);
            }
        }



    } // ----- End StartupScanner Class -----

} // ---- End Namespace -----

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}