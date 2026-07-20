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

namespace New_Startup_App_Notifier
{
    public enum StartupItemType
    {
        Service,
        ScheduledTask
    }

    public class StartupItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public StartupItemType Type { get; set; } // "Service" or "ScheduledTask"
    }

    public class StartupService
    {
        public string Name { get; set; }
        public string Path { get; set; }

        // Constructor
        public StartupService(string rawNameString, string path)
        {
            Name = Utils.ResolveIndirectString(rawNameString);
            Path = path;
        }

    }

    public class StartupTask
    {
        private IRegisteredTask _taskObj;
        private XDocument? _XmlObj;

        public string Name { get; set; }
        public string TaskSchedulerPath { get; set; }
        public string TaskXml { get; init; } = string.Empty;
        public List<_TASK_TRIGGER_TYPE2> StartupTaskTypes { get; init; }
        public ITriggerCollection Triggers { get; init; }
        public List<IExecAction2> ExecActions { get; init; }
        public List<string> ExecActionPaths { get; init; }
        public List<string> ExecActionPathsWithArgs { get; init; }

        // Constructor
        public StartupTask(IRegisteredTask task)
        {
            // Private
            _taskObj = task; // The original object
            _XmlObj = GetTaskXmlDocument(task.Xml);

            // Public
            Name = task.Name;
            TaskSchedulerPath = task.Path;
            TaskXml = task.Xml;
            Triggers = task.Definition.Triggers;
            StartupTaskTypes = GetAutoStartTypes(task);

            List<IExecAction2> execActionsList = GetExecActions(task);
            ExecActions = execActionsList;
            ExecActionPaths = GetExecActionPaths(execActionsList, includeArgs: false);
            ExecActionPathsWithArgs = GetExecActionPaths(execActionsList, includeArgs: true);

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
        public static List<_TASK_TRIGGER_TYPE2> GetAutoStartTypes(IRegisteredTask task)
        {
            List<_TASK_TRIGGER_TYPE2> autoStartTypes = new List<_TASK_TRIGGER_TYPE2>();
            foreach (ITrigger trigger in task.Definition.Triggers)
            {
                if (consideredAutostartTriggers.Contains(trigger.Type) && trigger.Enabled == true)
                {
                    autoStartTypes.Add(trigger.Type);
                }
            }
            return autoStartTypes;
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

                                items.Add(new StartupService(rawNameString: displayName, path:imagePath));
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
                foreach (IRegisteredTask task in tasks)
                {
                    try
                    {
                        if (StartupTask.GetAutoStartTypes(task).Count > 0)
                        {
                            string taskPath = string.Empty;
                            foreach (IAction action in task.Definition.Actions)
                            {
                                // 0 = TASK_ACTION_EXEC
                                if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC)
                                {
                                    taskItems.Add(new StartupTask(task)); // Just add it to the list. It will handle itself for setting path property
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