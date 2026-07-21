using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thio_Background_App_Notifier;

internal class Utils
{

    /// <summary>
    /// Produces the friendliest available name for a service DisplayName. First tries to resolve
    /// an indirect string (@dll,-id). If that fails, many driver INF entries carry a plain-text
    /// fallback after a ';' (e.g. "@oem154.inf,%ViGEmBus.SVCDESC%;Nefarius Virtual Gamepad
    /// Emulation Service"); use that when present. Otherwise returns the resolved/raw value.
    /// </summary>
    internal static string DeriveFriendlyName(string rawDisplayName)
    {
        string resolved = WindowsUtils.ResolveIndirectString(rawDisplayName);

        // If it's still an unresolved indirect string, look for a human-readable fallback after ';'.
        if (!string.IsNullOrEmpty(resolved) && resolved.StartsWith("@"))
        {
            int semicolon = resolved.LastIndexOf(';');
            if (semicolon >= 0 && semicolon < resolved.Length - 1)
            {
                string fallback = resolved.Substring(semicolon + 1).Trim();
                if (fallback.Length > 0)
                    return fallback;
            }
        }

        return resolved;
    }

    /// <summary>
    /// Normalizes a service ImagePath (or similar) into a stable, comparable key.
    /// Strips the NT object-manager prefix and lower-cases it so the same executable
    /// resolves to the same key on every run. Any launch arguments are preserved so that,
    /// for example, two different svchost-hosted services do not collapse into one.
    /// </summary>
    internal static string NormalizePathForKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string s = raw.Trim();

        // Strip the NT namespace prefix that some driver/service paths use (e.g. \??\C:\...)
        if (s.StartsWith(@"\??\"))
            s = s.Substring(4);

        return s.ToLowerInvariant();
    }
}
