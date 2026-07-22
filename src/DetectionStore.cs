using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

#nullable enable

namespace Thio_Background_App_Notifier
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

        /// <summary>The quiet-mode popup has already alerted the user about this item; don't alert again.</summary>
        [DataMember(Order = 7)] public bool Alerted { get; set; }

        /// <summary>The user has actually viewed this item in the main window.</summary>
        [DataMember(Order = 8)] public bool SeenInWindow { get; set; }
    }

    /// <summary>
    /// Root object persisted to disk. Holds run bookkeeping plus every startup item ever seen.
    /// </summary>
    [DataContract]
    public class DetectionStoreData
    {
        [DataMember(Order = 0)] public int SchemaVersion { get; set; } = 2;
        [DataMember(Order = 1)] public string? FirstRunUtc { get; set; }
        [DataMember(Order = 2)] public string? LastRunUtc { get; set; }
        [DataMember(Order = 3)] public List<KnownStartupItem> Items { get; set; } = new List<KnownStartupItem>();
    }

    /// <summary>
    /// The outcome of a single scan. Distinguishes what's brand-new on this scan from the running
    /// history of everything that has appeared since the baseline:
    ///  - <see cref="UnalertedItems"/>: things the quiet-mode popup hasn't told the user about yet.
    ///  - <see cref="NewItems"/>: things detected for the very first time on this scan (highlighted).
    ///  - <see cref="ItemsSinceBaseline"/>: everything that has appeared since the first-run baseline,
    ///    whether this scan or an earlier one. This is the persistent history the main window lists.
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

        /// <summary>
        /// Items the quiet-mode popup hasn't alerted the user about yet. Drives whether (and about what)
        /// the startup popup appears. Empty on the first/baseline run.
        /// </summary>
        public List<IStartupItem> UnalertedItems { get; set; } = new List<IStartupItem>();

        /// <summary>
        /// Items detected for the first time on this scan (always empty on the first/baseline run).
        /// These are the ones highlighted as "NEW" in the list and counted in the status headline.
        /// </summary>
        public List<IStartupItem> NewItems { get; set; } = new List<IStartupItem>();

        /// <summary>
        /// Every currently-present item that appeared after the first-run baseline — added this scan
        /// or on a previous one. This is the running history the main window lists, so already-seen
        /// items stay listed across rescans instead of disappearing once viewed. Empty on the first run.
        /// </summary>
        public List<IStartupItem> ItemsSinceBaseline { get; set; } = new List<IStartupItem>();

        public bool HasUnalertedItems => UnalertedItems.Count > 0;
        public bool HasNewItems => NewItems.Count > 0;

        // The store this result came from, so it can be persisted once the results are actually shown.
        internal DetectionStore? Store { get; set; }

        /// <summary>
        /// Records that the user has now seen the main window: marks the listed history items as
        /// alerted (so the quiet popup won't re-nag about anything already shown) and saves — which
        /// also persists newly-detected items so they remain in the history on later scans.
        /// Call this when the main window is displayed.
        /// </summary>
        public void CommitShown() => Store?.MarkAlerted(ItemsSinceBaseline);

        /// <summary>
        /// Records that the quiet popup alerted the user (even if they declined to open the window):
        /// marks the alerted items so they won't be alerted again, and saves.
        /// </summary>
        public void CommitAlerted() => Store?.MarkAlerted(UnalertedItems);

        /// <summary>
        /// Saves run bookkeeping only (no new items to mark). Used on the silent quiet path.
        /// </summary>
        public void CommitBookkeeping() => Store?.Save();
    }

    /// <summary>
    /// Loads and saves the detection log, and compares a fresh scan against what was previously known.
    /// The log lives in the user's roaming AppData so it persists between runs without admin rights.
    /// </summary>
    public class DetectionStore
    {
        private readonly DetectionStoreData _data;
        private readonly bool _existedBefore;
        private readonly Dictionary<string, KnownStartupItem> _byKey;

        private DetectionStore(DetectionStoreData data, bool existedBefore)
        {
            _data = data;
            _existedBefore = existedBefore;
            _data.Items ??= new List<KnownStartupItem>();

            _byKey = new Dictionary<string, KnownStartupItem>(StringComparer.OrdinalIgnoreCase);
            foreach (KnownStartupItem known in _data.Items)
            {
                if (!string.IsNullOrEmpty(known.IdentityKey))
                    _byKey[known.IdentityKey] = known;
            }
        }

        // ---- Locations ----

        /// <summary>
        /// Directory that holds the detection log. It always lives in the current user's roaming
        /// AppData, under the same "group" folder the installer uses: %APPDATA%\ThioJoe\&lt;AppName&gt;.
        /// Keeping it in AppData rather than next to the exe means it works without admin rights even
        /// when the app itself was installed for all users under Program Files.
        /// </summary>
        public static string StoreDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                DefaultRelativeInstallPath);

        public static string StorePath => Path.Combine(StoreDirectory, "detections.json");

        // ---- Public entry point ----

        /// <summary>
        /// Performs a full scan (services + scheduled tasks) and compares it to the saved log.
        /// This mutates the in-memory store but does NOT write to disk; the caller persists via one of
        /// the ScanResult.Commit* methods once the results are actually surfaced to the user.
        /// </summary>
        public static ScanResult PerformScan()
        {
            var liveItems = new List<IStartupItem>();
            liveItems.AddRange(StartupScanner.GetStartupServices());
            liveItems.AddRange(StartupScanner.GetStartupScheduledTasks());

            DetectionStore store = Load();
            return store.Apply(liveItems, DateTime.UtcNow);
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

        // ---- Marking (used by ScanResult.Commit* once results are shown) ----

        internal void MarkAlerted(IEnumerable<IStartupItem> items)
        {
            foreach (IStartupItem item in items)
            {
                if (_byKey.TryGetValue(item.IdentityKey, out KnownStartupItem? record))
                    record.Alerted = true;
            }
            Save();
        }

        // ---- Comparison ----

        private ScanResult Apply(List<IStartupItem> liveItems, DateTime nowUtc)
        {
            string isoNow = ToIso(nowUtc);
            DateTime nowLocal = nowUtc.ToLocalTime();
            bool isFirstRun = !_existedBefore;

            // Everything recorded on the first run shares this exact first-detection stamp, so it marks
            // the baseline: any record with a different FirstDetectedUtc appeared later. Captured before
            // FirstRunUtc is (re)assigned below.
            string? baselineStamp = _data.FirstRunUtc;

            var unalertedItems = new List<IStartupItem>();
            var newItems = new List<IStartupItem>();
            var itemsSinceBaseline = new List<IStartupItem>();

            foreach (IStartupItem item in liveItems)
            {
                string key = item.IdentityKey;
                KnownStartupItem record;

                if (_byKey.TryGetValue(key, out KnownStartupItem? existing))
                {
                    // Seen on a previous run -> keep its original first-detection time, refresh the rest.
                    existing.LastSeenUtc = isoNow;
                    existing.Name = item.Name;
                    existing.Path = item.Path;
                    existing.Detail = UiHelpers.GetDetail(item);

                    item.IsFirstDetection = false;
                    item.FirstDetectionTime = FromIso(existing.FirstDetectedUtc, nowLocal);
                    record = existing;
                }
                else
                {
                    // Never seen before.
                    record = new KnownStartupItem
                    {
                        IdentityKey = key,
                        ItemType = item.Type.ToString(),
                        Name = item.Name,
                        Path = item.Path,
                        Detail = UiHelpers.GetDetail(item),
                        FirstDetectedUtc = isoNow,
                        LastSeenUtc = isoNow
                    };

                    if (isFirstRun)
                    {
                        // Everything present on the first run is the baseline: treat it as already
                        // known so it neither pops up nor fills the "new" list.
                        record.Alerted = true;
                        record.SeenInWindow = true;
                    }

                    _data.Items.Add(record);
                    _byKey[key] = record;

                    item.FirstDetectionTime = nowLocal;
                    item.IsFirstDetection = !isFirstRun; // Highlight genuinely-new items in the UI.

                    // Detected for the first time on this scan (never on the baseline run).
                    if (!isFirstRun)
                        newItems.Add(item);
                }

                // The quiet popup only cares about items it hasn't alerted about yet.
                if (!record.Alerted)
                    unalertedItems.Add(item);

                // Anything not part of the first-run baseline belongs in the running history and stays
                // listed on the main window, whether it was added this scan or an earlier one.
                bool isBaseline = isFirstRun || record.FirstDetectedUtc == baselineStamp;
                if (!isBaseline)
                    itemsSinceBaseline.Add(item);
            }

            // Forget items that are no longer present. If a startup item is deleted (or disabled so it
            // no longer shows up) and later comes back, this ensures it's reported as new again rather
            // than remembered as already-known.
            var liveKeys = new HashSet<string>(
                liveItems.Select(i => i.IdentityKey), StringComparer.OrdinalIgnoreCase);
            _data.Items.RemoveAll(r => !liveKeys.Contains(r.IdentityKey));

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
                UnalertedItems = unalertedItems,
                NewItems = newItems,
                ItemsSinceBaseline = itemsSinceBaseline,
                Store = this
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
