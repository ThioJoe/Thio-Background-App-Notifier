using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

#nullable enable

namespace New_Startup_App_Notifier
{
    /// <summary>
    /// One remembered startup item, as written to the on-disk detection log.
    /// This is a lightweight snapshot (a plain data record) rather than the live scan object,
    /// so it can be serialized without dragging along COM / registry handles.
    /// </summary>
    [DataContract]
    public class KnownStartupItem
    {
        [DataMember(Order = 0)] public string IdentityKey { get; set; } = string.Empty;
        [DataMember(Order = 1)] public string ItemType { get; set; } = string.Empty; // "Service" or "ScheduledTask"
        [DataMember(Order = 2)] public string Name { get; set; } = string.Empty;
        [DataMember(Order = 3)] public string Path { get; set; } = string.Empty;
        [DataMember(Order = 4)] public string Detail { get; set; } = string.Empty; // start type / triggers
        [DataMember(Order = 5)] public string FirstDetectedUtc { get; set; } = string.Empty; // ISO-8601 (round-trip)
        [DataMember(Order = 6)] public string LastSeenUtc { get; set; } = string.Empty;      // ISO-8601 (round-trip)
    }

    /// <summary>
    /// Root object persisted to disk. Holds run bookkeeping plus every startup item ever seen.
    /// </summary>
    [DataContract]
    public class DetectionStoreData
    {
        [DataMember(Order = 0)] public int SchemaVersion { get; set; } = 1;
        [DataMember(Order = 1)] public string? FirstRunUtc { get; set; }
        [DataMember(Order = 2)] public string? LastRunUtc { get; set; }
        [DataMember(Order = 3)] public List<KnownStartupItem> Items { get; set; } = new List<KnownStartupItem>();
    }

    /// <summary>
    /// The outcome of a single scan: the live items, plus which of them are new since the last run.
    /// </summary>
    public class ScanResult
    {
        /// <summary>True when this was the very first run (the baseline was just established).</summary>
        public bool IsFirstRun { get; set; }

        public DateTime? PreviousRunTimeLocal { get; set; }
        public DateTime RunTimeLocal { get; set; }

        public List<IStartupItem> AllItems { get; set; } = new List<IStartupItem>();
        public List<IStartupItem> Services { get; set; } = new List<IStartupItem>();
        public List<IStartupItem> Tasks { get; set; } = new List<IStartupItem>();

        /// <summary>Items detected for the first time on this run (always empty on the first/baseline run).</summary>
        public List<IStartupItem> NewItems { get; set; } = new List<IStartupItem>();

        public bool HasNewItems => NewItems.Count > 0;
    }

    /// <summary>
    /// Loads and saves the detection log, and compares a fresh scan against what was previously known.
    /// The log lives in the user's roaming AppData so it persists between runs without admin rights.
    /// </summary>
    public class DetectionStore
    {
        private readonly DetectionStoreData _data;
        private readonly bool _existedBefore;

        private DetectionStore(DetectionStoreData data, bool existedBefore)
        {
            _data = data;
            _existedBefore = existedBefore;
            _data.Items ??= new List<KnownStartupItem>();
        }

        // ---- Locations ----

        public static string StoreDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Sneaky Startup App Notifier");

        public static string StorePath => Path.Combine(StoreDirectory, "detections.json");

        // ---- Public entry point ----

        /// <summary>
        /// Performs a full scan (services + scheduled tasks), compares it to the saved log,
        /// records any newly-seen items with a first-detection timestamp, saves the log, and
        /// returns the result for display.
        /// </summary>
        public static ScanResult PerformScan()
        {
            // 1. Live scan.
            var liveItems = new List<IStartupItem>();
            liveItems.AddRange(StartupScanner.GetStartupServices());
            liveItems.AddRange(StartupScanner.GetStartupScheduledTasks());

            // 2. Load the previous log (or an empty baseline on first run / if the file is unreadable).
            DetectionStore store = Load();

            // 3. Compare + timestamp.
            ScanResult result = store.Apply(liveItems, DateTime.UtcNow);

            // 4. Persist.
            store.Save();

            return result;
        }

        // ---- Load / Save ----

