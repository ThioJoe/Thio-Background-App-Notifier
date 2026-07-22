//--------------------------- Global Usings ------------------------------------------
global using static Thio_Background_App_Notifier.MyStrings;


namespace Thio_Background_App_Notifier;

public static class MyStrings
{
    public const string AppName = "Thio's Background App Notifier";
    public const string AppNameDashed = "Thio-Background-App-Notifier";

    // Display name for the Startup-folder shortcut (created by either the installer or the app's
    // "run at startup" toggle). The "(Quiet Mode)" suffix makes clear the logon launch is meant to
    // be silent, so users don't mistake the invisible startup entry for something broken.
    // NOTE: the WiX installer hard-codes this same name in ThioBackgroundAppNotifier_Installer.wxs;
    // keep the two in sync so the app and installer manage the same shortcut file.
    public const string StartupShortcutName = $"{AppName} (Quiet Mode)";
    public const string DefaultInstallGroupFolder = "ThioJoe";
    public static readonly string DefaultRelativeInstallPath = $"{MyStrings.DefaultInstallGroupFolder}\\{MyStrings.AppName}";
}