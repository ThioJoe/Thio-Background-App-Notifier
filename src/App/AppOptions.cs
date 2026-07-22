using System;

#nullable enable

namespace Thio_Background_App_Notifier;

/// <summary>
/// Parsed command-line options.
/// </summary>
public class AppOptions
{
    /// <summary>
    /// Quiet mode: intended for the Startup-folder shortcut. The app scans silently and only
    /// shows a window when it finds startup items that are new since the last run.
    /// </summary>
    public bool QuietMode { get; private set; }

    /// <summary>
    /// Internal mode: the app was relaunched elevated purely to delete the all-users startup shortcut.
    /// When set, the app removes that shortcut and exits immediately, without scanning or showing UI.
    /// Matches StartupShortcut.RemoveMachineStartupArgument.
    /// </summary>
    public bool RemoveMachineStartupShortcut { get; private set; }

    public static AppOptions Parse(string[]? args)
    {
        var options = new AppOptions();
        if (args == null)
            return options;

        foreach (string raw in args)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            // Accept -quiet, --quiet, /quiet, -q, etc.
            string arg = raw.Trim().TrimStart('-', '/').ToLowerInvariant();
            switch (arg)
            {
                case "quiet":
                case "q":
                    options.QuietMode = true;
                    break;

                case "remove-machine-startup":
                    options.RemoveMachineStartupShortcut = true;
                    break;
            }
        }

        return options;
    }
}
