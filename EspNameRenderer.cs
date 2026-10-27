using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AotForms
{
    /// <summary>
    /// Unicode-safe ESP name renderer. Rasterizes with GDI+ (OS shaping + BiDi) and
    /// splits runs across explicit fonts: Arial (base) + Javanese Text + Microsoft
    /// Himalaya + Segoe UI Symbol + embedded Noto (Egyptian Hieroglyphs, Cuneiform).
    /// Every player name shows exactly as-is — never '?' boxes, never ASCII conversion.
    /// </summary>
    internal static class EspNameRenderer
    {
        internal delegate void UploadImageDelegate(string path, bool temp, out IntPtr tex, out uint w, out uint h);
        internal static UploadImageDelegate? Upload;

        private const float FontPx = 16f;
        private const int MaxCache = 400;

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, (IntPtr tex, float w, float h)> Cache = new(StringComparer.Ordinal);

        private static Font? s_base;
        private static Font? s_java;
        private static Font? s_himalaya;
        private static Font? s_symbol;
        private static Font? s_egypt;
        private static Font? s_cunei;
        private static PrivateFontCollection? s_pfc;
        private static readonly List<IntPtr> s_pfcMem = new();
        private static Bitmap? s_meter;
        private static Graphics? s_g;
        private static bool s_fontsTried;
        private static int s_consecFails;
        private static bool s_broken;
        private static DateTime s_brokenSince = DateTime.MinValue;

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        private static Font? TryFamily(string name)
        {
            try { return new Font(name, FontPx, FontStyle.Regular, GraphicsUnit.Pixel); }
            catch { return null; }
        }

        private static Font? LoadPrivate(string suffix, string namePart)
        {
            try
            {
                var asm = typeof(EspNameRenderer).Assembly;
                string? res = null;
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) { res = n; break; }
                }
                if (res == null) return null;
                using var s = asm.GetManifestResourceStream(res);
                if (s == null) return null;
                byte[] buf = new byte[s.Length];
                int read = 0;
                while (read < buf.Length)
                {
                    int r = s.Read(buf, read, buf.Length - read);
                    if (r <= 0) break;
                    read += r;
                }
                IntPtr ptr = Marshal.AllocCoTaskMem(buf.Length);
                Marshal.Copy(buf, 0, ptr, buf.Length);
                s_pfcMem.Add(ptr);
                s_pfc ??= new PrivateFontCollection();
                s_pfc.AddMemoryFont(ptr, buf.Length);
                uint pc = 0;
                AddFontMemResourceEx(ptr, (uint)buf.Length, IntPtr.Zero, ref pc);
                // Families order is NOT insertion order — pick by name.
                foreach (var fam in s_pfc.Families)
                {
                    if (fam.Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                        return new Font(fam, FontPx, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                if (s_pfc.Families.Length == 0) return null;
                return new Font(s_pfc.Families[0], FontPx, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            catch { return null; }
        }

        private static void EnsureFonts()
        {
            if (s_fontsTried) return;
            s_fontsTried = true;
            try
            {
                try { s_base = new Font(new FontFamily("Arial"), FontPx, FontStyle.Bold, GraphicsUnit.Pixel); }
                catch { s_base = new Font(FontFamily.GenericSansSerif, FontPx, FontStyle.Bold, GraphicsUnit.Pixel); }
                s_java = TryFamily("Javanese Text");
                s_himalaya = TryFamily("Microsoft Himalaya");
                s_symbol = TryFamily("Segoe UI Symbol");
                s_egypt = LoadPrivate("Resources.Fonts.NotoEgypt.ttf", "Egyptian");
                s_cunei = LoadPrivate("Resources.Fonts.NotoCunei.ttf", "Cuneiform");
                s_meter = new Bitmap(4, 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                s_g = Graphics.FromImage(s_meter);
                s_g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            }
            catch { }
        }

        private static Font FontFor(int cp)
        {
            if (cp >= 0xA980 && cp <= 0xA9DF && s_java != null) return s_java;
            if (cp >= 0x0F00 && cp <= 0x0FFF && s_himalaya != null) return s_himalaya;
            if (cp >= 0x1D400 && cp <= 0x1D7FF && s_symbol != null) return s_symbol;
            if (cp >= 0x13000 && cp <= 0x1342F && s_egypt != null) return s_egypt;
            if (cp >= 0x12000 && cp <= 0x1247F && s_cunei != null) return s_cunei;
            return s_base!;
        }

        private static bool IsInheriting(char c)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            return cat == UnicodeCategory.NonSpacingMark
                || cat == UnicodeCategory.SpacingCombiningMark
                || cat == UnicodeCategory.EnclosingMark
                || cat == UnicodeCategory.Format;
        }

        private readonly struct Run
        {
            internal readonly Font font;
            internal readonly string text;
            internal Run(Font f, string t) { font = f; text = t; }
        }

        private static List<Run> SplitRuns(string s)
        {
            var runs = new List<Run>();
            if (string.IsNullOrEmpty(s) || s_base == null) return runs;
            int i = 0, n = s.Length;
            Font cur = s_base;
            int start = 0;
            bool first = true;
            while (i < n)
            {
                int cp;
                int chl;
                if (char.IsHighSurrogate(s[i]) && i + 1 < n && char.IsLowSurrogate(s[i + 1]))
                {
                    cp = char.ConvertToUtf32(s[i], s[i + 1]);
                    chl = 2;
                }
                else
                {
                    cp = s[i];
                    chl = 1;
                }
                Font want;
                if (!first && ((chl == 1 && IsInheriting(s[i])) || (chl == 2 && IsInheriting(s[i]))))
                {
                    want = cur;
                }
                else
                {
                    want = FontFor(cp);
                }
                if (first)
                {
                    cur = want; start = i; first = false;
                }
                else if (!ReferenceEquals(want, cur))
                {
                    runs.Add(new Run(cur, s.Substring(start, i - start)));
                    cur = want; start = i;
                }
                i += chl;
            }
            if (!first)
                runs.Add(new Run(cur, s.Substring(start, n - start)));
            return runs;
        }

        private static StringFormat MakeFormat(bool rtl)
        {
            var f = new StringFormat(StringFormatFlags.NoClip | StringFormatFlags.NoWrap)
            {
                Trimming = StringTrimming.None
            };
            if (rtl)
                f.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            return f;
        }

        private static bool HasRtl(string s)
        {
            foreach (char c in s)
            {
                if ((c >= '\u0590' && c <= '\u08FF') || (c >= '\uFB00' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'))
                    return true;
            }
            return false;
        }

        internal static Vector2 Measure(string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;
            if (s_broken)
            {
                if ((DateTime.Now - s_brokenSince).TotalSeconds < 30) return ImGui.CalcTextSize(text);
                s_broken = false;
                s_consecFails = 0;
            }
            lock (Gate)
            {
                EnsureFonts();
                if (s_g == null || s_base == null)
                    return ImGui.CalcTextSize(text);
                try
                {
                    float total = 0, maxH = 0;
                    using var fmt = MakeFormat(false);
                    foreach (var r in SplitRuns(text))
                    {
                        var sz = s_g.MeasureString(r.text, r.font, int.MaxValue, fmt);
                        total += sz.Width;
                        if (sz.Height > maxH) maxH = sz.Height;
                    }
                    return new Vector2((float)Math.Ceiling(total) * scale, (float)Math.Ceiling(maxH) * scale);
                }
                catch { return ImGui.CalcTextSize(text); }
            }
        }

        private static readonly HashSet<string> loggedRenderFails = new();
        private static void LogRenderFail(string text, string why)
        {
            if (!Config.DebugLogging) return;
            try
            {
                string key = why + ":" + text;
                lock (Gate)
                {
                    if (loggedRenderFails.Count > 100 || !loggedRenderFails.Add(key)) return;
                }
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_espdiag.txt"),
                    DateTime.Now.ToString("HH:mm:ss") + " render-" + why + " [" + text + "]\r\n");
            }
            catch { }
        }

        internal static void Draw(ImDrawListPtr dl, Vector2 pos, string text, uint colorAbgr, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (s_broken)
            {
                if ((DateTime.Now - s_brokenSince).TotalSeconds < 30)
                {
                    dl.AddText(pos + new Vector2(1, 1), 0xFF000000, text);
                    dl.AddText(pos, colorAbgr, text);
                    return;
                }
                s_broken = false;
                s_consecFails = 0;
            }
            try
            {
                scale = Math.Clamp(scale, 0.5f, 3f);
                string key = text + "|" + colorAbgr.ToString() + "|" + scale.ToString("F2");
                IntPtr tex = IntPtr.Zero;
                float w = 0, h = 0;
                lock (Gate)
                {
                    if (Cache.TryGetValue(key, out var hit))
                    {
                        tex = hit.tex; w = hit.w; h = hit.h;
                    }
                }
                if (tex == IntPtr.Zero && !BuildTexture(text, colorAbgr, scale, out tex, out w, out h))
                {
                    if (++s_consecFails > 20) { s_broken = true; s_brokenSince = DateTime.Now; }
                    LogRenderFail(text, "build-false");
                    dl.AddText(pos + new Vector2(1, 1), 0xFF000000, text);
                    dl.AddText(pos, colorAbgr, text);
                    return;
                }
                s_consecFails = 0;
                dl.AddImage(tex, pos, pos + new Vector2(w, h));
            }
            catch { }
        }

        private static bool BuildTexture(string text, uint colorAbgr, float scale, out IntPtr tex, out float w, out float h)
        {
            tex = IntPtr.Zero; w = 0; h = 0;
            try
            {
                EnsureFonts();
                if (s_base == null) return false;
                bool rtl = HasRtl(text);
                var runs = SplitRuns(text);
                if (runs.Count == 0) return false;
                using var fmtM = MakeFormat(false);
                float totalW = 0, maxH = 0;
                var sizes = new List<(float w, float h)>();
                lock (Gate)
                {
                    if (s_g == null) return false;
                    foreach (var r in runs)
                    {
                        var sz = s_g.MeasureString(r.text, r.font, int.MaxValue, fmtM);
                        sizes.Add(((float)sz.Width, (float)sz.Height));
                        totalW += sz.Width;
                        if (sz.Height > maxH) maxH = sz.Height;
                    }
                }
                if (totalW <= 0 || maxH <= 0) return false;
                int bw = (int)Math.Ceiling(totalW) + 4, bh = (int)Math.Ceiling(maxH) + 4;
                Color main = Color.FromArgb((int)((colorAbgr >> 24) & 255), (int)(colorAbgr & 255), (int)((colorAbgr >> 8) & 255), (int)((colorAbgr >> 16) & 255));
                using var bmp = new Bitmap(Math.Max(4, bw), Math.Max(4, bh), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    using var fmt = MakeFormat(rtl);
                    using var outline = new SolidBrush(Color.FromArgb(255, 0, 0, 0));
                    using var fill = new SolidBrush(main);
                    float x = rtl ? 2f + totalW : 2f;
                    for (int k = 0; k < runs.Count; k++)
                    {
                        int idx = rtl ? runs.Count - 1 - k : k;
                        var r = runs[idx];
                        float rw = sizes[idx].w, rh = sizes[idx].h;
                        float rx = rtl ? x - rw : x;
                        float ry = 2f + (maxH - rh) * 0.5f;
                        g.DrawString(r.text, r.font, outline, new PointF(rx - 1, ry - 1), fmt);
                        g.DrawString(r.text, r.font, outline, new PointF(rx + 1, ry - 1), fmt);
                        g.DrawString(r.text, r.font, outline, new PointF(rx - 1, ry + 1), fmt);
                        g.DrawString(r.text, r.font, outline, new PointF(rx + 1, ry + 1), fmt);
                        g.DrawString(r.text, r.font, fill, new PointF(rx, ry), fmt);
                        x = rtl ? x - rw : x + rw;
                    }
                }
                string tmp = TempImages.PathFor("espn_" + text);
                bmp.Save(tmp, System.Drawing.Imaging.ImageFormat.Png);
                var up = Upload;
                if (up == null) return false;
                up(tmp, true, out tex, out _, out _);
                if (tex == IntPtr.Zero) return false;
                w = bw * scale; h = bh * scale;
                lock (Gate)
                {
                    if (Cache.Count > MaxCache) Cache.Clear();
                    Cache[text + "|" + colorAbgr.ToString() + "|" + scale.ToString("F2")] = (tex, w, h);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
