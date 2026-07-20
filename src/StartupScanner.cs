// StartupScanner.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

// Add using for TaskScheduler
using TaskScheduler;

namespace New_Startup_App_Notifier
{
    public class StartupItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public StartupItemType Type { get; set; } // "Service" or "ScheduledTask"
    }

    public class StartupTask
    {
        public string Name { get; set; }
        public string TaskPath { get; set; }
        public string TaskXml { get; init; } = string.Empty;

        // Constructor
        public StartupTask(IRegisteredTask task)
        {
            XDocument taskObj = GetTaskXmlDocument(task.Xml);

            Name = task.Name;
            TaskPath = task.Path;
            TaskXml = task.Xml;
        }

        // Convert XML to intellisense object
        public XDocument GetTaskXmlDocument(string taskXml)
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

    }

    // Enum for type
    public enum StartupItemType
    {
        Service,
        ScheduledTask
    }

    public class StartupScanner
    {
        public static List<StartupItem> GetStartupServices()
        {
            var items = new List<StartupItem>();
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

                                items.Add(new StartupItem
                                {
                                    Name = displayName,
                                    Path = imagePath,
                                    Type = StartupItemType.Service
                                });
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
                    bool isStartup = false;
                    try
                    {
                        foreach (ITrigger trigger in task.Definition.Triggers)
                        {
                            // 8 = TASK_TRIGGER_BOOT, 9 = TASK_TRIGGER_LOGON
                            if (trigger.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT ||
                                trigger.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON)
                            {
                                isStartup = true;
                                break;
                            }
                        }

                        if (isStartup)
                        {
                            string taskPath = string.Empty;
                            foreach (IAction action in task.Definition.Actions)
                            {
                                // 0 = TASK_ACTION_EXEC
                                if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC)
                                {
                                    IExecAction execAction = (IExecAction)action;
                                    taskPath = execAction.Path;
                                    break;
                                }
                            }

                            taskItems.Add(new StartupTask(task));
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
            }
        }


    } // ----- End StartupScanner Clas -----

} // ---- End Namespace -----

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}