using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace New_Startup_App_Notifier;

internal class Utils
{
    internal static string ResolveIndirectString(string input)
    {
        // If the string doesn't start with '@', it's already a standard string (or empty)
        if (string.IsNullOrEmpty(input) || !input.StartsWith("@"))
        {
            return input;
        }

        StringBuilder outBuf = new StringBuilder(1024);
        int result = NativeMethods.SHLoadIndirectString(input, outBuf, (uint)outBuf.Capacity, IntPtr.Zero);

        // A result of 0 (S_OK) means the string was successfully resolved
        if (result == 0)
        {
            return outBuf.ToString();
        }

        // Fallback to the raw string if resolution fails (e.g., missing DLL)
        return input;
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
