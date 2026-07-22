//--------------------------- Global Usings ------------------------------------------
global using static Thio_Background_App_Notifier.MyStrings;

namespace Thio_Background_App_Notifier;

public static class MyStrings
{
    public const string AppName = "Thio's Background App Notifier";
    public const string AppNameDashed = "Thio-Background-App-Notifier";
    public const string repoUrl = "https://github.com/ThioJoe/Thio-Background-App-Notifier";


    public const string DefaultInstallGroupFolder = "ThioJoe";
    public static readonly string DefaultRelativeInstallPath = $"{MyStrings.DefaultInstallGroupFolder}\\{MyStrings.AppName}";

    // Auto fetch the assembly version
    public static string VERSION = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

    // Display name for the Startup-folder shortcut (created by either the installer or the app's "run at startup" toggle).
    // NOTE: the WiX installer hard-codes this same name in ThioBackgroundAppNotifier_Installer.wxs. Keep the two in sync so the app and installer manage the same shortcut file.
    public const string StartupShortcutName = $"{AppName} (Quiet Mode)";
}