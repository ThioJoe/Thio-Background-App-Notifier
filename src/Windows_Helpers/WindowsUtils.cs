using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Thio_Background_App_Notifier;

internal class WindowsUtils
{
    /// <summary>
    /// Resolve ms resource string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
    /// Resolve ms resource string, but if it's a valid resource and can't be resolved, returns null
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    internal static string? ResolveIndirectString_NoFallback(string input)
    {
        // If the string doesn't start with '@', it's already a standard string (or empty), so return as is
        if (string.IsNullOrEmpty(input) || !input.StartsWith("@"))
        {
            return input;
        }

        string resolved = ResolveIndirectString(input);

        // Return null if there was no change meaning it couldn't be resolved
        if (resolved == input)
        {
            return null;
        }
        else
        {
            return resolved;
        }
    }
}
