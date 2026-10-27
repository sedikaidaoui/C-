using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace AotForms
{
    /// <summary>
    /// Per-weapon loot ESP options, driven by the embedded weapon icon set
    /// (always complete, grows automatically). Session-only (not saved to profile).
    /// </summary>
    internal static class WeaponEspConfig
    {
        private static readonly ConcurrentDictionary<string, bool> Enabled =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Color> Colors =
            new(StringComparer.OrdinalIgnoreCase);
        private static List<(string key, string pretty)>? _list;

        internal static List<(string key, string pretty)> List()
        {
            if (_list != null) return _list;
            var list = new List<(string key, string pretty)>();
            try
            {
                var asm = typeof(WeaponEspConfig).Assembly;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var full in asm.GetManifestResourceNames())
                {
                    int wi = full.IndexOf("Resources.Weapons.", StringComparison.OrdinalIgnoreCase);
                    if (wi < 0 || !full.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                    string file = full.Substring(wi + "Resources.Weapons.".Length);
                    string stem = file.Substring(0, file.Length - 4);
                    string norm = WeaponCatalog.NormalizeKey(stem);
                    if (norm.Length == 0 || !seen.Add(norm)) continue;
                    string pretty = string.Join(" ", stem.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    if (pretty.Length > 0)
                        pretty = char.ToUpperInvariant(pretty[0]) + pretty.Substring(1);
                    list.Add((norm, pretty));
                }
            }
            catch { }
            list.Sort((a, b) => string.Compare(a.pretty, b.pretty, StringComparison.OrdinalIgnoreCase));
            _list = list;
            return list;
        }

        internal static bool IsEnabled(string? weaponName)
        {
            try
            {
                string key = WeaponCatalog.NormalizeKey(weaponName);
                if (key.Length == 0) return true;
                if (Enabled.TryGetValue(key, out var v)) return v;
                Enabled[key] = true;
                return true;
            }
            catch { return true; }
        }

        internal static void SetEnabled(string key, bool v)
        {
            try { Enabled[key] = v; } catch { }
        }

        internal static Color GetColor(string? weaponName)
        {
            try
            {
                string key = WeaponCatalog.NormalizeKey(weaponName);
                if (key.Length == 0) return Color.White;
                if (Colors.TryGetValue(key, out var c)) return c;
                Colors[key] = Color.White;
                return Color.White;
            }
            catch { return Color.White; }
        }

        internal static void SetColor(string key, Color c)
        {
            try { Colors[key] = c; } catch { }
        }
    }
}
