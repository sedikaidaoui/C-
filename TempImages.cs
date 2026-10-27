using System;
using System.IO;
using System.Linq;

namespace AotForms
{
    /// <summary>
    /// Stable per-session temp image paths. The overlay backend may read the file
    /// AFTER AddOrGetImagePointer returns, so deleting temp files immediately is a
    /// use-after-free race (intermittent crash). Fixed paths are simply overwritten.
    /// </summary>
    internal static class TempImages
    {
        internal static readonly string Dir = Path.Combine(Path.GetTempPath(), "ob54img");

        internal static string PathFor(string key)
        {
            try { Directory.CreateDirectory(Dir); } catch { }
            var safe = new string((key ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (safe.Length == 0) safe = "x";
            if (safe.Length > 48) safe = safe.Substring(0, 48);
            return Path.Combine(Dir, safe + "_" + Math.Abs(key.GetHashCode()).ToString("x") + ".png");
        }
    }
}
