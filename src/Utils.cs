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
}
