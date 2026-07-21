using System;
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
    private const string ShortcutFileName = $"{AppName}.lnk";

    /// <summary>Argument the startup shortcut passes so the launch is quiet.</summary>
    public const string QuietArgument = "-quiet";

    public static string ShortcutPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            ShortcutFileName);

    /// <summary>True if the startup shortcut currently exists.</summary>
    public static bool IsEnabled => File.Exists(ShortcutPath);

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
    /// Removes the startup shortcut if it exists.
    /// </summary>
    public static bool Disable(out string error)
    {
        error = string.Empty;
        try
        {
            if (File.Exists(ShortcutPath))
                File.Delete(ShortcutPath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