        public static DetectionStore Load()
        {
            try
            {
                if (File.Exists(StorePath))
                {
                    using (FileStream fs = File.OpenRead(StorePath))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(DetectionStoreData));
                        var data = (DetectionStoreData?)serializer.ReadObject(fs);
                        if (data != null)
                            return new DetectionStore(data, existedBefore: true);
                    }
                }
            }
            catch (Exception ex)
            {
                // A corrupt or unreadable log is treated as a fresh baseline rather than crashing,
                // so we don't suddenly flag every existing startup item as "new".
                Debug.WriteLine("Failed to read detection log, starting fresh: " + ex.Message);
            }

            return new DetectionStore(new DetectionStoreData(), existedBefore: false);
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(StoreDirectory);

                // Write to a temp file then move into place so an interrupted write can't corrupt the log.
                string tempPath = StorePath + ".tmp";
                using (FileStream fs = File.Create(tempPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DetectionStoreData));
                    serializer.WriteObject(fs, _data);
                }

                if (File.Exists(StorePath))
                    File.Delete(StorePath);
                File.Move(tempPath, StorePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to write detection log: " + ex.Message);
            }
        }

        // ---- Comparison ----

        private ScanResult Apply(List<IStartupItem> liveItems, DateTime nowUtc)
        {
            string isoNow = ToIso(nowUtc);
            DateTime nowLocal = nowUtc.ToLocalTime();
            bool isFirstRun = !_existedBefore;

            var byKey = new Dictionary<string, KnownStartupItem>(StringComparer.OrdinalIgnoreCase);
            foreach (KnownStartupItem known in _data.Items)
            {
                if (!string.IsNullOrEmpty(known.IdentityKey))
                    byKey[known.IdentityKey] = known;
            }

            var newItems = new List<IStartupItem>();

            foreach (IStartupItem item in liveItems)
            {
                string key = item.IdentityKey;

                if (byKey.TryGetValue(key, out KnownStartupItem? existing))
                {
                    // Seen on a previous run -> keep its original first-detection time, refresh the rest.
                    existing.LastSeenUtc = isoNow;
                    existing.Name = item.Name;
                    existing.Path = item.Path;
                    existing.Detail = UiHelpers.GetDetail(item);

                    item.IsFirstDetection = false;
                    item.FirstDetectionTime = FromIso(existing.FirstDetectedUtc, nowLocal);
                }
                else
                {
                    // Never seen before.
                    var record = new KnownStartupItem
                    {
                        IdentityKey = key,
                        ItemType = item.Type.ToString(),
                        Name = item.Name,
                        Path = item.Path,
                        Detail = UiHelpers.GetDetail(item),
                        FirstDetectedUtc = isoNow,
                        LastSeenUtc = isoNow
                    };
                    _data.Items.Add(record);
                    byKey[key] = record;

                    item.FirstDetectionTime = nowLocal;

                    // On the very first run everything is "new", which isn't useful to flag, so we only
                    // treat items as new once a baseline already exists.
                    item.IsFirstDetection = !isFirstRun;
                    if (!isFirstRun)
                        newItems.Add(item);
                }
            }

            DateTime? previousRunLocal = TryFromIso(_data.LastRunUtc);

            if (string.IsNullOrEmpty(_data.FirstRunUtc))
                _data.FirstRunUtc = isoNow;
            _data.LastRunUtc = isoNow;

            return new ScanResult
            {
                IsFirstRun = isFirstRun,
                PreviousRunTimeLocal = previousRunLocal,
                RunTimeLocal = nowLocal,
                AllItems = liveItems,
                Services = liveItems.Where(i => i.Type == StartupItemType.Service).ToList(),
                Tasks = liveItems.Where(i => i.Type == StartupItemType.ScheduledTask).ToList(),
                NewItems = newItems
            };
        }

        // ---- Timestamp helpers (stored as round-trip ISO-8601 UTC strings) ----

        private static string ToIso(DateTime utc) =>
            utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

        private static DateTime FromIso(string iso, DateTime fallbackLocal)
        {
            DateTime? parsed = TryFromIso(iso);
            return parsed ?? fallbackLocal;
        }

        private static DateTime? TryFromIso(string? iso)
        {
            if (string.IsNullOrEmpty(iso))
                return null;

            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return parsed.ToLocalTime();
            }

            return null;
        }
    }
}
