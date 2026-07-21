using System;
using System.Diagnostics;
using Microsoft.Win32;

#nullable enable

namespace New_Startup_App_Notifier;

/// <summary>
/// Reads (and can turn on) Windows' own "Startup app notifications" setting
/// (Settings ▸ System ▸ Notifications ▸ Startup app notifications), which is what notifies the user
/// about regular startup apps that this tool intentionally doesn't track.
/// </summary>
internal static class WindowsStartupNotification
{
    // Per-source notification setting for the "an app was added to startup" toast.
    private const string SubKey =
        @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.StartupApp";
    private const string ValueName = "Enabled";

    /// <summary>
    /// Whether the Windows startup-app notification is enabled: true / false when known, or null if
    /// it can't be determined. A missing value counts as enabled, since that's the Windows default.
    /// </summary>
    public static bool? IsEnabled()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(SubKey))
            {
                if (key == null)
                    return true; // No key yet -> Windows default is on.

                object? value = key.GetValue(ValueName);
                if (value is int i)
                    return i != 0;

                return true; // Value absent -> default on.
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Couldn't read the Windows startup-notification setting: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Turns the Windows startup-app notification on by writing the per-source registry value
    /// (HKCU, so no admin rights needed).
    /// </summary>
    public static bool Enable(out string error)
    {
        error = string.Empty;
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(SubKey))
            {
                if (key == null)
                {
                    error = "Couldn't open the notification settings registry key.";
                    return false;
                }
                key.SetValue(ValueName, 1, RegistryValueKind.DWord);
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Opens the Windows notification settings page so the user can toggle it themselves.
    /// </summary>
    public static void OpenSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:notifications",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Couldn't open the Windows notification settings: " + ex.Message);
        }
    }
}
