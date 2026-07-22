// StartupScanner.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;

#nullable enable

// Add using for TaskScheduler
using TaskScheduler;
using System.Linq;

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
        public string ServiceName { get; init; } // The (unique) registry key name of the service
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
        /// Falls back to the registry key name if no path is available.
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

        public List<Dictionary<string, string>> TypeSpecificDetails { get; set; } = [];

        // Constructor
        public StartupTask(IRegisteredTask task)
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

            List<IExecAction2> execActionsList = GetExecActions(task);
            ExecActions = execActionsList;
            ExecActionPaths = GetExecActionPaths(execActionsList, includeArgs: false);
            ExecActionPathsWithArgs = GetExecActionPaths(execActionsList, includeArgs: true);

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
            _TASK_TRIGGER_TYPE2.TASK_TRIGGER_IDLE,
            _TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY

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

        private static string GetTriggerName(_TASK_TRIGGER_TYPE2 trigger)
        {
            switch (trigger)
            {
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT: return "At Boot";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON: return "At Logon";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_IDLE: return "Whenever Idle";
                case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY: return "Daily Schedule";
                default: return trigger.ToString();
            }
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
                if (trigger.Enabled == true)
                {
                    if (consideredAutostartTriggers.Contains(trigger.Type))
                    {
                        normalAutoStartTypes.Add(trigger.Type);
                    }
                    else if (CheckRepititionInteval(trigger) is TimeSpan repeatInterval)
                    {
                        // Check if it's 26 hours or less
                        if (repeatInterval.TotalHours <= 26)
                        {
                            otherTypeDescriptions.Add(MakeFriendlyRepeatString(repeatInterval));
                        }
                    }
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
                return $"Repeats every {interval.TotalDays} day{s}";
            }
            else if (interval.TotalHours >= 1)
            {
                if (interval.TotalHours != 1) { s = "s"; }
                return $"Repeats every {interval.TotalHours} hour{s}";
            }
            else if (interval.TotalMinutes >= 1)
            {
                if (interval.TotalMinutes != 1) { s = "s"; }
                return $"Repeats every {interval.TotalMinutes} minute{s}";
            }
            else
            {
                if (interval.TotalSeconds != 1) { s = "s"; }
                return $"Repeats every {interval.TotalSeconds} second{s}";
            }
        }

        /// <summary>
        /// Other types of triggers can have "reptition" triggers tagged on and can repeat regardless of original type apparently. So need to check those too.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns>A timespan for how often it repeats if it does, otherwise null</returns>
        public static TimeSpan? CheckRepititionInteval(ITrigger trigger)
        {
            // Check if it contains reptition
            if (trigger.Repetition == null)
            {
                return null;
            }
            else // It has repitition
            {
                // See for repitition patterns: https://learn.microsoft.com/en-us/windows/win32/taskschd/repetitionpattern-interval
                string interval = trigger.Repetition.Interval;
                string duration = trigger.Repetition.Duration;
                bool durationStop = trigger.Repetition.StopAtDurationEnd;

                // If there's an interval and it's less than 24 hours
                if (string.IsNullOrEmpty(interval))
                {
                    return null;
                }
                else
                {
                    //// Handle durations later
                    //if (!string.IsNullOrEmpty(duration))
                    //{
                    //    TimeSpan durationTimeSpan;
                    //    durationTimeSpan = System.Xml.XmlConvert.ToTimeSpan(duration);
                    //    if (durationTimeSpan.TotalSeconds > 0)
                    //    {
                    //        // Do something
                    //    }
                    //}

                    TimeSpan intervalTimeSpan;

                    try
                    {
                        // The string will look like "PT20M" or something but this converts it to time
                        intervalTimeSpan = System.Xml.XmlConvert.ToTimeSpan(interval);
                        return intervalTimeSpan;
                    }
                    catch
                    {
                        return null; // Invalid format
                    }
                }
            }
        }

    } // ---- End StartupTask Class ----

    // ------------------------------------------------------------

    public class StartupScanner
    {
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
                        //DEBUG
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
                            var typesResult = StartupTask.GetAutoStartTypes(task);

                            if (typesResult.normalTypes.Count > 0 || typesResult.otherDescriptions.Count > 0)
                            {
                                // Only include tasks that actually launch an executable, and add each such
                                // task exactly once (a task can have multiple exec actions).
                                bool hasExecAction = false;
                                foreach (IAction action in task.Definition.Actions)
                                {
                                    // 0 = TASK_ACTION_EXEC
                                    if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC)
                                    {
                                        hasExecAction = true;
                                        break;
                                    }
                                }

                                if (hasExecAction)
                                {
                                    taskItems.Add(new StartupTask(task)); // It sets its own path property
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