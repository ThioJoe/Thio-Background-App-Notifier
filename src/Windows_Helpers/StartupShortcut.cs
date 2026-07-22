using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#nullable enable

namespace Thio_Background_App_Notifier;

/// <summary>
/// Manages an optional shortcut to this app in the current user's Startup folder.
/// When present, Windows launches the app (quietly) at logon so it can check for new startup items.
///
/// The shortcut is created via the built-in Windows Script Host COM object (WScript.Shell) using
/// late binding, so there are no third-party dependencies and no extra project references.
/// </summary>
internal static class StartupShortcut
{
    private const string ShortcutFileName = $"{StartupShortcutName}.lnk";

    /// <summary>Argument the startup shortcut passes so the launch is quiet.</summary>
    public const string QuietArgument = "-quiet";

    /// <summary>
    /// Argument that tells a (relaunched, elevated) instance to delete the all-users startup shortcut
    /// and exit immediately. See <see cref="RemoveMachineShortcut"/> and Program.Main.
    /// </summary>
    public const string RemoveMachineStartupArgument = "--remove-machine-startup";

    /// <summary>The current user's Startup-folder shortcut. Creating/removing it needs no admin rights.</summary>
    public static string ShortcutPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            ShortcutFileName);

    /// <summary>
    /// The machine-wide (all-users) Startup-folder shortcut. An "all users" install creates the
    /// shortcut here, because Windows redirects the installer's Startup folder to the common one for
    /// per-machine installs. This folder is protected, so removing this shortcut requires elevation.
    /// </summary>
    public static string CommonShortcutPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            ShortcutFileName);

    /// <summary>
    /// True if the app is set to run at logon via either the current user's Startup folder or the
    /// machine-wide (all-users) Startup folder, so the checkbox stays correct after an all-users
    /// install even though that shortcut lives in the common folder.
    /// </summary>
    public static bool IsEnabled => File.Exists(ShortcutPath) || File.Exists(CommonShortcutPath);

    /// <summary>
    /// Creates (or overwrites) the startup shortcut pointing at this executable with the quiet flag.
    /// </summary>
    public static bool Enable(out string error)
    {
        error = string.Empty;
        object? shell = null;
        try
        {
            string exePath = Application.ExecutablePath;

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                error = "Windows Script Host (WScript.Shell) is not available on this system.";
                return false;
            }

            shell = Activator.CreateInstance(shellType);
            dynamic dynShell = shell!;

            dynamic shortcut = dynShell.CreateShortcut(ShortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.Arguments = QuietArgument;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
            shortcut.Description = "Quietly checks for new startup services and scheduled tasks at logon.";
            shortcut.IconLocation = exePath + ",0";
            shortcut.Save();

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (shell != null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }
    }

    /// <summary>
    /// Removes the startup shortcut. The per-user shortcut is deleted directly (no admin needed). If a
    /// machine-wide (all-users) shortcut is also present, we relaunch ourselves elevated to delete it,
    /// so a user who turns startup off after an all-users install can actually get rid of it. The UAC
    /// prompt only appears when there is a machine-wide shortcut to remove.
    /// </summary>
    public static bool Disable(out string error)
    {
        error = string.Empty;
        try
        {
            if (File.Exists(ShortcutPath))
                File.Delete(ShortcutPath);

            if (File.Exists(CommonShortcutPath))
                return RemoveMachineShortcutElevated(out error);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Relaunches this executable elevated (UAC) so it can delete the all-users startup shortcut, then
    /// waits for it to finish. Returns false with a message if the user declines the prompt or the
    /// shortcut is still there afterwards.
    /// </summary>
    private static bool RemoveMachineShortcutElevated(out string error)
    {
        error = string.Empty;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = RemoveMachineStartupArgument,
                UseShellExecute = true,
                Verb = "runas",                 // request elevation (shows the UAC prompt)
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using (Process? p = Process.Start(psi))
            {
                p?.WaitForExit();
            }

            // The elevated helper is fire-and-forget; the on-disk result is the source of truth.
            if (File.Exists(CommonShortcutPath))
            {
                error = "The all-users startup entry could not be removed.";
                return false;
            }

            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED (UAC declined)
        {
            error = "Administrator approval is required to remove the all-users startup entry.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Deletes the machine-wide (all-users) startup shortcut. Intended to be called by an instance that
    /// was relaunched elevated with <see cref="RemoveMachineStartupArgument"/>; the caller must already
    /// hold administrator rights (the common Startup folder is otherwise not writable).
    /// </summary>
    public static bool RemoveMachineShortcut(out string error)
    {
        error = string.Empty;
        try
        {
            if (File.Exists(CommonShortcutPath))
                File.Delete(CommonShortcutPath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
