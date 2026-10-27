using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AotForms
{
    /// <summary>
    /// Unified weapon catalog: ID -&gt; display name (via WeaponIndex) -&gt; icon file.
    /// Icons: embedded Resources/Weapons/*.png (normalized lower-hyphen),
    /// fallback old Resources/Icons/*.png, fallback Documents/Weapon Icons folder.
    /// Never throws, never breaks ESP render.
    /// </summary>
    internal static class WeaponCatalog
    {
        private static readonly Dictionary<string, IntPtr> _texCache = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string>? _embeddedMap;

        internal static string NormalizeKey(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s.ToLowerInvariant())
            {
                if (c >= 'a' && c <= 'z') sb.Append(c);
                else if (c >= '0' && c <= '9') sb.Append(c);
            }
            return sb.ToString();
        }

        private static readonly Dictionary<string, string> FileAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["heal-pistol.png"] = "healing-pistol.png",
            ["healsniper.png"] = "healing-sniper.png",
            ["thompson-x.png"] = "thompson.png",
        };

        internal static string IconFileName(string? weaponName)
        {
            if (string.IsNullOrWhiteSpace(weaponName)) return "fist.png";
            string n = weaponName.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            while (n.Contains("--")) n = n.Replace("--", "-");
            if (!n.EndsWith(".png")) n += ".png";
            return n;
        }

        private static readonly Dictionary<string, string> ArabicNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SKORP"] = "سكورب",
            ["FF KNIFE"] = "سكين",
            ["RPK"] = "آر بي كي",
            ["M7"] = "إم7",
            ["HAWK"] = "هوك",
            ["THOMPSON"] = "تومسون",
            ["THOMPSON X"] = "تومسون إيكس",
            ["WINCHESTER"] = "وينشستر",
            ["UZI"] = "يوزي",
            ["GRENADE"] = "قنبلة",
            ["FLASHBANG"] = "قنبلة فلاش",
            ["SMOKE GRENADE"] = "قنبلة دخانية",
            ["FLASH FREEZE"] = "قنبلة ثلجية",
            ["GLOO WALL"] = "جدار غلو",
            ["HEAVY SHURIKEN"] = "شوريكن ثقيل",
            ["LIGHT SHURIKEN"] = "شوريكن خفيف",
            ["GEAR"] = "عتاد",
        };

        /// <summary>Display name (Arabic for new items, English otherwise). Icon lookup always uses the English key.</summary>
        internal static string GetDisplayName(string? weaponName)
        {
            if (string.IsNullOrWhiteSpace(weaponName)) return "";
            if (ArabicNames.TryGetValue(weaponName.Trim(), out var ar)) return ar;
            return weaponName.Trim();
        }

        private static Dictionary<string, string> EmbeddedMap()
        {
            if (_embeddedMap != null) return _embeddedMap;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var asm = typeof(WeaponCatalog).Assembly;
                foreach (var full in asm.GetManifestResourceNames())
                {
                    // Weapons folder ONLY (user request): never resolve from old white Icons.
                    if (full.IndexOf("Resources.Weapons.", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!full.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                    string file = full.Substring(full.LastIndexOf('.') >= 0 ? full.LastIndexOf('.', full.LastIndexOf('.') - 1) + 1 : 0);
                    // simpler: file part after last dot-group = name.png
                    int lastDot = full.LastIndexOf('.');
                    int prevDot = full.LastIndexOf('.', lastDot - 1);
                    string fileName = prevDot >= 0 ? full.Substring(prevDot + 1) : full;
                    string key = NormalizeKey(Path.GetFileNameWithoutExtension(fileName));
                    if (key.Length > 0 && !map.ContainsKey(key)) map[key] = full;
                }
            }
            catch { }
            _embeddedMap = map;
            return map;
        }

        internal static bool TryGetIcon(ClickableTransparentOverlay.Overlay overlay, string? weaponName, out IntPtr tex)
        {
            tex = IntPtr.Zero;
            try
            {
                if (overlay == null || string.IsNullOrWhiteSpace(weaponName)) return false;
                string key = NormalizeKey(weaponName);
                if (key.Length == 0) return false;
                if (_texCache.TryGetValue(key, out var hit) && hit != IntPtr.Zero) { tex = hit; return true; }

                string fileName = IconFileName(weaponName);
                if (FileAliases.TryGetValue(fileName, out var aliased))
                    fileName = aliased;
                // 1) embedded Weapons (only source — never the old white Icons)
                var map = EmbeddedMap();
                string? res = null;
                if (map.TryGetValue(key, out var found)) res = found;
                if (res != null)
                {
                    var asm = typeof(WeaponCatalog).Assembly;
                    using var stream = asm.GetManifestResourceStream(res);
                    if (stream != null)
                    {
                        using var bitmap = new System.Drawing.Bitmap(stream);
                        string tmp = TempImages.PathFor("wx_" + key);
                        bitmap.Save(tmp);
                        overlay.AddOrGetImagePointer(tmp, true, out var ptr, out _, out _);
                        if (ptr != IntPtr.Zero) { _texCache[key] = ptr; tex = ptr; return true; }
                    }
                }
                // 2) suffix scan (assembly prefix may differ) — Weapons folder ONLY.
                try
                {
                    var asm = typeof(WeaponCatalog).Assembly;
                    foreach (var full in asm.GetManifestResourceNames())
                    {
                        if (full.EndsWith($"Weapons.{fileName}", StringComparison.OrdinalIgnoreCase))
                        {
                            using var s = asm.GetManifestResourceStream(full);
                            if (s == null) continue;
                            using var bmp = new System.Drawing.Bitmap(s);
                            string tmp = TempImages.PathFor("wx_" + key);
                            bmp.Save(tmp);
                            overlay.AddOrGetImagePointer(tmp, true, out var ptr, out _, out _);
                            if (ptr != IntPtr.Zero) { _texCache[key] = ptr; tex = ptr; return true; }
                        }
                    }
                }
                catch { }
                // 3) Documents/Weapon Icons folder (original files, any case/space)
                try
                {
                    string docsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Weapon Icons");
                    if (Directory.Exists(docsDir))
                    {
                        foreach (var f in Directory.GetFiles(docsDir, "*.png"))
                        {
                            if (NormalizeKey(Path.GetFileNameWithoutExtension(f)) == key)
                            {
                                overlay.AddOrGetImagePointer(f, false, out var ptr, out _, out _);
                                if (ptr != IntPtr.Zero) { _texCache[key] = ptr; tex = ptr; return true; }
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }
            return false;
        }
    }
}
