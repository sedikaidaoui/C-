using Guna.UI2.WinForms;
using ImGuiNET;
using Memory;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NAudio.Wave;
using AotForms.Components;
using static AotForms.Config;
using static AotForms.WinAPI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace AotForms
{
    internal partial class ESP : ClickableTransparentOverlay.Overlay
    {
        IntPtr hWnd;
        IntPtr HDPlayer;
        private float previousCameraVal = Config.cameraVal;
        private float previousVisionVal = Config.visionVal;
        private bool _styleInitialized = false;
        private const short DefaultMaxHealth = 200;
        private Vector4 lineColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 fovColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 boxColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 box3DColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 skeletonColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private static Dictionary<string, IntPtr> _weaponIcons = new();
        private static int _dragonLogoLoadAttempts;
        private static float _dragonLogoNextTryAt;

        private static IntPtr _espEditorPreviewPtr = IntPtr.Zero;
        private static uint _espEditorPreviewW;
        private static uint _espEditorPreviewH;
        private static bool _espEditorPreviewLoadAttempted;
        private static IntPtr _espEditorWeaponPreviewPtr = IntPtr.Zero;
        private static uint _espEditorWeaponPreviewW;
        private static uint _espEditorWeaponPreviewH;
        private static bool _espEditorWeaponPreviewLoadAttempted;
        private static Vector2 _mainPanelPos;
        private static Vector2 _mainPanelSize;

        private enum EspEditorDragKind { None, LineStart, LineEnd, Box, Name, Distance, Health, Weapon, Hacker }
        private enum EspEditorColorTarget { None, Line, Box, Name, Health, Weapon, Hacker }

        private EspEditorDragKind _espEditorDragKind = EspEditorDragKind.None;
        private EspEditorColorTarget _espEditorColorTarget = EspEditorColorTarget.None;
        private bool _espEditorColorPopupOpen;
        private Vector2 _espEditorColorPopupPos = Vector2.Zero;
        private EspEditorColorTarget _espEditorColorClickTarget = EspEditorColorTarget.None;
        private Vector2 _espEditorColorClickStart = Vector2.Zero;

        private static float EspEditorDistSq(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static float EspEditorDistToSegmentSq(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.X * ab.X + ab.Y * ab.Y;
            if (t < 1e-6f) return EspEditorDistSq(p, a);
            float u = Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / t, 0f, 1f);
            float px = a.X + ab.X * u;
            float py = a.Y + ab.Y * u;
            float dx = p.X - px, dy = p.Y - py;
            return dx * dx + dy * dy;
        }

        private static bool EspEditorPointInRect(Vector2 p, Vector2 mn, Vector2 mx)
        {
            return p.X >= mn.X && p.X <= mx.X && p.Y >= mn.Y && p.Y <= mx.Y;
        }

        private static bool EspEditorPointInRectPad(Vector2 p, Vector2 mn, Vector2 mx, float pad)
        {
            return p.X >= mn.X - pad && p.X <= mx.X + pad && p.Y >= mn.Y - pad && p.Y <= mx.Y + pad;
        }

        private IntPtr _brandIcon = IntPtr.Zero;
        private static readonly Random _hackerRandom = new();
        private static uint _hackerEntityAddress = 0;
        private static DateTime _lastHackerUpdate = DateTime.MinValue;
        private const double HackerUpdateIntervalSeconds = 3.0;
        public static Vector4 ThemeColor = new Vector4(10f / 255f, 105f / 255f, 245f / 255f, 1f); // Royal Blue #0A69F5
        public static ImFontPtr HeaderFont;
        public static ImFontPtr LebelFont;
        public static ImFontPtr TextFont;

        private static float _findWindowTimer = 0f;
        private static int _espMatchEnemyStreak;
        private static bool _espMatchHudLocked;
        private static bool _uiVisible = true;
        public static float _uiAlpha = 1.0f;
        private static float _uiFadeSpeed = 15.0f;
        private static float _uiFadeOutSpeed = 48.0f;
        private static bool _insertKeyPressedLastFrame = false;
        private static bool _vkInsertDownLast = false;
        private static readonly Dictionary<int, float> _tabFade = new();
        private static readonly Dictionary<int, float> _tabAnim = new();
        private static float _tabDropLocalY = -1f;
        private static float _tabDropTargetLocalY = -1f;
        private static float _tabDropLocalX = -1f;
        private static float _tabDropTargetLocalX = -1f;
        private static float _tabDropLocalW = 0f;
        private static float _tabDropLocalH = 0f;
        private static bool _tabDropTargetValid = false;
        private static readonly Dictionary<int, IntPtr> _tabIconPtrs = new();
        private static bool _tabIconsInitialized = false;

        private struct DragonRainParticle
        {
            public float X;
            public float Y;
            public float Speed;
            public float Spin;
            public float Angle;
            public float Size;
            public float Phase;
        }

        private static DragonRainParticle[] _dragonRainTabsParticles = Array.Empty<DragonRainParticle>();
        private static bool _dragonRainTabsInited;
        private static Vector2 _dragonRainTabsLastSize;

        private static DragonRainParticle[] _dragonRainContentParticles = Array.Empty<DragonRainParticle>();
        private static bool _dragonRainContentInited;
        private static Vector2 _dragonRainContentLastSize;

        private static DragonRainParticle[] _dragonRainLoginParticles = Array.Empty<DragonRainParticle>();
        private static bool _dragonRainLoginInited;
        private static Vector2 _dragonRainLoginLastSize;

        private static DragonRainParticle[] _dragonRainBackdropParticles = Array.Empty<DragonRainParticle>();
        private static bool _dragonRainBackdropInited;
        private static Vector2 _dragonRainBackdropLastSize;
        private static bool _faMergedIntoTextFont;
        private static byte[]? _retainedFaSolidBytes;
        private static bool _showTabPopup = false;
        private static float _tabPopupAnim = 0f;
        private static int _previousTab = 0;
        private static int _activeTab = 0;
        private static string? _activeBindingId = null;
        internal static string? ActiveBindingId
        {
            get => _activeBindingId;
            set => _activeBindingId = value;
        }

        internal static bool FaMergedIntoTextFont => _faMergedIntoTextFont;

        private static bool _hooksFunctionsEnabled = true;
        private static bool _hooksToolsEnabled = true;
        private static bool _resetGuestEnabled = false;

        private const string BrandTitle = "RIGLO CHEAT";
        private const string BrandTitleAccent = " CHEAT";
        private const string BrandNameWhite = "RIGLO";
        private const string BrandNamePurple = "CHEAT";
        private static Vector4 BrandAccentBlue => ThemeColor;

        /// <summary>Layout metrics aligned to reference compact menu.</summary>
        private static class UiMetrics
        {
            public const float WindowW = 380f;
            public const float WindowH = 420f;
            public const float OuterPad = 6f;
            public const float ChromeH = 34f;
            public const float ChromeBodyGap = 4f;
            /// <summary>100% solid dark panels as per screenshot.</summary>
            public const float WindowGlassAlpha = 1.0f;
            public const float PanelGlassAlpha = 1.0f;
            public const float CardGlassAlpha = 1.0f;
            public const float HeaderH = 0f;
            public const float HeaderBodyGap = 0f;
            public const float SidebarW = 58f;
            public const float SplitGap = 0f;
            public const float PanelPad = 6f;
            public const float TabW = 44f;
            public const float TabH = 38f;
            public const float TabGap = 8f;
            public const float SidebarLogo = 40f;
            public const float SidebarLogoTop = 8f;
            public const float SidebarLogoBottom = 8f;
            public const float TabUnderlineW = 22f;
            public const float TabUnderlineH = 2.5f;
            public const float TabUnderlineOffsetY = 4f;
            public const float SidebarTabTopPad = 2f;
            public const float SidebarTabBlockBias = 0.0f;
            public const float SidebarTabChevronSize = 0f;
            public const float SidebarTabChevronPad = 0f;
            public const int LoginTabIndex = 5;
            public const int SidebarTabCount = 5;
            public const int MainSidebarTabCount = 5;
        }

        private enum FlowPhase { Login, PostLoginLoad, Main }
        private static FlowPhase _flowPhase = FlowPhase.Main;
        private static string _loginLicense = "";
        private static string _loginUsername = "";
        private static string _loginPassword = "";
        private static string _loginError = "";
        private static bool _loginBusy = false;
        private static Task<string?>? _emulatorInitTask;
        private static bool _startupDone = false;
        private static DateTime _nextEmulatorRetryTime = DateTime.MinValue;
        private static volatile bool _loginDone = true;
        private static bool _loginOk = true;
        private static string _loginResultMsg = "";

        private static float _loaderTime = 0f;
        private static float _connectedFade = 0f;
        private static bool _connected = false;
        private static bool _soundPlayed = false;
        private static float _loaderAngle = 0f;
        private static float _pulseAlpha = 0.5f;
        private static bool _pulseIncreasing = true;

        private static bool _showHookSuccessScreen;
        private static bool _pendingFeatureRuntimeApply;
        private static float _hookSuccessAnimTime;
        private const float HookSuccessAnimDuration = 2.75f;

        private static readonly Vector4 PanelBg = new(6f / 255f, 6f / 255f, 8f / 255f, 1f); // #060608
        private static readonly Vector4 CardHeaderBg = new(19f / 255f, 21f / 255f, 29f / 255f, 1f); // #13151D
        private static readonly Vector4 CardBodyBg = new(11f / 255f, 12f / 255f, 16f / 255f, 1f); // #0B0C10
        private static readonly Vector4 PanelBorder = new(24f / 255f, 25f / 255f, 31f / 255f, 1f);
        /// <summary>Font Awesome solid glyphs matching screenshot tabs: Target, Eye, Wifi, Bomb, Gear.</summary>
        private static readonly string[] TabFaGlyphs =
        {
            "\uF05B", // crosshairs (Aim)
            "\uF06E", // eye (Esp)
            "\uF1EB", // wifi (Misc)
            "\uF1E2", // bomb (Extreme Hooks)
            "\uF013", // cog (Settings)
        };
        private static readonly Vector4 TabAccentRed = new(10f / 255f, 105f / 255f, 245f / 255f, 1f);
        private static readonly Vector4 HexBrandRed = new(10f / 255f, 105f / 255f, 245f / 255f, 1f);
        private static readonly Vector4 HexTabUnderlineRed = new(10f / 255f, 105f / 255f, 245f / 255f, 1f);
        private readonly string[] _comboItems2 = { "Closest To Crosshair", "Target360", "Closest To Player", "Lowest Health" };
        private readonly string[] _comboItems1 = { "Silent Aim", "Aimbot Rage" };
        private readonly string[] _comboItems = { "Top", "Center", "Bottom" };
        private readonly string[] _espPreviewItems = { "Static", "Dynamic" };
        /// <summary>Labels match <see cref="EspNameBoxAnchor"/> order.</summary>
        private readonly string[] _espEditorNameItems =
        {
            "Top of box",
            "Bottom of box",
            "Top Left",
            "Top Right",
            "Bottom Left",
            "Bottom Right",
            "Middle Left",
            "Middle Right",
        };
        /// <summary>Labels match <see cref="EspDistanceBoxAnchor"/> order.</summary>
        private readonly string[] _espEditorDistanceItems =
        {
            "Inside nameplate",
            "Above name (when name at top)",
            "Top of box",
            "Bottom of box",
            "Top Left",
            "Top Right",
            "Bottom Left",
            "Bottom Right",
            "Middle Left",
            "Middle Right",
        };
        private readonly string[] _espEditorHealthItems =
        {
            "Right side",
            "Left side",
            "Top of box",
            "Bottom of box",
        };
        private readonly string[] _espEditorWeaponItems =
        {
            "Top of box",
            "Bottom of box",
            "Top Left",
            "Top Right",
            "Bottom Left",
            "Bottom Right",
            "Middle Left",
            "Middle Right",
        };
        private float _espEditorHealthSectionAnim;
        private float _visLineComboAnim;
        private float _visInfoComboAnim;
        private float _visNameComboAnim;
        private float _visWeaponComboAnim;
        private readonly string[] _headerItems = { "Aim", "Esp", "Colors", "Extras", "Misc" };
        private int _selectedHeader, _comboBox, _comboBox1, _comboBox2, _espPreviewStyleCombo;
        private struct EntityRenderData
        {
            public Vector2 headScreenPos;
            public Vector2 bottomScreenPos;
            public float Distance;
            public bool IsValid;
        }
        Vector4 HsvToRgb(float h, float s, float v)
        {
            int i = (int)(h * 6);
            float f = h * 6 - i;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            float r = 0, g = 0, b = 0;
            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }

            return new Vector4(r, g, b, 1f);
        }
        private void UploadNameImage(string path, bool temp, out IntPtr tex, out uint w, out uint h)
        {
            AddOrGetImagePointer(path, temp, out tex, out w, out h);
        }

        private void RenderEntities()
        {
            if (!Core.HaveMatrix)
                return;

            // Watchdog: data older than 5s means the reader stalled — drop the frozen
            // frame and force a clean re-sync instead of lying (auto-refresh).
            if ((DateTime.Now - Core.LastDataUpdate).TotalSeconds > 5)
            {
                try { Core.Entities.Clear(); InternalMemory.Cache.Clear(); } catch { }
                Core.HaveMatrix = false;
                return;
            }

            var drawList = ImGui.GetBackgroundDrawList();
            var fgDrawList = ImGui.GetForegroundDrawList(); // Cache once per frame
            const float espEdgePad = 72f;
            EspNameRenderer.Upload = UploadNameImage;

            // Update random hacker entity for tag (only one enemy marked)
            if (Config.HackerTag)
            {
                if ((DateTime.Now - _lastHackerUpdate).TotalSeconds > HackerUpdateIntervalSeconds || _hackerEntityAddress == 0)
                {
                    // Zero-allocation reservoir sampling (replaces LINQ .Where().ToList())
                    int validCount = 0;
                    uint chosenAddr = 0;
                    foreach (var e in Core.Entities.Values)
                    {
                        if (e.IsDead || !e.IsKnown) continue;
                        validCount++;
                        if (_hackerRandom.Next(validCount) == 0)
                            chosenAddr = e.Address;
                    }
                    _hackerEntityAddress = chosenAddr;
                    _lastHackerUpdate = DateTime.Now;
                }
            }

            // Snapshot: Data thread mutates the dict live; iterating .Values directly causes flicker/tear.
            Entity[] snapshot = Array.Empty<Entity>();
            try { snapshot = Core.Entities.Values.ToArray(); }
            catch { return; }
            // Live held-weapon ID readout (training-ground mapping). Debug only: hidden otherwise.
            if (Config.DebugLogging && Config.espweapon && Data.LastLocalWeaponId > 0)
            {
                string holdTxt = "HOLDING ID:" + Data.LastLocalWeaponId;
                Vector2 holdPos = _emulatorScreenOffset + new Vector2(30f, 120f);
                fgDrawList.AddText(holdPos + new Vector2(1, 1), 0xFF000000, holdTxt);
                fgDrawList.AddText(holdPos, 0xFF00FF00, holdTxt);
            }
            foreach (var entity in snapshot)
            {
                if (entity == null) continue;
                if (entity.Address == 0) continue;
                if (entity.IsDead) { Data.LogDiag("skip-dead", entity); continue; }
                if (entity.IsTeam == Bool3.True) { Data.LogDiag("skip-team", entity); continue; }
                if (!entity.IsKnown) { Data.LogDiag("skip-unknown", entity); continue; }

                float distW = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                float espRenderMax = Math.Clamp(Config.Esprender, 0f, 200f);
                if (distW > espRenderMax) continue;

                Vector2 headScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                Vector2 bottomScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Root, Core.Width, Core.Height);
                // W2S returns (-1,-1) behind camera; skip if EITHER point is invalid (was &&, caused ghost lines).
                if ((headScreenPos.X < 0f && headScreenPos.Y < 0f) || (bottomScreenPos.X < 0f && bottomScreenPos.Y < 0f)) continue;
                if (headScreenPos.X < -200f || headScreenPos.Y < -200f || bottomScreenPos.X < -200f || bottomScreenPos.Y < -200f) continue;

                // Shift core screen coords (0..Core.Width/Height) into overlay coords (virtual desktop origin).
                headScreenPos += _emulatorScreenOffset;
                bottomScreenPos += _emulatorScreenOffset;

                var entityData = new EntityRenderData
                {
                    headScreenPos = headScreenPos,
                    bottomScreenPos = bottomScreenPos,
                    Distance = distW,
                    IsValid = true
                };

                float CornerHeight = Math.Abs(entityData.headScreenPos.Y - entityData.bottomScreenPos.Y);
                float CornerWidth = CornerHeight * 0.65f;

                // Dynamic ESP colors (RGB mode)
                uint lineColor = ColorToUint32(Config.ESPLineColor);
                uint boxColor = ColorToUint32(Config.ESPBoxColor);
                if (Config.ESPRGB)
                {
                    Color rainbow = ColorFromHSV((float)(ImGui.GetTime() * 100 % 360), 1.0f, 1.0f);
                    lineColor = ColorToUint32(rainbow);
                    boxColor = lineColor;
                }

                Vector2 offLineStart = ScaleEspOffsetPx(Config.EspSnapLineStartOffsetPx, CornerHeight);
                Vector2 offLineEnd = ScaleEspOffsetPx(Config.EspSnapLineEndOffsetPx, CornerHeight);
                Vector2 offBox = ScaleEspOffsetPx(Config.EspBoxOffsetPx, CornerHeight);
                Vector2 offName = ScaleEspOffsetPx(Config.EspNameOffsetPx, CornerHeight);
                Vector2 offHacker = ScaleEspOffsetPx(Config.EspHackerOffsetPx, CornerHeight);
                Vector2 offWeapon = ScaleEspOffsetPx(Config.EspWeaponIconOffsetPx, CornerHeight);
                Vector2 offHealth = ScaleEspOffsetPx(Config.EspHealthBarOffsetPx, CornerHeight);
                Vector2 offDist = ScaleEspOffsetPx(Config.EspDistanceOffsetPx, CornerHeight);

                if (string.IsNullOrEmpty(entity.Name))
                    entity.Name = "Training Bot";

                float distM = MathF.Round(Vector3.Distance(Core.LocalMainCamera, entity.Head));
                string distStr = $"{distM}m";
                string nameDisplay = SanitizeEspDisplayName(entity.Name);

                float healthPercentage = entity.Health > 1000 ? 1f :
                    entity.Health < 0 ? 1f :
                    (float)entity.Health / (entity.Health > 230 ? 500 : 200);
                healthPercentage = Math.Clamp(healthPercentage, 0f, 1f);

                GetEspBoxBounds(entityData.headScreenPos, CornerWidth, CornerHeight, offBox,
                    out float boxL, out float boxT, out float boxR, out float boxB, out float boxMidX, out float boxMidY);
                float gapScaled = Math.Max(3f, EspAnchorGapPx * (CornerHeight / EspOffsetRefCornerHeight));

                float npW = 0f;
                float npH = 0f;
                Vector2 npTL = Vector2.Zero;
                bool showDist = Config.espdistance;
                EspDistanceBoxAnchor distAnchor = Config.EspDistanceAnchor;
                if (!Config.ESPInformation &&
                    (distAnchor == EspDistanceBoxAnchor.InsideNameplate || distAnchor == EspDistanceBoxAnchor.AboveNameWhenNameAtTop))
                {
                    distAnchor = EspDistanceBoxAnchor.BoxTopCenter;
                }
                bool distInNameplate = showDist && Config.ESPInformation && distAnchor == EspDistanceBoxAnchor.InsideNameplate;
                if (Config.ESPInformation)
                {
                    if (distInNameplate)
                        ComputeEspNameplateSize(nameDisplay, distStr, out npW, out npH);
                    else
                        ComputeEspNameplateSizeNameOnly(nameDisplay, out npW, out npH);
                    npTL = GetNameBoxAnchorTopLeft(Config.EspNameAnchor, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled, npW, npH) + offName;
                }

                if (Config.ESPLine)
                {
                    Vector2 lineEndBase = Config.ESPLineEnd == 2 ? entityData.bottomScreenPos
                        : Config.ESPLineEnd == 1 ? (entityData.headScreenPos + entityData.bottomScreenPos) * 0.5f
                        : entityData.headScreenPos;
                    Vector2 endPos = lineEndBase + offLineEnd;
                    Vector2 lineStart = new Vector2(Core.Width / 2f, 10f);
                    switch (Config.ESPLinePosition)
                    {
                        case LinePosition.Top:
                            lineStart = new Vector2(Core.Width / 2f, 10f);
                            endPos = lineEndBase + offLineEnd;
                            break;
                        case LinePosition.Center:
                            lineStart = new Vector2(Core.Width / 2f, Core.Height / 2f);
                            endPos = lineEndBase + offLineEnd;
                            break;
                        case LinePosition.Bottom:
                            lineStart = new Vector2(Core.Width / 2f, Core.Height - 10f);
                            endPos = entityData.bottomScreenPos + offLineEnd;
                            break;
                    }
                    lineStart += offLineStart + _emulatorScreenOffset;

                    if (Config.EspShowDragonLogo && Config.ESPLinePosition == LinePosition.Top)
                    {
                        const float logoSize = 28f;
                        IntPtr logoPtr = CustomNotification.DragonLogoPtr;
                        if (logoPtr != IntPtr.Zero)
                        {
                            Vector2 logoPos = new Vector2(lineStart.X - (logoSize * 0.5f), lineStart.Y);
                            drawList.AddImage(logoPtr, logoPos, logoPos + new Vector2(logoSize, logoSize));
                            lineStart.Y += logoSize + 6f;
                        }
                    }

                    if (Config.ESPLineStyle == 1)
                    {
                        // Curved instead of straight.
                        Vector2 mid = (lineStart + endPos) * 0.5f;
                        Vector2 dir = endPos - lineStart;
                        float len = Math.Max(1f, dir.Length());
                        Vector2 nrm = new Vector2(-dir.Y / len, dir.X / len);
                        Vector2 ctrl = mid + nrm * (len * 0.18f);
                        drawList.AddBezierQuadratic(lineStart, ctrl, endPos, lineColor, 1.5f, 12);
                    }
                    else if (Config.ESPLineStyle == 2)
                    {
                        // Zigzag instead of straight: right-left segments along the line.
                        Vector2 dir = endPos - lineStart;
                        float len = Math.Max(1f, dir.Length());
                        Vector2 nrm = new Vector2(-dir.Y / len, dir.X / len);
                        const int segs = 8;
                        Vector2 prev = lineStart;
                        for (int sgi = 1; sgi <= segs; sgi++)
                        {
                            float tseg = sgi / (float)segs;
                            float off = (sgi % 2 == 0) ? 0f : 7f;
                            Vector2 pp = lineStart + dir * tseg + nrm * off;
                            drawList.AddLine(prev, pp, lineColor, 1.5f);
                            prev = pp;
                        }
                    }
                    else
                    {
                        drawList.AddLine(lineStart, endPos, lineColor, 1f);
                    }
                }

                if (Config.ESPBox)
                {
                    float th = Math.Clamp(Config.ESPThickness, 1f, 5f);
                    float bx0 = entityData.headScreenPos.X - (CornerWidth / 2) + offBox.X;
                    float by0 = entityData.headScreenPos.Y + offBox.Y;
                    if (Config.ESPBoxStyle == 2)
                    {
                        // 3D box: front rect + offset back rect + connectors.
                        float depth = Math.Min(CornerWidth, CornerHeight) * 0.22f;
                        Vector2 f0 = new Vector2(bx0, by0), f1 = new Vector2(bx0 + CornerWidth, by0 + CornerHeight);
                        Vector2 b0 = f0 + new Vector2(depth, -depth), b1 = f1 + new Vector2(depth, -depth);
                        drawList.AddRect(b0, b1, boxColor, 0f, ImDrawFlags.None, th);
                        drawList.AddRect(f0, f1, boxColor, 0f, ImDrawFlags.None, th);
                        drawList.AddLine(f0, b0, boxColor, th);
                        drawList.AddLine(new Vector2(f1.X, f0.Y), new Vector2(b1.X, b0.Y), boxColor, th);
                        drawList.AddLine(new Vector2(f0.X, f1.Y), new Vector2(b0.X, b1.Y), boxColor, th);
                        drawList.AddLine(f1, b1, boxColor, th);
                    }
                    else if (Config.ESPBoxStyle == 1)
                    {
                        // Full 2D rect style.
                        Vector2 mn = new Vector2(bx0, by0), mx = new Vector2(bx0 + CornerWidth, by0 + CornerHeight);
                        drawList.AddRect(mn - new Vector2(1, 1), mx + new Vector2(1, 1), 0xFF000000, 0f, ImDrawFlags.None, th + 1.5f);
                        drawList.AddRect(mn, mx, boxColor, 0f, ImDrawFlags.None, th);
                    }
                    else
                    {
                        DrawCorneredBoxDrawList(drawList, bx0, by0, CornerWidth, CornerHeight, boxColor, th);
                    }
                }

                if (Config.ESPFillBox)
                {
                    float bx = entityData.headScreenPos.X - (CornerWidth / 2) + offBox.X;
                    float by = entityData.headScreenPos.Y + offBox.Y;
                    uint fill = ColorToUint32(Config.ESPFillBoxColor);
                    drawList.AddRectFilled(new Vector2(bx, by), new Vector2(bx + CornerWidth, by + CornerHeight), fill);
                }

                if (Config.ESPFullBox)
                {
                    float th = Math.Clamp(Config.ESPThickness, 1f, 5f);
                    float bx = entityData.headScreenPos.X - (CornerWidth / 2) + offBox.X;
                    float by = entityData.headScreenPos.Y + offBox.Y;
                    Vector2 mn = new Vector2(bx, by), mx = new Vector2(bx + CornerWidth, by + CornerHeight);
                    drawList.AddRect(mn - new Vector2(1, 1), mx + new Vector2(1, 1), 0xFF000000, 0f, ImDrawFlags.None, th + 1.5f);
                    drawList.AddRect(mn, mx, boxColor, 0f, ImDrawFlags.None, th);
                }

                // Bunny-ears marker (2D sprite that tracks Head; follows movement like the box).
                if (Config.ESPBunnyEars)
                {
                    Vector2 earC = entityData.headScreenPos + new Vector2(0, -14f * (CornerHeight / 200f) - 10f);
                    float er = Math.Clamp(CornerWidth * 0.09f, 3f, 9f);
                    uint earCol = boxColor;
                    drawList.AddLine(earC + new Vector2(-er * 1.2f, 0), earC + new Vector2(-er * 1.8f, -er * 2.2f), earCol, 2f);
                    drawList.AddLine(earC + new Vector2(er * 1.2f, 0), earC + new Vector2(er * 1.8f, -er * 2.2f), earCol, 2f);
                    drawList.AddCircleFilled(earC + new Vector2(-er * 1.8f, -er * 2.2f), er * 0.55f, earCol);
                    drawList.AddCircleFilled(earC + new Vector2(er * 1.8f, -er * 2.2f), er * 0.55f, earCol);
                }

                Vector2 nameAnchorSize = Config.ESPName && !Config.ESPInformation
                    ? EspCalcTextSizeName(nameDisplay)
                    : new Vector2(60, 14);
                Vector2 namePos = GetNameBoxAnchorTopLeft(Config.EspNameAnchor, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled, nameAnchorSize.X, nameAnchorSize.Y) + offName;

                // Clipping check must use core coords (origin at emulator), not overlay coords.
                Vector2 headCore = entityData.headScreenPos - _emulatorScreenOffset;
                if (headCore.X >= -espEdgePad && headCore.Y >= -espEdgePad &&
                    headCore.X <= Core.Width + espEdgePad && headCore.Y <= Core.Height + espEdgePad)
                {
                    if (Config.ESPInformation)
                    {
                        var fg = fgDrawList;
                        int teamDigit = (int)(entity.Address % 10);
                        DrawEspInfoNameplate(fg, npTL, npW, nameDisplay, distStr, teamDigit, healthPercentage, entity.IsKnocked, showDist && distInNameplate);
                        if (showDist && !distInNameplate)
                        {
                            Vector2 distSz = ImGui.CalcTextSize(distStr);
                            Vector2 distTL = GetDistanceBoxAnchorTopLeft(distAnchor, Config.EspNameAnchor, distSz, npTL, npW, npH, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled) + offDist;
                            fg.AddText(distTL, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.93f, 0.2f, 1f)), distStr);
                        }
                    }
                    else if (Config.ESPName)
                    {
                        var fg = fgDrawList;
                        float nameScale = Math.Clamp(Config.ESPNameSize, 0.5f, 3f);
                        Vector2 nameSz = EspCalcTextSizeName(nameDisplay);
                        nameSz = new Vector2(nameSz.X * nameScale, nameSz.Y * nameScale);
                        Vector2 simpleNamePos = GetNameBoxAnchorTopLeft(Config.EspNameAnchor, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled, nameSz.X, nameSz.Y) + offName;
                        // Unicode-safe bold name (GDI+ shaping/BiDi/fallback); original chars untouched.
                        EspNameRenderer.Draw(fg, simpleNamePos, nameDisplay, ColorToUint32(Config.ESPNameColor), nameScale);
                    }

                    // Distance can render independently of ESP Information.
                    if (showDist && !Config.ESPInformation)
                    {
                        Vector2 distSz = ImGui.CalcTextSize(distStr);
                        Vector2 distTL = GetDistanceBoxAnchorTopLeft(distAnchor, Config.EspNameAnchor, distSz, npTL, npW, npH, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled) + offDist;
                        
                        // Drop shadow
                        fgDrawList.AddText(distTL + new Vector2(1, 1), 0xFF000000, distStr);
                        // Main text
                        fgDrawList.AddText(distTL, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.93f, 0.2f, 1f)), distStr);
                    }

                    if (Config.HackerTag && entity.Address == _hackerEntityAddress)
                    {
                        string hackerText = "HACKER";
                        Vector2 hackerSize = ImGui.CalcTextSize(hackerText);
                        float anchorW = Config.ESPInformation ? npW : nameAnchorSize.X;
                        float anchorX = Config.ESPInformation ? npTL.X : namePos.X;
                        float anchorTopY = Config.ESPInformation ? npTL.Y : namePos.Y;
                        Vector2 hackerPos = new Vector2(
                            anchorX + (anchorW - hackerSize.X) * 0.5f,
                            anchorTopY - hackerSize.Y - 3f);
                        hackerPos += offHacker;

                        uint hackerCol = ColorToUint32(Config.HackerTagColor);
                        var fg = fgDrawList;
                        fg.AddText(hackerPos + new Vector2(1, 1), ColorToUint32(Color.Black), hackerText);
                        fg.AddText(hackerPos, hackerCol, hackerText);
                    }


                    if (Config.espweapon)
                    {
                        // Use core coordinates for clipping (overlay coords include _emulatorScreenOffset).
                        if (headCore.X >= -espEdgePad && headCore.Y >= -espEdgePad && headCore.X <= Core.Width + espEdgePad && headCore.Y <= Core.Height + espEdgePad)
                        {
                            string weaponName = (entity.WeaponName ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(weaponName))
                            {
                                try
                                {
                                    EspNameBoxAnchor wMap = MapWeaponAnchorToNameAnchor(Config.EspWeaponAnchor);
                                    float wScale = Math.Clamp(Config.ESPWeaponIconSize, 0.5f, 3f);
                                    bool wantIcon = Config.ESPWeaponIcon;
                                    bool wantText = Config.ESPWeaponName;
                                    IntPtr imagehandle = IntPtr.Zero;
                                    if (wantIcon)
                                        WeaponCatalog.TryGetIcon(this, weaponName, out imagehandle);

                                    Vector2 iconSize = new Vector2(60 * Config.iconsize * wScale, 20 * Config.iconsize * wScale);
                                    Vector2 iconPos = GetNameBoxAnchorTopLeft(wMap, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled, iconSize.X, iconSize.Y);
                                    iconPos += offWeapon;

                                    if (wantIcon && imagehandle != IntPtr.Zero)
                                    {
                                        if (!Config.rgb)
                                        {
                                            fgDrawList.AddImage(imagehandle, iconPos, iconPos + iconSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(Config.ICONCOLOR));
                                        }
                                        else
                                        {
                                            const int slices = 10;
                                            float time = (float)ImGui.GetTime();
                                            float sliceWidth = iconSize.X / slices;
                                            for (int i = 0; i < slices; i++)
                                            {
                                                float xStart = iconPos.X + i * sliceWidth;
                                                float xEnd = xStart + sliceWidth;
                                                float uvStart = (float)i / slices;
                                                float uvEnd = (float)(i + 1) / slices;
                                                float hue = ((float)i / slices + time * 0.25f) % 1.0f;
                                                Vector4 rgbColor = HsvToRgb(hue, 1f, 1f);
                                                rgbColor.W = Config.ICONCOLOR.W;
                                                fgDrawList.AddImage(imagehandle, new Vector2(xStart, iconPos.Y), new Vector2(xEnd, iconPos.Y + iconSize.Y), new Vector2(uvStart, 0), new Vector2(uvEnd, 1), ImGui.ColorConvertFloat4ToU32(rgbColor));
                                            }
                                        }
                                        // HP number above the weapon icon.
                                        if (entity.Health > 0)
                                        {
                                            string hpStr = entity.Health.ToString();
                                            Vector2 hsz = ImGui.CalcTextSize(hpStr);
                                            Vector2 hpPos = new Vector2(iconPos.X + (iconSize.X - hsz.X) * 0.5f, iconPos.Y - hsz.Y - 2f);
                                            fgDrawList.AddText(hpPos + new Vector2(1, 1), 0xFF000000, hpStr);
                                            fgDrawList.AddText(hpPos, ColorToUint32(Config.ESPHealthColor), hpStr);
                                        }
                                        if (wantText)
                                        {
                                            string dispName = WeaponCatalog.GetDisplayName(weaponName);
                                            // Name beside the icon (right, centered); below it if no room.
                                            Vector2 tsz = EspNameRenderer.Measure(dispName);
                                            Vector2 tpos = new Vector2(iconPos.X + iconSize.X + 4f, iconPos.Y + (iconSize.Y - tsz.Y) * 0.5f);
                                            if (tpos.X + tsz.X > boxR + 90f)
                                                tpos = new Vector2(iconPos.X + (iconSize.X - tsz.X) * 0.5f, iconPos.Y + iconSize.Y + 1f);
                                            EspNameRenderer.Draw(fgDrawList, tpos, dispName, ColorToUint32(Config.ESPNameColor));
                                        }
                                        // Weapon distance = where that gun is.
                                        {
                                            Vector2 dsz = ImGui.CalcTextSize(distStr);
                                            Vector2 dpos = new Vector2(iconPos.X + (iconSize.X - dsz.X) * 0.5f, iconPos.Y + iconSize.Y + 2f);
                                            fgDrawList.AddText(dpos + new Vector2(1, 1), 0xFF000000, distStr);
                                            fgDrawList.AddText(dpos, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.93f, 0.2f, 1f)), distStr);
                                        }
                                    }
                                    else if (wantText)
                                    {
                                        // Text fallback: icon missing -> still show which weapon (never blank, never skip entity).
                                        string dispName = WeaponCatalog.GetDisplayName(weaponName);
                                        Vector2 tsz = EspNameRenderer.Measure(dispName);
                                        Vector2 tpos = GetNameBoxAnchorTopLeft(wMap, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gapScaled, tsz.X, tsz.Y) + offWeapon;
                                        EspNameRenderer.Draw(fgDrawList, tpos, dispName, ColorToUint32(Config.ESPNameColor));
                                    }
                                }
                                catch { /* weapon draw must never break the entity loop */ }
                            }
                        }

                    }
                    var vList = fgDrawList;
                    if (Config.ESPHealth && !Config.ESPInformation)
                    {
                        float t = 4f;
                        HealthGradientColors(healthPercentage, entity.IsKnocked, out uint healthBarColor, out uint healthBarColorDark);

                        switch (Config.EspHealthAnchor)
                        {
                            case EspHealthBoxAnchor.BoxTop:
                                {
                                    float y0 = boxT - gapScaled - t;
                                    Vector2 o = offHealth;
                                    vList.AddRect(new Vector2(boxL + o.X - 1, y0 + o.Y - 1), new Vector2(boxR + o.X + 1, y0 + t + o.Y + 1), 0xFF000000, 2.0f);
                                    vList.AddRectFilled(new Vector2(boxL + o.X, y0 + o.Y), new Vector2(boxR + o.X, y0 + t + o.Y), 0x90000000, 2.0f);
                                    float fillW = CornerWidth * healthPercentage;
                                    vList.AddRectFilledMultiColor(new Vector2(boxL + o.X, y0 + o.Y), new Vector2(boxL + fillW + o.X, y0 + t + o.Y), healthBarColor, healthBarColor, healthBarColorDark, healthBarColorDark);
                                    break;
                                }
                            case EspHealthBoxAnchor.BoxBottom:
                                {
                                    float y0 = boxB + gapScaled;
                                    Vector2 o = offHealth;
                                    vList.AddRect(new Vector2(boxL + o.X - 1, y0 + o.Y - 1), new Vector2(boxR + o.X + 1, y0 + t + o.Y + 1), 0xFF000000, 2.0f);
                                    vList.AddRectFilled(new Vector2(boxL + o.X, y0 + o.Y), new Vector2(boxR + o.X, y0 + t + o.Y), 0x90000000, 2.0f);
                                    float fillW = CornerWidth * healthPercentage;
                                    vList.AddRectFilledMultiColor(new Vector2(boxL + o.X, y0 + o.Y), new Vector2(boxL + fillW + o.X, y0 + t + o.Y), healthBarColor, healthBarColor, healthBarColorDark, healthBarColorDark);
                                    break;
                                }
                            case EspHealthBoxAnchor.BoxLeft:
                                {
                                    Vector2 p = new Vector2(boxL - 5f - t, boxT) + offHealth;
                                    vList.AddRect(new Vector2(p.X - 1, p.Y - 1), new Vector2(p.X + t + 1, p.Y + CornerHeight + 1), 0xFF000000, 2.0f);
                                    vList.AddRectFilled(p, new Vector2(p.X + t, p.Y + CornerHeight), 0x90000000, 2.0f);
                                    vList.AddRectFilledMultiColor(new Vector2(p.X, p.Y + CornerHeight * (1 - healthPercentage)), new Vector2(p.X + t, p.Y + CornerHeight), healthBarColor, healthBarColorDark, healthBarColorDark, healthBarColor);
                                    break;
                                }
                            default:
                                {
                                    Vector2 healthBarPosRight = new Vector2(boxR + 5f, boxT) + offHealth;
                                    float healthBarWidth = t;
                                    float healthBarFullHeight = CornerHeight;
                                    vList.AddRect(new Vector2(healthBarPosRight.X - 1, healthBarPosRight.Y - 1),
                                        new Vector2(healthBarPosRight.X + healthBarWidth + 1, healthBarPosRight.Y + healthBarFullHeight + 1),
                                        0xFF000000, 2.0f);
                                    vList.AddRectFilled(new Vector2(healthBarPosRight.X, healthBarPosRight.Y),
                                        new Vector2(healthBarPosRight.X + healthBarWidth, healthBarPosRight.Y + healthBarFullHeight),
                                        0x90000000, 2.0f);
                                    vList.AddRectFilledMultiColor(new Vector2(healthBarPosRight.X, healthBarPosRight.Y + healthBarFullHeight * (1 - healthPercentage)),
                                        new Vector2(healthBarPosRight.X + healthBarWidth, healthBarPosRight.Y + healthBarFullHeight),
                                        healthBarColor, healthBarColorDark, healthBarColorDark, healthBarColor);
                                    break;
                                }
                        }
                    }
                    if (Config.ESPHealthText)
                    {
                        if (!entity.IsKnocked)
                        {
                            DrawHealthTextFixed(entity, entity.Health);
                        }

                    }

                }



                if (Config.ESPSkeleton)
                {
                    DrawSkeleton(entity);
                }

            }

            if (Config.ESPRadar)
                DrawEspRadar(fgDrawList, snapshot);

            if (Config.LootWeaponsESP)
                DrawLootEsp(fgDrawList);

        }

        /// <summary>Loot Weapons ESP: ground items with line + icon + name + live distance (max 200m).</summary>
        private void DrawLootEsp(ImDrawListPtr fg)
        {
            try
            {
                KeyValuePair<long, LootItem>[] items;
                try { items = Core.LootItems.ToArray(); }
                catch { return; }
                if (items.Length == 0) return;
                const float edgePad = 72f;
                Vector2 lineFrom = _emulatorScreenOffset + new Vector2(Core.Width / 2f, Core.Height - 12f);
                foreach (var kv in items)
                {
                    var li = kv.Value;
                    if (li == null || string.IsNullOrWhiteSpace(li.Name)) continue;
                    if (!WeaponEspConfig.IsEnabled(li.Name)) continue;
                    float dist = Vector3.Distance(Core.LocalMainCamera, li.Position);
                    if (dist > 200f) continue;
                    Vector2 sp = W2S.WorldToScreen(Core.CameraMatrix, li.Position, Core.Width, Core.Height);
                    if (sp.X < -edgePad || sp.Y < -edgePad || sp.X > Core.Width + edgePad || sp.Y > Core.Height + edgePad)
                        continue;
                    sp += _emulatorScreenOffset;
                    uint wcol = ColorToUint32(WeaponEspConfig.GetColor(li.Name));
                    if (Config.LootShowLine)
                        fg.AddLine(lineFrom, sp, wcol, 1f);
                    Vector2 isz = new Vector2(46 * Config.iconsize, 15 * Config.iconsize);
                    Vector2 ipos = new Vector2(sp.X - isz.X / 2f, sp.Y - isz.Y - 4f);
                    bool iconOk = false;
                    if (Config.LootShowIcon && Config.ESPWeaponIcon
                        && WeaponCatalog.TryGetIcon(this, li.Name, out var tex) && tex != IntPtr.Zero)
                    {
                        fg.AddImage(tex, ipos, ipos + isz, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(Config.ICONCOLOR));
                        iconOk = true;
                    }
                    if (Config.LootShowBox)
                    {
                        Vector2 b0 = iconOk ? ipos : sp - new Vector2(14f, 10f);
                        Vector2 b1 = iconOk ? ipos + isz : sp + new Vector2(14f, 10f);
                        fg.AddRect(b0 - new Vector2(1, 1), b1 + new Vector2(1, 1), 0xFF000000, 0f, ImDrawFlags.None, 2f);
                        fg.AddRect(b0, b1, wcol, 0f, ImDrawFlags.None, 1f);
                    }
                    if (Config.LootShowName || Config.LootShowDist)
                    {
                        string label = "";
                        if (Config.LootShowName && Config.ESPWeaponName)
                            label = WeaponCatalog.GetDisplayName(li.Name);
                        if (Config.LootShowDist)
                            label = (label.Length > 0 ? label + " " : "") + ((int)dist) + "m";
                        if (label.Length > 0)
                        {
                            Vector2 tsz = EspNameRenderer.Measure(label);
                            Vector2 tpos = iconOk
                                ? new Vector2(sp.X - tsz.X / 2f, ipos.Y + isz.Y + 1f)
                                : new Vector2(sp.X - tsz.X / 2f, sp.Y + 2f);
                            EspNameRenderer.Draw(fg, tpos, label, ColorToUint32(Config.ESPNameColor));
                        }
                    }
                }
            }
            catch { }
        }

        private void DrawEspRadar(ImDrawListPtr dl, Entity[] snapshot)
        {
            try
            {
                // Arrow keys move the radar anywhere on screen.
                const float step = 6f;
                if ((WinAPI.GetAsyncKeyState(System.Windows.Forms.Keys.Left) & 0x8000) != 0) Config.RadarPosX -= step;
                if ((WinAPI.GetAsyncKeyState(System.Windows.Forms.Keys.Right) & 0x8000) != 0) Config.RadarPosX += step;
                if ((WinAPI.GetAsyncKeyState(System.Windows.Forms.Keys.Up) & 0x8000) != 0) Config.RadarPosY -= step;
                if ((WinAPI.GetAsyncKeyState(System.Windows.Forms.Keys.Down) & 0x8000) != 0) Config.RadarPosY += step;
                Config.RadarPosX = Math.Clamp(Config.RadarPosX, -1200f, 1200f);
                Config.RadarPosY = Math.Clamp(Config.RadarPosY, -1200f, 1200f);

                float size = Math.Clamp(Config.ESPRadarSize, 80f, 400f);
                float range = Math.Max(20f, Config.ESPRadarRange);
                Vector2 center = _emulatorScreenOffset + new Vector2(90f + size * 0.5f + Config.RadarPosX, 90f + size * 0.5f + Config.RadarPosY);
                float r = size * 0.5f;
                dl.AddCircleFilled(center, r + 4f, 0x90000000);
                dl.AddCircle(center, r, 0xFFFFFFFF, 48, 1.5f);
                dl.AddCircle(center, r * 0.5f, 0x60FFFFFF, 32, 1f);
                dl.AddLine(center + new Vector2(-r, 0), center + new Vector2(r, 0), 0x40FFFFFF, 1f);
                dl.AddLine(center + new Vector2(0, -r), center + new Vector2(0, r), 0x40FFFFFF, 1f);
                dl.AddCircleFilled(center, 3f, 0xFF00FF00);
                // Facing tick: up is always where YOU look.
                dl.AddLine(center, center + new Vector2(0, -r - 8f), 0xFF00FF00, 2f);
                if (snapshot == null) return;
                // Heading-relative: rotate world delta by camera yaw so ahead = up, right = right.
                Vector3 fwd = new Vector3(Core.CameraMatrix.M13, 0f, Core.CameraMatrix.M33);
                if (fwd.LengthSquared() < 1e-8f) fwd = new Vector3(0f, 0f, 1f);
                fwd = Vector3.Normalize(fwd);
                Vector3 lp = Core.LocalMainCamera;
                foreach (var e in snapshot)
                {
                    if (e == null || e.IsDead || !e.IsKnown) continue;
                    Vector3 d = e.Head - lp;
                    float side = d.X * fwd.Z - d.Z * fwd.X;
                    float ahead = d.X * fwd.X + d.Z * fwd.Z;
                    float dist = MathF.Sqrt(side * side + ahead * ahead);
                    if (dist < 0.01f) continue;
                    if (dist > range)
                    {
                        side *= range / dist;
                        ahead *= range / dist;
                        dist = range;
                    }
                    float k = r / range;
                    Vector2 p = center + new Vector2(side * k, -ahead * k);
                    uint col = e.IsKnocked ? 0xFF00BFFF : 0xFF0000FF;
                    dl.AddCircleFilled(p, 4f, 0xFF000000);
                    dl.AddCircleFilled(p, 3f, col);
                }
            }
            catch { }
        }

        private static void ResetEspMatchTimerState()
        {
            Config.EnemyTimerActive = false;
            _espMatchEnemyStreak = 0;
            _espMatchHudLocked = false;
            Config.LastEnemySeen = DateTime.Now;
        }

        private static bool EspMatchTimerSeesEnemy()
        {
            foreach (var entity in Core.Entities.Values)
            {
                if (entity.IsDead || !entity.IsKnown)
                    continue;

                float dist = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist > Config.espran)
                    continue;

                Vector2 headScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                Vector2 bottomScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Root, Core.Width, Core.Height);
                if (headScreenPos.X < 0f && headScreenPos.Y < 0f)
                    continue;
                if (bottomScreenPos.X < 0f && bottomScreenPos.Y < 0f)
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>Logic only — no draw. Keeps HUD latched to avoid frame-to-frame flicker.</summary>
        private void UpdateEspMatchTimerLogic()
        {
            if (!Config.EspTimer)
            {
                ResetEspMatchTimerState();
                return;
            }

            if (!Core.HaveMatrix)
                return;

            const int enemyConfirmFrames = 10;
            bool enemySeen = EspMatchTimerSeesEnemy();

            if (enemySeen)
            {
                _espMatchEnemyStreak = Math.Min(_espMatchEnemyStreak + 1, enemyConfirmFrames + 4);
                Config.LastEnemySeen = DateTime.Now;

                if (!_espMatchHudLocked && _espMatchEnemyStreak >= enemyConfirmFrames)
                {
                    _espMatchHudLocked = true;
                    Config.EnemyTimerActive = true;
                    Config.EnemyTimerStart = DateTime.Now;
                    Config.LastEnemySeen = DateTime.Now;
                }
            }
            else if (!_espMatchHudLocked)
            {
                _espMatchEnemyStreak = Math.Max(0, _espMatchEnemyStreak - 1);
            }

            if (_espMatchHudLocked && Config.EnemyTimerActive && !enemySeen)
            {
                double sinceSeen = (DateTime.Now - Config.LastEnemySeen).TotalSeconds;
                int elapsedTotal = (int)(DateTime.Now - Config.EnemyTimerStart).TotalSeconds;

                if (sinceSeen > Config.ResetAfterNoEnemy
                    || (elapsedTotal >= Config.EspMatchTimerDurationSec && sinceSeen > 5))
                {
                    ResetEspMatchTimerState();
                }
            }
        }

        private void DrawEspMatchTimerOverlay()
        {
            if (!Config.EspTimer || !_espMatchHudLocked || !Config.EnemyTimerActive || !Core.HaveMatrix)
                return;

            int durationSec = Config.EspMatchTimerDurationSec;
            Config.EnemyTimerDuration = durationSec;

            int elapsed = (int)(DateTime.Now - Config.EnemyTimerStart).TotalSeconds;
            int remaining = Math.Max(0, durationSec - elapsed);
            float progress = durationSec > 0 ? remaining / (float)durationSec : 0f;

            var dl = ImGui.GetForegroundDrawList();
            dl.PushClipRect(_emulatorClipMin, _emulatorClipMax, true);
            DrawEspMatchTimerHud(dl, remaining, durationSec, progress);
            dl.PopClipRect();
        }

        private void DrawEspMatchTimerHud(ImDrawListPtr dl, int remaining, int durationSec, float progress)
        {
            const float panelW = 210f;
            const float panelH = 58f;
            const float topY = 72f;

            float centerX = _emulatorScreenOffset.X + Core.Width * 0.5f;
            float panelX = centerX - panelW * 0.5f;
            float panelY = _emulatorScreenOffset.Y + topY;
            Vector2 pMin = new Vector2(panelX, panelY);
            Vector2 pMax = pMin + new Vector2(panelW, panelH);

            bool matchComplete = remaining <= 0;
            progress = Math.Clamp(progress, 0f, 1f);

            Vector4 accent = matchComplete
                ? new Vector4(1f, 0.35f, 0.30f, 1f)
                : ThemeColor;

            dl.AddRectFilled(pMin + new Vector2(2f, 3f), pMax + new Vector2(2f, 3f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.40f)), 7f);
            dl.AddRectFilled(pMin, pMax,
                ImGui.ColorConvertFloat4ToU32(new Vector4(8f / 255f, 10f / 255f, 16f / 255f, 0.88f)), 7f);
            dl.AddRect(pMin, pMax,
                ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.55f)),
                7f, ImDrawFlags.None, 1f);
            dl.AddRectFilled(pMin, new Vector2(pMax.X, pMin.Y + 2f),
                ImGui.ColorConvertFloat4ToU32(accent), 7f, ImDrawFlags.RoundCornersTop);

            ImFontPtr font = TextFont.IsLoaded() ? TextFont : ImGui.GetFont();
            float midSize = 12f;
            float bigSize = 22f;

            string header = matchComplete ? "MATCH OK" : "VALID MATCH";
            string timerLine = matchComplete ? "END THIS GAME" : $"{remaining}s / {durationSec}s";

            Vector2 headerSz = font.IsLoaded()
                ? font.CalcTextSizeA(midSize, float.MaxValue, 0f, header)
                : ImGui.CalcTextSize(header);
            dl.AddText(font, midSize, new Vector2(centerX - headerSz.X * 0.5f, pMin.Y + 7f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.90f, 0.91f, 0.94f, 1f)), header);

            Vector2 numSz = font.IsLoaded()
                ? font.CalcTextSizeA(bigSize, float.MaxValue, 0f, timerLine)
                : ImGui.CalcTextSize(timerLine);
            dl.AddText(font, bigSize, new Vector2(centerX - numSz.X * 0.5f, pMin.Y + 22f),
                ImGui.ColorConvertFloat4ToU32(matchComplete
                    ? new Vector4(1f, 0.45f, 0.38f, 1f)
                    : new Vector4(1f, 1f, 1f, 1f)), timerLine);

            const float barPadX = 12f;
            float barY = pMin.Y + 46f;
            float barW = panelW - barPadX * 2f;
            const float barH = 5f;
            Vector2 barMin = new Vector2(pMin.X + barPadX, barY);
            Vector2 barMax = barMin + new Vector2(barW, barH);

            dl.AddRectFilled(barMin, barMax,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.13f, 0.18f, 1f)), 3f);

            int fillW = (int)MathF.Round(barW * progress);
            if (fillW > 0)
            {
                Vector2 fillMax = barMin + new Vector2(fillW, barH);
                dl.AddRectFilled(barMin, fillMax, ImGui.ColorConvertFloat4ToU32(accent), 3f);
            }
        }

        private void UpdateEntities()
        {

            foreach (var entity in Core.Entities.Values)
            {
                if (entity.IsTeam != Bool3.False) continue;

                TreeNode entityNode = new TreeNode(entity.Name);

                entityNode.Nodes.Add(new TreeNode($"IsKnown: {entity.IsKnown}"));
                entityNode.Nodes.Add(new TreeNode($"IsTeam: {entity.IsTeam}"));
                entityNode.Nodes.Add(new TreeNode($"Head: {entity.Head}"));
                entityNode.Nodes.Add(new TreeNode($"Root: {entity.Root}"));
                entityNode.Nodes.Add(new TreeNode($"Health: {entity.Health}"));
                entityNode.Nodes.Add(new TreeNode($"IsDead: {entity.IsDead}"));
                entityNode.Nodes.Add(new TreeNode($"IsKnocked: {entity.IsKnocked}"));


            }
            Thread.Sleep(1000);
        }
        private void NoCache()
        {

            InternalMemory.Cache = new();
            Core.Entities = new();
            Thread.Sleep(1000);
        }

        private void HandleUIAnimation()
        {
            bool vkDown = (GetAsyncKeyState(Config.PanelToggleKey) & 0x8000) != 0;
            bool vkEdge = vkDown && !_vkInsertDownLast;
            _vkInsertDownLast = vkDown;

            bool insertPressed = vkEdge;
            if (_flowPhase == FlowPhase.Main && insertPressed && !_insertKeyPressedLastFrame)
            {
                _uiVisible = !_uiVisible;
                if (!_uiVisible)
                {
                    _showTabPopup = false;
                    _tabPopupAnim = 0f;
                }
            }
            _insertKeyPressedLastFrame = insertPressed;

            float targetAlpha = _uiVisible ? 1.0f : 0.0f;
            float dt = ImGui.GetIO().DeltaTime;
            float fadeRate = _uiVisible ? _uiFadeSpeed : _uiFadeOutSpeed;
            _uiAlpha += (targetAlpha - _uiAlpha) * dt * fadeRate;
            _uiAlpha = Math.Clamp(_uiAlpha, 0.0f, 1.0f);
            if (!_uiVisible && _uiAlpha < 0.02f)
                _uiAlpha = 0f;
        }

        protected override unsafe void Render()
        {
            EnsureTabIconsLoaded();
            if (Core.Handle != IntPtr.Zero)
                CreateHandle();
            HandleUIAnimation();

            if (_flowPhase == FlowPhase.Main)
            {
                FeatureKeybinds.ProcessHotkeys(_activeBindingId);
                bool fbDown = Config.FeedbackKey != System.Windows.Forms.Keys.None
                    && (GetAsyncKeyState(Config.FeedbackKey) & 0x8000) != 0;
                if (fbDown && !_fbKeyLast && !ImGui.GetIO().WantTextInput && !_fbKeyWaiting && !_panelKeyWaiting)
                    PostFeedbackAsync(_feedbackText, true);
                _fbKeyLast = fbDown;
            }

            // Auto-initialize emulator on startup (no login required)
            if (!_startupDone && _emulatorInitTask == null && DateTime.UtcNow >= _nextEmulatorRetryTime)
            {
                Config.ApplyStartupFromSingleConfigFile();
                _pendingFeatureRuntimeApply = true;
                _emulatorInitTask = BootstrapRuntime.InitEmulatorAsync();
            }
            if (!_startupDone && _emulatorInitTask != null && _emulatorInitTask.IsCompleted)
            {
                _startupDone = true;
                string? initErr = _emulatorInitTask.IsFaulted
                    ? (_emulatorInitTask.Exception?.GetBaseException().Message ?? "Init failed")
                    : _emulatorInitTask.Result;
                if (string.IsNullOrEmpty(initErr))
                {
                    _ = BootstrapRuntime.StartNINZAAsync();
                    CustomNotification.Notify("Notification", "Emulator ready!", 4.5f);
                }
                else
                {
                    CustomNotification.Notify("Error", initErr, 4f);
                    // Reset to allow retry after 4-second delay (never spam ADB!)
                    _emulatorInitTask = null;
                    _startupDone = false;
                    _nextEmulatorRetryTime = DateTime.UtcNow.AddSeconds(4);
                }
            }

            if (_uiVisible || _uiAlpha > 0.001f)
            {
                try
                {
                    RenderUI();
                }
                catch (Exception ex)
                {
                    if (Config.DebugLogging)
                    {
                        try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_uierror.txt"), DateTime.Now.ToString("HH:mm:ss") + " UI: " + ex.GetType().Name + ": " + ex.Message + "\r\n" + (ex.StackTrace ?? "") + "\r\n---\r\n"); } catch { }
                    }
                }
            }

            if (_pendingFeatureRuntimeApply)
            {
                _pendingFeatureRuntimeApply = false;
                ApplyLoadedFeatureRuntimeState();
            }

            if (_flowPhase == FlowPhase.Main)
                EnsureDragonLogoLoaded();
                CustomNotification.Render();

            if (!Core.HaveMatrix) return;



            var espClipDl = ImGui.GetBackgroundDrawList();
            espClipDl.PushClipRect(_emulatorClipMin, _emulatorClipMax, true);
            try
            {
                if (Config.FOVEnabled)
                {
                    DrawFOVCircle(Config.AimFov);
                }

                // Aim Track Line: draw from screen center to nearest enemy inside FOV
                if (Config.AimTrackLine)
                {
                    Vector2 screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                    Entity nearestEnemy = null;
                    Vector2 nearestEnemyScreenPos = Vector2.Zero;
                    float nearestDistance = float.MaxValue;

                    foreach (var entity in Core.Entities.Values)
                    {
                        if (entity.IsDead || !entity.IsKnown) continue;

                        var headPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                        if (headPos.X < 1 || headPos.Y < 1) continue;

                        float distToCrosshair = Vector2.Distance(screenCenter, headPos);
                        if (distToCrosshair < nearestDistance &&
                            (!Config.FOVEnabled || distToCrosshair <= Config.AimFov))
                        {
                            nearestDistance = distToCrosshair;
                            nearestEnemy = entity;
                            nearestEnemyScreenPos = headPos;
                        }
                    }

                    if (nearestEnemy != null)
                    {
                        uint trackLineColor = ColorToUint32(Config.AimTrackLineColor);
                        DrawGlowLine(screenCenter + _emulatorScreenOffset, nearestEnemyScreenPos + _emulatorScreenOffset, trackLineColor, 0.8f, 4.0f, 1.0f, 0.6f);
                    }
                }
            }
            finally
            {
                espClipDl.PopClipRect();
            }

            // Cache FindWindow result – only call once per second instead of every frame
            _findWindowTimer -= ImGui.GetIO().DeltaTime;
            if (_findWindowTimer <= 0f)
            {
                string windowName = "Overlay";
                hWnd = FindWindow(null!, windowName);
                HDPlayer = FindWindow("BlueStacksApp", null!);
                _findWindowTimer = 1.0f;
            }

            if (hWnd != IntPtr.Zero)
            {
                long extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                SetWindowLong(hWnd, GWL_EXSTYLE, (extendedStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
            }
            UpdateEspMatchTimerLogic();

            var espEntityClipDl = ImGui.GetBackgroundDrawList();
            espEntityClipDl.PushClipRect(_emulatorClipMin, _emulatorClipMax, true);
            try
            {
                RenderEntities();
            }
            finally
            {
                espEntityClipDl.PopClipRect();
            }

            DrawEspMatchTimerOverlay();
        }

        // Glowing line helper (for Aim Track Line)
        private void DrawGlowLine(Vector2 start, Vector2 end, uint color, float thickness, float glowRadius, float feather, float glowOpacityMultiplier)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            Vector4 colorVec = ImGui.ColorConvertU32ToFloat4(color);

            for (float i = glowRadius; i > 0; i -= feather)
            {
                float alpha = colorVec.W * (i / glowRadius) * glowOpacityMultiplier;
                alpha = Math.Clamp(alpha, 0, 1);
                uint glowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(colorVec.X, colorVec.Y, colorVec.Z, alpha));
                drawList.AddLine(start, end, glowColor, thickness + (glowRadius - i) * 0.5f);
            }

            drawList.AddLine(start, end, color, thickness);
        }

        private void DrawLine(ImDrawListPtr drawList, Vector2 startPos, Vector2 endPos, uint color)
        {
            Vector2 sCore = startPos - _emulatorScreenOffset;
            Vector2 eCore = endPos - _emulatorScreenOffset;
            float margin = 200f;
            bool startValid = sCore.X > 0 && sCore.Y > 0
                           && sCore.X < Core.Width + margin
                           && sCore.Y < Core.Height + margin;
            bool endValid   = eCore.X > 0 && eCore.Y > 0
                           && eCore.X < Core.Width + margin
                           && eCore.Y < Core.Height + margin;

            if (startValid && endValid)
            {
                drawList.AddLine(startPos, endPos, color, 1.5f);
            }
        }

        private void DrawSkeletonJoint(ImDrawListPtr drawList, Vector2 pos, float radius, uint fillColor, uint outlineColor)
        {
            if (radius <= 0.01f)
                return;

            if (pos.X < 0f && pos.Y < 0f)
                return;

            drawList.AddCircleFilled(pos, radius, fillColor);
            if (outlineColor != 0)
                drawList.AddCircle(pos, radius + 0.45f, outlineColor, 8, 1.0f);
        }

        private void DrawCurvedSkeletonSegment(
            ImDrawListPtr drawList,
            Vector2 startPos,
            Vector2 endPos,
            uint color,
            float thickness,
            float curveStrength,
            Vector2 bodyCenter,
            float forcedCurveSign = 0f)
        {
            if (thickness <= 0.01f)
                return;

            if ((startPos.X < 0f && startPos.Y < 0f) || (endPos.X < 0f && endPos.Y < 0f))
                return;

            Vector2 dir = endPos - startPos;
            float len = dir.Length();
            if (!float.IsFinite(len) || len <= 0.01f)
                return;

            if (len < 6f)
            {
                drawList.AddLine(startPos, endPos, color, thickness);
                return;
            }

            float sign = Math.Abs(forcedCurveSign) > 0.001f
                ? Math.Sign(forcedCurveSign)
                : (startPos.X >= bodyCenter.X ? 1f : -1f);

            Vector2 perp = new Vector2(-dir.Y / len, dir.X / len);
            Vector2 mid = (startPos + endPos) * 0.5f;
            Vector2 control = mid + perp * (len * curveStrength * sign);

            const int segments = 8;
            Vector2 prev = startPos;
            for (int i = 1; i <= segments; i++)
            {
                float u = i / (float)segments;
                float omu = 1f - u;
                Vector2 p =
                    omu * omu * startPos +
                    2f * omu * u * control +
                    u * u * endPos;

                drawList.AddLine(prev, p, color, thickness);
                prev = p;
            }
        }

        private void DrawSkeleton(Entity entity)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            uint lineColor = ColorToUint32(Config.ESPSkeletonColor);
            uint circleColor = ColorToUint32(Color.White);

            Matrix4x4 mat = Core.CameraMatrix;
            int w = Core.Width;
            int h = Core.Height;
            Vector2 o = _emulatorScreenOffset;

            var headScreenPos            = W2S.WorldToScreen(mat, entity.Head,            w, h) + o;
            var spineScreenPos           = W2S.WorldToScreen(mat, entity.Spine,           w, h) + o;
            var hipScreenPos             = W2S.WorldToScreen(mat, entity.Hip,             w, h) + o;
            var rightCalfScreenPos       = W2S.WorldToScreen(mat, entity.RightCalf,       w, h) + o;
            var leftCalfScreenPos        = W2S.WorldToScreen(mat, entity.LeftCalf,        w, h) + o;
            var rightFootScreenPos       = W2S.WorldToScreen(mat, entity.RightFoot,       w, h) + o;
            var leftFootScreenPos        = W2S.WorldToScreen(mat, entity.LeftFoot,        w, h) + o;
            var rightWristScreenPos      = W2S.WorldToScreen(mat, entity.RightWrist,      w, h) + o;
            var leftHandScreenPos        = W2S.WorldToScreen(mat, entity.LeftHand,        w, h) + o;
            var leftShoulderScreenPos    = W2S.WorldToScreen(mat, entity.LeftShoulder,    w, h) + o;
            var rightShoulderScreenPos   = W2S.WorldToScreen(mat, entity.RightShoulder,   w, h) + o;
            var rightWristJointScreenPos = W2S.WorldToScreen(mat, entity.RightWristJoint, w, h) + o;
            var leftWristJointScreenPos  = W2S.WorldToScreen(mat, entity.LeftWristJoint,  w, h) + o;

            // Head -> Spine -> Hip
            DrawLine(drawList, headScreenPos,          spineScreenPos,           lineColor);
            DrawLine(drawList, spineScreenPos,         hipScreenPos,             lineColor);

            // Spine -> Shoulders
            DrawLine(drawList, spineScreenPos,         leftShoulderScreenPos,    lineColor);
            DrawLine(drawList, spineScreenPos,         rightShoulderScreenPos,   lineColor);

            // Shoulders -> WristJoints
            DrawLine(drawList, leftShoulderScreenPos,  leftWristJointScreenPos,  lineColor);
            DrawLine(drawList, rightShoulderScreenPos, rightWristJointScreenPos, lineColor);

            // WristJoints -> Wrists/Hands
            DrawLine(drawList, leftWristJointScreenPos,  leftHandScreenPos,      lineColor);
            DrawLine(drawList, rightWristJointScreenPos, rightWristScreenPos,    lineColor);

            // Hip -> Calves -> Feet
            DrawLine(drawList, hipScreenPos,           rightCalfScreenPos,       lineColor);
            DrawLine(drawList, hipScreenPos,           leftCalfScreenPos,        lineColor);
            DrawLine(drawList, rightCalfScreenPos,     rightFootScreenPos,       lineColor);
            DrawLine(drawList, leftCalfScreenPos,      leftFootScreenPos,        lineColor);

            // Head Circle
            Vector2 headCore = headScreenPos - o;
            if (headCore.X > 0 && headCore.Y > 0
             && headCore.X < w && headCore.Y < h)
            {
                float distance = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (distance <= 0f) distance = 1f;
                float circleRadius = Math.Clamp(50.0f / distance, 3f, 30f);
                drawList.AddCircle(headScreenPos, circleRadius, circleColor, 30);
            }
        }

        private void DrawSkeletonEditorPreview(
            ImDrawListPtr dl,
            Vector2 headScreen,
            Vector2 bottomScreen,
            float boxMidX,
            float cornerWidth,
            float cornerHeight)
        {
            if (!Config.ESPSkeleton)
                return;

            uint lineColor = ColorToUint32(Config.ESPSkeletonColor);
            uint outlineColor = ColorToUint32(Color.Black);

            // Editor preview is drawn on top of a fixed PNG silhouette.
            // Keep it thinner/smaller so it doesn't look like a "blob".
            float thickness = Math.Clamp(cornerHeight * 0.012f, 1.8f, 4.6f);
            float jointRadius = thickness * 0.30f;

            // Approximate points around the silhouette.
            // Y anchors are relative to head->feet height (cornerHeight).
            float neckY = headScreen.Y + cornerHeight * 0.10f;
            float shoulderY = headScreen.Y + cornerHeight * 0.24f;
            float spineY = headScreen.Y + cornerHeight * 0.34f;
            float hipY = headScreen.Y + cornerHeight * 0.56f;

            float leftX = boxMidX - cornerWidth * 0.30f;
            float rightX = boxMidX + cornerWidth * 0.30f;

            Vector2 neck = new Vector2(boxMidX, neckY);
            Vector2 spine = new Vector2(boxMidX, spineY);
            Vector2 hip = new Vector2(boxMidX, hipY);

            Vector2 leftShoulder = new Vector2(leftX, shoulderY);
            Vector2 rightShoulder = new Vector2(rightX, shoulderY);

            Vector2 leftElbow = leftShoulder + new Vector2(-cornerWidth * 0.06f, cornerHeight * 0.14f);
            Vector2 rightElbow = rightShoulder + new Vector2(cornerWidth * 0.06f, cornerHeight * 0.14f);

            Vector2 leftWristJoint = leftElbow + new Vector2(-cornerWidth * 0.03f, cornerHeight * 0.13f);
            Vector2 rightWristJoint = rightElbow + new Vector2(cornerWidth * 0.03f, cornerHeight * 0.13f);

            Vector2 leftWrist = leftWristJoint + new Vector2(-cornerWidth * 0.02f, cornerHeight * 0.09f);
            Vector2 rightWrist = rightWristJoint + new Vector2(cornerWidth * 0.02f, cornerHeight * 0.09f);

            Vector2 leftHand = leftWrist + new Vector2(-cornerWidth * 0.04f, cornerHeight * 0.05f);
            Vector2 rightHand = rightWrist + new Vector2(cornerWidth * 0.04f, cornerHeight * 0.05f);

            // Knees/calf
            Vector2 leftCalf = hip + new Vector2(-cornerWidth * 0.14f, cornerHeight * 0.18f);
            Vector2 rightCalf = hip + new Vector2(cornerWidth * 0.14f, cornerHeight * 0.18f);

            // Ankles/feet
            Vector2 leftFoot = bottomScreen + new Vector2(-cornerWidth * 0.12f, 0f);
            Vector2 rightFoot = bottomScreen + new Vector2(cornerWidth * 0.12f, 0f);

            Vector2 bodyCenter = new Vector2(boxMidX, hipY);

            // Segments (curved)
            DrawCurvedSkeletonSegment(dl, headScreen, neck, lineColor, thickness, 0.03f, bodyCenter, 0f);
            DrawCurvedSkeletonSegment(dl, neck, spine, lineColor, thickness, 0.02f, bodyCenter, 0f);
            DrawCurvedSkeletonSegment(dl, spine, hip, lineColor, thickness, 0.02f, bodyCenter, 0f);

            float leftSign = -1f;
            float rightSign = 1f;
            float armCurve = 0.05f;  // reduced curve strength (prevents bowing out)
            float legCurve = 0.04f;

            DrawCurvedSkeletonSegment(dl, neck, leftShoulder, lineColor, thickness, armCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, leftShoulder, leftElbow, lineColor, thickness, armCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, leftElbow, leftWristJoint, lineColor, thickness, armCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, leftWristJoint, leftWrist, lineColor, thickness, armCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, leftWrist, leftHand, lineColor, thickness, armCurve * 0.9f, bodyCenter, leftSign);

            DrawCurvedSkeletonSegment(dl, neck, rightShoulder, lineColor, thickness, armCurve, bodyCenter, rightSign);
            DrawCurvedSkeletonSegment(dl, rightShoulder, rightElbow, lineColor, thickness, armCurve, bodyCenter, rightSign);
            DrawCurvedSkeletonSegment(dl, rightElbow, rightWristJoint, lineColor, thickness, armCurve, bodyCenter, rightSign);
            DrawCurvedSkeletonSegment(dl, rightWristJoint, rightWrist, lineColor, thickness, armCurve, bodyCenter, rightSign);
            DrawCurvedSkeletonSegment(dl, rightWrist, rightHand, lineColor, thickness, armCurve * 0.9f, bodyCenter, rightSign);

            DrawCurvedSkeletonSegment(dl, hip, leftCalf, lineColor, thickness, legCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, leftCalf, leftFoot, lineColor, thickness, legCurve, bodyCenter, leftSign);
            DrawCurvedSkeletonSegment(dl, hip, rightCalf, lineColor, thickness, legCurve, bodyCenter, rightSign);
            DrawCurvedSkeletonSegment(dl, rightCalf, rightFoot, lineColor, thickness, legCurve, bodyCenter, rightSign);

            // Joints (dots)
            DrawSkeletonJoint(dl, headScreen, Math.Max(jointRadius, thickness * 0.45f), lineColor, outlineColor);
            DrawSkeletonJoint(dl, neck, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, spine, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, hip, jointRadius, lineColor, outlineColor);

            DrawSkeletonJoint(dl, leftShoulder, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, leftElbow, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, leftWristJoint, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, leftWrist, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, leftHand, jointRadius * 0.9f, lineColor, outlineColor);

            DrawSkeletonJoint(dl, rightShoulder, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightElbow, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightWristJoint, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightWrist, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightHand, jointRadius * 0.9f, lineColor, outlineColor);

            DrawSkeletonJoint(dl, leftCalf, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightCalf, jointRadius, lineColor, outlineColor);
            DrawSkeletonJoint(dl, leftFoot, jointRadius * 0.95f, lineColor, outlineColor);
            DrawSkeletonJoint(dl, rightFoot, jointRadius * 0.95f, lineColor, outlineColor);
        }

        private static uint[] HealthColors = new uint[]
{
    ColorToUint32(Color.Green),
    ColorToUint32(Color.Yellow),
    ColorToUint32(Color.Red)
};


        public void DrawHealthText(Entity entity, int espHealthInt)
        {

            //Vector2 headScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
            //Vector2 pieDerechoScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.RightFoot, Core.Width, Core.Height);


            //string healthText = "Health: " + espHealthInt.ToString();


            //Vector2 textPosition = new Vector2(.X - 25, pieDerechoScreenPos.Y + 10);


            //var drawList = ImGui.GetBackgroundDrawList();


            //uint borderColor = ColorToUint32(Color.Black);


            //float offset = 1.0f;

            //drawList.AddText(new Vector2(textPosition.X - offset, textPosition.Y), borderColor, healthText);
            //drawList.AddText(new Vector2(textPosition.X + offset, textPosition.Y), borderColor, healthText);
            //drawList.AddText(new Vector2(textPosition.X, textPosition.Y - offset), borderColor, healthText);
            //drawList.AddText(new Vector2(textPosition.X, textPosition.Y + offset), borderColor, healthText);


            //drawList.AddText(textPosition, ColorToUint32(Color.White), healthText);
        }

        private void DrawHealthTextFixed(Entity entity, int espHealthInt)
        {
            if (entity == null)
                return;

            if (entity.IsKnocked)
                return;

            // Draw the health number near the health bar.
            int hp = Math.Max(0, espHealthInt);
            string ht = hp.ToString();

            Vector2 headScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
            Vector2 bottomScreenPos = W2S.WorldToScreen(Core.CameraMatrix, entity.Root, Core.Width, Core.Height);
            if (headScreenPos.X < 0f && headScreenPos.Y < 0f) return;
            if (bottomScreenPos.X < 0f && bottomScreenPos.Y < 0f) return;

            headScreenPos += _emulatorScreenOffset;
            bottomScreenPos += _emulatorScreenOffset;

            float cornerHeight = Math.Abs(headScreenPos.Y - bottomScreenPos.Y);
            if (cornerHeight <= 0.01f) return;
            float cornerWidth = cornerHeight * 0.65f;

            Vector2 offBox = ScaleEspOffsetPx(Config.EspBoxOffsetPx, cornerHeight);
            Vector2 offHealth = ScaleEspOffsetPx(Config.EspHealthBarOffsetPx, cornerHeight);
            float gapScaled = Math.Max(3f, EspAnchorGapPx * (cornerHeight / EspOffsetRefCornerHeight));

            GetEspBoxBounds(headScreenPos, cornerWidth, cornerHeight, offBox,
                out float boxL, out float boxT, out float boxR, out float boxB, out float boxMidX, out float boxMidY);

            // Same thickness used in the in-game health bar drawing.
            float t = 3f;
            Vector2 healthHitMin = Vector2.Zero;
            Vector2 healthHitMax = Vector2.Zero;

            switch (Config.EspHealthAnchor)
            {
                case EspHealthBoxAnchor.BoxTop:
                    {
                        float y0 = boxT - gapScaled - t;
                        Vector2 o = offHealth;
                        healthHitMin = new Vector2(boxL + o.X, y0 + o.Y);
                        healthHitMax = new Vector2(boxR + o.X, y0 + t + o.Y);
                        break;
                    }
                case EspHealthBoxAnchor.BoxBottom:
                    {
                        float y0 = boxB + gapScaled;
                        Vector2 o = offHealth;
                        healthHitMin = new Vector2(boxL + o.X, y0 + o.Y);
                        healthHitMax = new Vector2(boxR + o.X, y0 + t + o.Y);
                        break;
                    }
                case EspHealthBoxAnchor.BoxLeft:
                    {
                        Vector2 p = new Vector2(boxL - 5f - t, boxT) + offHealth;
                        healthHitMin = p;
                        healthHitMax = p + new Vector2(t, cornerHeight);
                        break;
                    }
                default:
                    {
                        Vector2 healthBarPosRight = new Vector2(boxR + 5f, boxT) + offHealth;
                        healthHitMin = healthBarPosRight;
                        healthHitMax = healthBarPosRight + new Vector2(t, cornerHeight);
                        break;
                    }
            }

            Vector2 hts = ImGui.CalcTextSize(ht);
            Vector2 center = (healthHitMin + healthHitMax) * 0.5f;

            Vector2 hpTextPos;
            switch (Config.EspHealthAnchor)
            {
                case EspHealthBoxAnchor.BoxLeft:
                    hpTextPos = new Vector2(healthHitMax.X + 6f, center.Y - hts.Y * 0.5f);
                    break;
                case EspHealthBoxAnchor.BoxTop:
                    hpTextPos = new Vector2(center.X - hts.X * 0.5f, healthHitMin.Y - hts.Y - 6f);
                    break;
                case EspHealthBoxAnchor.BoxBottom:
                    hpTextPos = new Vector2(center.X - hts.X * 0.5f, healthHitMax.Y + 6f);
                    break;
                case EspHealthBoxAnchor.BoxRight:
                default:
                    hpTextPos = new Vector2(healthHitMin.X - hts.X - 6f, center.Y - hts.Y * 0.5f);
                    break;
            }

            var fg = ImGui.GetForegroundDrawList();
            uint outlineColor = ColorToUint32(Color.Black);
            float hpFrac = hp > 1000 ? 1f : Math.Clamp(hp / 200f, 0f, 1f);
            HealthGradientColors(hpFrac, false, out uint textColor, out _);

            // Outline for readability.
            fg.AddText(hpTextPos + new Vector2(1, 1), outlineColor, ht);
            fg.AddText(hpTextPos, textColor, ht);
        }

        //public void DrawGlowingBall(Vector2 position, Color color, float radius)
        //{
        //    var drawList = ImGui.GetBackgroundDrawList();
        //    uint ballColor = ColorToUint32(color);


        //    for (int i = 0; i < 5; i++)
        //    {
        //        float glowRadius = radius + (i * 2); 
        //        float alpha = 1.0f - (i * 0.2f);    

        //        drawList.AddCircleFilled(
        //            position,
        //            glowRadius,
        //            ImGui.ColorConvertFloat4ToU32(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, alpha)),
        //            50 
        //        );
        //    }


        //    drawList.AddCircleFilled(position, radius, ballColor, 50);
        //}

        public void DrawCorneredBox(float X, float Y, float W, float H, uint color, float thickness)
        {
            var vList = ImGui.GetBackgroundDrawList();
            DrawCorneredBoxDrawList(vList, X, Y, W, H, color, thickness);
        }

        private static void DrawCorneredBoxDrawList(ImDrawListPtr dl, float X, float Y, float W, float H, uint color, float thickness)
        {
            float lineW = W / 3;
            float lineH = H / 3;
            uint outlineColor = 0xFF000000;
            float outlineThickness = thickness + 2f;
            
            // Draw outlines first
            dl.AddLine(new Vector2(X, Y - outlineThickness / 2), new Vector2(X, Y + lineH), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X - outlineThickness / 2, Y), new Vector2(X + lineW, Y), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X + W - lineW, Y), new Vector2(X + W + outlineThickness / 2, Y), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X + W, Y - outlineThickness / 2), new Vector2(X + W, Y + lineH), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X, Y + H - lineH), new Vector2(X, Y + H + outlineThickness / 2), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X - outlineThickness / 2, Y + H), new Vector2(X + lineW, Y + H), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X + W - lineW, Y + H), new Vector2(X + W + outlineThickness / 2, Y + H), outlineColor, outlineThickness);
            dl.AddLine(new Vector2(X + W, Y + H - lineH), new Vector2(X + W, Y + H + outlineThickness / 2), outlineColor, outlineThickness);
            
            // Draw main colored lines
            dl.AddLine(new Vector2(X, Y - thickness / 2), new Vector2(X, Y + lineH), color, thickness);
            dl.AddLine(new Vector2(X - thickness / 2, Y), new Vector2(X + lineW, Y), color, thickness);
            dl.AddLine(new Vector2(X + W - lineW, Y), new Vector2(X + W + thickness / 2, Y), color, thickness);
            dl.AddLine(new Vector2(X + W, Y - thickness / 2), new Vector2(X + W, Y + lineH), color, thickness);
            dl.AddLine(new Vector2(X, Y + H - lineH), new Vector2(X, Y + H + thickness / 2), color, thickness);
            dl.AddLine(new Vector2(X - thickness / 2, Y + H), new Vector2(X + lineW, Y + H), color, thickness);
            dl.AddLine(new Vector2(X + W - lineW, Y + H), new Vector2(X + W + thickness / 2, Y + H), color, thickness);
            dl.AddLine(new Vector2(X + W, Y + H - lineH), new Vector2(X + W, Y + H + thickness / 2), color, thickness);
        }

        private const float EspOffsetRefCornerHeight = 200f;

        private static Vector2 ScaleEspOffsetPx(Vector2 editorPx, float cornerHeight)
        {
            float refH = Math.Max(1f, EspOffsetRefCornerHeight);
            float s = cornerHeight / refH;
            return editorPx * s;
        }

        private const float EspAnchorGapPx = 5f;

        private static bool IsNameAnchorTopRow(EspNameBoxAnchor a) =>
            a == EspNameBoxAnchor.BoxTopCenter || a == EspNameBoxAnchor.BoxTopLeft || a == EspNameBoxAnchor.BoxTopRight;

        private static void GetEspBoxBounds(Vector2 headScreen, float cornerWidth, float cornerHeight, Vector2 offBox,
            out float bl, out float bt, out float br, out float bb, out float midX, out float midY)
        {
            bl = headScreen.X - cornerWidth * 0.5f + offBox.X;
            bt = headScreen.Y + offBox.Y;
            br = bl + cornerWidth;
            bb = bt + cornerHeight;
            midX = bl + cornerWidth * 0.5f;
            midY = bt + cornerHeight * 0.5f;
        }

        private static Vector2 GetNameBoxAnchorTopLeft(EspNameBoxAnchor anchor, float bl, float bt, float br, float bb, float midX, float midY, float gap, float w, float h)
        {
            return anchor switch
            {
                EspNameBoxAnchor.BoxTopCenter => new Vector2(midX - w * 0.5f, bt - gap - h),
                EspNameBoxAnchor.BoxBottomCenter => new Vector2(midX - w * 0.5f, bb + gap),
                EspNameBoxAnchor.BoxTopLeft => new Vector2(bl - gap - w, bt),
                EspNameBoxAnchor.BoxTopRight => new Vector2(br + gap, bt),
                EspNameBoxAnchor.BoxBottomLeft => new Vector2(bl - gap - w, bb - h),
                EspNameBoxAnchor.BoxBottomRight => new Vector2(br + gap, bb - h),
                EspNameBoxAnchor.BoxMiddleLeft => new Vector2(bl - gap - w, midY - h * 0.5f),
                EspNameBoxAnchor.BoxMiddleRight => new Vector2(br + gap, midY - h * 0.5f),
                _ => new Vector2(midX - w * 0.5f, bt - gap - h),
            };
        }

        private static readonly EspNameBoxAnchor[] EspEditorNameAnchorOrder =
        {
            EspNameBoxAnchor.BoxTopCenter,
            EspNameBoxAnchor.BoxBottomCenter,
            EspNameBoxAnchor.BoxTopLeft,
            EspNameBoxAnchor.BoxTopRight,
            EspNameBoxAnchor.BoxBottomLeft,
            EspNameBoxAnchor.BoxBottomRight,
            EspNameBoxAnchor.BoxMiddleLeft,
            EspNameBoxAnchor.BoxMiddleRight,
        };

        private static Vector2 GetEspNameAnchorAttachPoint(EspNameBoxAnchor a, float bl, float bt, float br, float bb, float midX, float midY)
        {
            return a switch
            {
                EspNameBoxAnchor.BoxTopCenter => new Vector2(midX, bt),
                EspNameBoxAnchor.BoxBottomCenter => new Vector2(midX, bb),
                EspNameBoxAnchor.BoxTopLeft => new Vector2(bl, bt),
                EspNameBoxAnchor.BoxTopRight => new Vector2(br, bt),
                EspNameBoxAnchor.BoxBottomLeft => new Vector2(bl, bb),
                EspNameBoxAnchor.BoxBottomRight => new Vector2(br, bb),
                EspNameBoxAnchor.BoxMiddleLeft => new Vector2(bl, midY),
                EspNameBoxAnchor.BoxMiddleRight => new Vector2(br, midY),
                _ => new Vector2(midX, bt),
            };
        }

        private static Vector2 GetEspDistanceAnchorAttachPoint(EspDistanceBoxAnchor d, EspNameBoxAnchor nameA,
            Vector2 npTL, float npW, float npH, float bl, float bt, float br, float bb, float midX, float midY, float gap)
        {
            if (d == EspDistanceBoxAnchor.InsideNameplate)
                return npTL + new Vector2(npW * 0.5f, npH * 0.5f);
            if (d == EspDistanceBoxAnchor.AboveNameWhenNameAtTop)
            {
                if (IsNameAnchorTopRow(nameA))
                    return new Vector2(npTL.X + npW * 0.5f, npTL.Y);
                return new Vector2(midX, bt);
            }
            EspNameBoxAnchor map = d switch
            {
                EspDistanceBoxAnchor.BoxTopCenter => EspNameBoxAnchor.BoxTopCenter,
                EspDistanceBoxAnchor.BoxBottomCenter => EspNameBoxAnchor.BoxBottomCenter,
                EspDistanceBoxAnchor.BoxTopLeft => EspNameBoxAnchor.BoxTopLeft,
                EspDistanceBoxAnchor.BoxTopRight => EspNameBoxAnchor.BoxTopRight,
                EspDistanceBoxAnchor.BoxBottomLeft => EspNameBoxAnchor.BoxBottomLeft,
                EspDistanceBoxAnchor.BoxBottomRight => EspNameBoxAnchor.BoxBottomRight,
                EspDistanceBoxAnchor.BoxMiddleLeft => EspNameBoxAnchor.BoxMiddleLeft,
                EspDistanceBoxAnchor.BoxMiddleRight => EspNameBoxAnchor.BoxMiddleRight,
                _ => EspNameBoxAnchor.BoxTopCenter,
            };
            return GetEspNameAnchorAttachPoint(map, bl, bt, br, bb, midX, midY);
        }

        private static Vector2 GetEspHealthAnchorAttachPoint(EspHealthBoxAnchor a, float bl, float bt, float br, float bb, float midX, float midY, float cornerH)
        {
            float my = bt + cornerH * 0.5f;
            return a switch
            {
                EspHealthBoxAnchor.BoxTop => new Vector2(midX, bt),
                EspHealthBoxAnchor.BoxBottom => new Vector2(midX, bb),
                EspHealthBoxAnchor.BoxLeft => new Vector2(bl, my),
                _ => new Vector2(br, my),
            };
        }

        private static void DrawEspEditorNameSnapSlots(ImDrawListPtr dl, float boxL, float boxT, float boxR, float boxB, float boxMidX, float boxMidY, float gap, float slotW, float slotH, EspNameBoxAnchor currentAnchor, EspNameBoxAnchor? highlightAnchor)
        {
            const float rounding = 2.5f;
            uint fillBase = 0x20FFFFFF;
            uint fillCurrent = 0x40B0B0FF;
            uint fillHi = 0x55C8E0FF;
            uint borderCol = 0x5AFFFFFF;
            foreach (EspNameBoxAnchor a in EspEditorNameAnchorOrder)
            {
                Vector2 tl = GetNameBoxAnchorTopLeft(a, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gap, slotW, slotH);
                Vector2 brc = tl + new Vector2(slotW, slotH);
                bool isCur = a == currentAnchor;
                bool isHi = highlightAnchor.HasValue && a == highlightAnchor.Value;
                uint fill = isHi ? fillHi : (isCur ? fillCurrent : fillBase);
                dl.AddRectFilled(tl, brc, fill, rounding, ImDrawFlags.RoundCornersAll);
                dl.AddRect(tl, brc, borderCol, rounding, ImDrawFlags.RoundCornersAll, 1f);
            }
        }

        private static EspNameBoxAnchor? EspEditorNameSlotUnderMouse(Vector2 mouse, float boxL, float boxT, float boxR, float boxB, float boxMidX, float boxMidY, float gap, float slotW, float slotH)
        {
            const float padIn = 12f;
            foreach (EspNameBoxAnchor a in EspEditorNameAnchorOrder)
            {
                Vector2 tl = GetNameBoxAnchorTopLeft(a, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gap, slotW, slotH);
                Vector2 brc = tl + new Vector2(slotW, slotH);
                if (EspEditorPointInRectPad(mouse, tl, brc, padIn))
                    return a;
            }
            return null;
        }

        private static bool TrySnapEspNameAnchorAtMouse(Vector2 mouse, float boxL, float boxT, float boxR, float boxB, float boxMidX, float boxMidY, float gap, float slotW, float slotH, out EspNameBoxAnchor chosen)
        {
            const float padIn = 12f;
            chosen = EspNameBoxAnchor.BoxTopCenter;
            foreach (EspNameBoxAnchor a in EspEditorNameAnchorOrder)
            {
                Vector2 tl = GetNameBoxAnchorTopLeft(a, boxL, boxT, boxR, boxB, boxMidX, boxMidY, gap, slotW, slotH);
                Vector2 brc = tl + new Vector2(slotW, slotH);
                if (EspEditorPointInRectPad(mouse, tl, brc, padIn))
                {
                    chosen = a;
                    return true;
                }
            }
            return false;
        }

        private static readonly EspDistanceBoxAnchor[] EspEditorDistanceAnchorOrder =
        {
            EspDistanceBoxAnchor.InsideNameplate,
            EspDistanceBoxAnchor.AboveNameWhenNameAtTop,
            EspDistanceBoxAnchor.BoxTopCenter,
            EspDistanceBoxAnchor.BoxBottomCenter,
            EspDistanceBoxAnchor.BoxTopLeft,
            EspDistanceBoxAnchor.BoxTopRight,
            EspDistanceBoxAnchor.BoxBottomLeft,
            EspDistanceBoxAnchor.BoxBottomRight,
            EspDistanceBoxAnchor.BoxMiddleLeft,
            EspDistanceBoxAnchor.BoxMiddleRight,
        };

        private static readonly EspHealthBoxAnchor[] EspEditorHealthAnchorOrder =
        {
            EspHealthBoxAnchor.BoxRight,
            EspHealthBoxAnchor.BoxLeft,
            EspHealthBoxAnchor.BoxTop,
            EspHealthBoxAnchor.BoxBottom,
        };

        private static void EspEditorGetDistanceSlotRect(EspDistanceBoxAnchor d, EspNameBoxAnchor nameA, Vector2 distSz,
            Vector2 npTL, float npW, float npH,
            float bl, float bt, float br, float bb, float midX, float midY, float gap,
            out Vector2 mn, out Vector2 mx)
        {
            if (d == EspDistanceBoxAnchor.InsideNameplate)
            {
                mn = npTL;
                mx = npTL + new Vector2(npW, npH);
                return;
            }
            Vector2 tl = GetDistanceBoxAnchorTopLeft(d, nameA, distSz, npTL, npW, npH, bl, bt, br, bb, midX, midY, gap);
            mn = tl;
            mx = tl + distSz;
        }

        private static void EspEditorGetHealthBarSlotRect(EspHealthBoxAnchor a, float bl, float bt, float br, float bb, float cornerW, float cornerH, float gap, float t,
            out Vector2 mn, out Vector2 mx)
        {
            switch (a)
            {
                case EspHealthBoxAnchor.BoxTop:
                    {
                        float y0 = bt - gap - t;
                        mn = new Vector2(bl, y0);
                        mx = new Vector2(br, y0 + t);
                        break;
                    }
                case EspHealthBoxAnchor.BoxBottom:
                    {
                        float y0 = bb + gap;
                        mn = new Vector2(bl, y0);
                        mx = new Vector2(br, y0 + t);
                        break;
                    }
                case EspHealthBoxAnchor.BoxLeft:
                    {
                        Vector2 p = new Vector2(bl - 5f - t, bt);
                        mn = p;
                        mx = p + new Vector2(t, cornerH);
                        break;
                    }
                default:
                    {
                        Vector2 p = new Vector2(br + 5f, bt);
                        mn = p;
                        mx = p + new Vector2(t, cornerH);
                        break;
                    }
            }
        }

        /// <summary>Actual drawn health bar rect (same math as editor preview draw) for hit-testing before draw.</summary>
        private static void EspEditorGetHealthBarDrawHitBounds(
            EspHealthBoxAnchor anchor,
            float boxL, float boxT, float boxR, float boxB,
            float cornerWidth, float cornerHeight, float gapScaled, float t,
            Vector2 offHealth,
            out Vector2 hitMin, out Vector2 hitMax)
        {
            switch (anchor)
            {
                case EspHealthBoxAnchor.BoxTop:
                    {
                        float y0 = boxT - gapScaled - t;
                        hitMin = new Vector2(boxL + offHealth.X, y0 + offHealth.Y);
                        hitMax = new Vector2(boxR + offHealth.X, y0 + t + offHealth.Y);
                        break;
                    }
                case EspHealthBoxAnchor.BoxBottom:
                    {
                        float y0 = boxB + gapScaled;
                        hitMin = new Vector2(boxL + offHealth.X, y0 + offHealth.Y);
                        hitMax = new Vector2(boxR + offHealth.X, y0 + t + offHealth.Y);
                        break;
                    }
                case EspHealthBoxAnchor.BoxLeft:
                    {
                        Vector2 p = new Vector2(boxL - 5f - t, boxT) + offHealth;
                        hitMin = p;
                        hitMax = p + new Vector2(t, cornerHeight);
                        break;
                    }
                default:
                    {
                        Vector2 p = new Vector2(boxR + 5f, boxT) + offHealth;
                        hitMin = p;
                        hitMax = p + new Vector2(t, cornerHeight);
                        break;
                    }
            }
        }

        private static void EspEditorGetWeaponSlotRect(EspWeaponBoxAnchor a, float bl, float bt, float br, float bb, float midX, float midY, float gap, Vector2 iconSz,
            out Vector2 mn, out Vector2 mx)
        {
            EspNameBoxAnchor map = a switch
            {
                EspWeaponBoxAnchor.BoxTopCenter => EspNameBoxAnchor.BoxTopCenter,
                EspWeaponBoxAnchor.BoxBottomCenter => EspNameBoxAnchor.BoxBottomCenter,
                EspWeaponBoxAnchor.BoxTopLeft => EspNameBoxAnchor.BoxTopLeft,
                EspWeaponBoxAnchor.BoxTopRight => EspNameBoxAnchor.BoxTopRight,
                EspWeaponBoxAnchor.BoxBottomLeft => EspNameBoxAnchor.BoxBottomLeft,
                EspWeaponBoxAnchor.BoxBottomRight => EspNameBoxAnchor.BoxBottomRight,
                EspWeaponBoxAnchor.BoxMiddleLeft => EspNameBoxAnchor.BoxMiddleLeft,
                EspWeaponBoxAnchor.BoxMiddleRight => EspNameBoxAnchor.BoxMiddleRight,
                _ => EspNameBoxAnchor.BoxTopCenter,
            };
            mn = GetNameBoxAnchorTopLeft(map, bl, bt, br, bb, midX, midY, gap, iconSz.X, iconSz.Y);
            mx = mn + iconSz;
        }

        private static void DrawEspEditorDistanceSnapSlots(ImDrawListPtr dl,
            EspNameBoxAnchor nameA, Vector2 distSz, Vector2 npTL, float npW, float npH,
            float bl, float bt, float br, float bb, float midX, float midY, float gap,
            EspDistanceBoxAnchor current, EspDistanceBoxAnchor? highlight)
        {
            const float rounding = 2f;
            uint fillBase = 0x22FFE090;
            uint fillCurrent = 0x45FFCC66;
            uint fillHi = 0x58FFEEDD;
            uint borderCol = 0x66FFCC44;
            foreach (EspDistanceBoxAnchor d in EspEditorDistanceAnchorOrder)
            {
                EspEditorGetDistanceSlotRect(d, nameA, distSz, npTL, npW, npH, bl, bt, br, bb, midX, midY, gap, out Vector2 mn, out Vector2 mx);
                bool isCur = d == current;
                bool isHi = highlight.HasValue && d == highlight.Value;
                uint fill = isHi ? fillHi : (isCur ? fillCurrent : fillBase);
                dl.AddRectFilled(mn, mx, fill, rounding, ImDrawFlags.RoundCornersAll);
                dl.AddRect(mn, mx, borderCol, rounding, ImDrawFlags.RoundCornersAll, 1f);
            }
        }

        private static void DrawEspEditorHealthSnapSlots(ImDrawListPtr dl,
            float bl, float bt, float br, float bb, float cornerW, float cornerH, float gap, float t,
            EspHealthBoxAnchor current, EspHealthBoxAnchor? highlight)
        {
            const float rounding = 2f;
            uint fillBase = 0x2290FF90;
            uint fillCurrent = 0x45AAFFAA;
            uint fillHi = 0x55CCFFCC;
            uint borderCol = 0x6644FF66;
            foreach (EspHealthBoxAnchor h in EspEditorHealthAnchorOrder)
            {
                EspEditorGetHealthBarSlotRect(h, bl, bt, br, bb, cornerW, cornerH, gap, t, out Vector2 mn, out Vector2 mx);
                bool isCur = h == current;
                bool isHi = highlight.HasValue && h == highlight.Value;
                uint fill = isHi ? fillHi : (isCur ? fillCurrent : fillBase);
                dl.AddRectFilled(mn, mx, fill, rounding, ImDrawFlags.RoundCornersAll);
                dl.AddRect(mn, mx, borderCol, rounding, ImDrawFlags.RoundCornersAll, 1f);
            }
        }

        private static void DrawEspEditorWeaponSnapSlots(ImDrawListPtr dl,
            float bl, float bt, float br, float bb, float midX, float midY, float gap, Vector2 iconSz,
            EspWeaponBoxAnchor current, EspWeaponBoxAnchor? highlight)
        {
            const float rounding = 2f;
            uint fillBase = 0x2290D0FF;
            uint fillCurrent = 0x45A0E0FF;
            uint fillHi = 0x55B8F0FF;
            uint borderCol = 0x6644AAFF;
            foreach (EspWeaponBoxAnchor w in new[]
            {
                EspWeaponBoxAnchor.BoxTopCenter,
                EspWeaponBoxAnchor.BoxBottomCenter,
                EspWeaponBoxAnchor.BoxTopLeft,
                EspWeaponBoxAnchor.BoxTopRight,
                EspWeaponBoxAnchor.BoxBottomLeft,
                EspWeaponBoxAnchor.BoxBottomRight,
                EspWeaponBoxAnchor.BoxMiddleLeft,
                EspWeaponBoxAnchor.BoxMiddleRight,
            })
            {
                EspEditorGetWeaponSlotRect(w, bl, bt, br, bb, midX, midY, gap, iconSz, out Vector2 mn, out Vector2 mx);
                bool isCur = w == current;
                bool isHi = highlight.HasValue && w == highlight.Value;
                uint fill = isHi ? fillHi : (isCur ? fillCurrent : fillBase);
                dl.AddRectFilled(mn, mx, fill, rounding, ImDrawFlags.RoundCornersAll);
                dl.AddRect(mn, mx, borderCol, rounding, ImDrawFlags.RoundCornersAll, 1f);
            }
        }

        private static EspDistanceBoxAnchor? EspEditorDistanceSlotUnderMouse(Vector2 mouse, EspNameBoxAnchor nameA, Vector2 distSz,
            Vector2 npTL, float npW, float npH, float bl, float bt, float br, float bb, float midX, float midY, float gap)
        {
            const float padIn = 10f;
            foreach (EspDistanceBoxAnchor d in EspEditorDistanceAnchorOrder)
            {
                EspEditorGetDistanceSlotRect(d, nameA, distSz, npTL, npW, npH, bl, bt, br, bb, midX, midY, gap, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                    return d;
            }
            return null;
        }

        private static EspHealthBoxAnchor? EspEditorHealthSlotUnderMouse(Vector2 mouse, float bl, float bt, float br, float bb, float cornerW, float cornerH, float gap, float t)
        {
            const float padIn = 10f;
            foreach (EspHealthBoxAnchor h in EspEditorHealthAnchorOrder)
            {
                EspEditorGetHealthBarSlotRect(h, bl, bt, br, bb, cornerW, cornerH, gap, t, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                    return h;
            }
            return null;
        }

        private static EspWeaponBoxAnchor? EspEditorWeaponSlotUnderMouse(Vector2 mouse, float bl, float bt, float br, float bb, float midX, float midY, float gap, Vector2 iconSz)
        {
            const float padIn = 10f;
            foreach (EspWeaponBoxAnchor w in new[]
            {
                EspWeaponBoxAnchor.BoxTopCenter,
                EspWeaponBoxAnchor.BoxBottomCenter,
                EspWeaponBoxAnchor.BoxTopLeft,
                EspWeaponBoxAnchor.BoxTopRight,
                EspWeaponBoxAnchor.BoxBottomLeft,
                EspWeaponBoxAnchor.BoxBottomRight,
                EspWeaponBoxAnchor.BoxMiddleLeft,
                EspWeaponBoxAnchor.BoxMiddleRight,
            })
            {
                EspEditorGetWeaponSlotRect(w, bl, bt, br, bb, midX, midY, gap, iconSz, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                    return w;
            }
            return null;
        }

        private static bool TrySnapEspDistanceAnchorAtMouse(Vector2 mouse, EspNameBoxAnchor nameA, Vector2 distSz,
            Vector2 npTL, float npW, float npH, float bl, float bt, float br, float bb, float midX, float midY, float gap,
            out EspDistanceBoxAnchor chosen)
        {
            const float padIn = 10f;
            chosen = EspDistanceBoxAnchor.InsideNameplate;
            foreach (EspDistanceBoxAnchor d in EspEditorDistanceAnchorOrder)
            {
                EspEditorGetDistanceSlotRect(d, nameA, distSz, npTL, npW, npH, bl, bt, br, bb, midX, midY, gap, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                {
                    chosen = d;
                    return true;
                }
            }
            return false;
        }

        private static bool TrySnapEspHealthAnchorAtMouse(Vector2 mouse, float bl, float bt, float br, float bb, float cornerW, float cornerH, float gap, float t,
            out EspHealthBoxAnchor chosen)
        {
            const float padIn = 10f;
            chosen = EspHealthBoxAnchor.BoxRight;
            foreach (EspHealthBoxAnchor h in EspEditorHealthAnchorOrder)
            {
                EspEditorGetHealthBarSlotRect(h, bl, bt, br, bb, cornerW, cornerH, gap, t, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                {
                    chosen = h;
                    return true;
                }
            }
            return false;
        }

        private static bool TrySnapEspWeaponAnchorAtMouse(Vector2 mouse, float bl, float bt, float br, float bb, float midX, float midY, float gap, Vector2 iconSz,
            out EspWeaponBoxAnchor chosen)
        {
            const float padIn = 10f;
            chosen = EspWeaponBoxAnchor.BoxTopCenter;
            foreach (EspWeaponBoxAnchor w in new[]
            {
                EspWeaponBoxAnchor.BoxTopCenter,
                EspWeaponBoxAnchor.BoxBottomCenter,
                EspWeaponBoxAnchor.BoxTopLeft,
                EspWeaponBoxAnchor.BoxTopRight,
                EspWeaponBoxAnchor.BoxBottomLeft,
                EspWeaponBoxAnchor.BoxBottomRight,
                EspWeaponBoxAnchor.BoxMiddleLeft,
                EspWeaponBoxAnchor.BoxMiddleRight,
            })
            {
                EspEditorGetWeaponSlotRect(w, bl, bt, br, bb, midX, midY, gap, iconSz, out Vector2 mn, out Vector2 mx);
                if (EspEditorPointInRectPad(mouse, mn, mx, padIn))
                {
                    chosen = w;
                    return true;
                }
            }
            return false;
        }

        private static Vector2 GetDistanceBoxAnchorTopLeft(EspDistanceBoxAnchor d, EspNameBoxAnchor nameA, Vector2 distSz,
            Vector2 namePlateTL, float npW, float npH, float bl, float bt, float br, float bb, float midX, float midY, float gap)
        {
            if (d == EspDistanceBoxAnchor.InsideNameplate)
                return Vector2.Zero;

            if (d == EspDistanceBoxAnchor.AboveNameWhenNameAtTop)
            {
                if (IsNameAnchorTopRow(nameA))
                    return new Vector2(namePlateTL.X + npW * 0.5f - distSz.X * 0.5f, namePlateTL.Y - gap - distSz.Y);
                return new Vector2(midX - distSz.X * 0.5f, bt - gap - distSz.Y);
            }

            EspNameBoxAnchor map = d switch
            {
                EspDistanceBoxAnchor.BoxTopCenter => EspNameBoxAnchor.BoxTopCenter,
                EspDistanceBoxAnchor.BoxBottomCenter => EspNameBoxAnchor.BoxBottomCenter,
                EspDistanceBoxAnchor.BoxTopLeft => EspNameBoxAnchor.BoxTopLeft,
                EspDistanceBoxAnchor.BoxTopRight => EspNameBoxAnchor.BoxTopRight,
                EspDistanceBoxAnchor.BoxBottomLeft => EspNameBoxAnchor.BoxBottomLeft,
                EspDistanceBoxAnchor.BoxBottomRight => EspNameBoxAnchor.BoxBottomRight,
                EspDistanceBoxAnchor.BoxMiddleLeft => EspNameBoxAnchor.BoxMiddleLeft,
                EspDistanceBoxAnchor.BoxMiddleRight => EspNameBoxAnchor.BoxMiddleRight,
                _ => EspNameBoxAnchor.BoxTopCenter,
            };
            return GetNameBoxAnchorTopLeft(map, bl, bt, br, bb, midX, midY, gap, distSz.X, distSz.Y);
        }

        private static EspNameBoxAnchor MapHealthAnchorToNameAnchor(EspHealthBoxAnchor a)
        {
            return a switch
            {
                EspHealthBoxAnchor.BoxTop => EspNameBoxAnchor.BoxTopCenter,
                EspHealthBoxAnchor.BoxBottom => EspNameBoxAnchor.BoxBottomCenter,
                EspHealthBoxAnchor.BoxLeft => EspNameBoxAnchor.BoxMiddleLeft,
                _ => EspNameBoxAnchor.BoxMiddleRight,
            };
        }

        private static EspNameBoxAnchor MapWeaponAnchorToNameAnchor(EspWeaponBoxAnchor a)
        {
            return a switch
            {
                EspWeaponBoxAnchor.BoxTopCenter => EspNameBoxAnchor.BoxTopCenter,
                EspWeaponBoxAnchor.BoxBottomCenter => EspNameBoxAnchor.BoxBottomCenter,
                EspWeaponBoxAnchor.BoxTopLeft => EspNameBoxAnchor.BoxTopLeft,
                EspWeaponBoxAnchor.BoxTopRight => EspNameBoxAnchor.BoxTopRight,
                EspWeaponBoxAnchor.BoxBottomLeft => EspNameBoxAnchor.BoxBottomLeft,
                EspWeaponBoxAnchor.BoxBottomRight => EspNameBoxAnchor.BoxBottomRight,
                EspWeaponBoxAnchor.BoxMiddleLeft => EspNameBoxAnchor.BoxMiddleLeft,
                EspWeaponBoxAnchor.BoxMiddleRight => EspNameBoxAnchor.BoxMiddleRight,
                _ => EspNameBoxAnchor.BoxTopCenter,
            };
        }

        private static bool IsOccupiedByOthers(EspNameBoxAnchor candidate, bool excludeName, bool excludeHealth, bool excludeWeapon)
        {
            if (!excludeName && Config.ESPName && Config.EspNameAnchor == candidate)
                return true;

            if (!excludeHealth && Config.ESPHealth && MapHealthAnchorToNameAnchor(Config.EspHealthAnchor) == candidate)
                return true;

            if (!excludeWeapon && Config.espweapon && MapWeaponAnchorToNameAnchor(Config.EspWeaponAnchor) == candidate)
                return true;

            return false;
        }

        private static Vector4 ColorToVec4(Color c)
        {
            return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        }

        private static Color Vec4ToColor(Vector4 v)
        {
            v.X = Math.Clamp(v.X, 0f, 1f);
            v.Y = Math.Clamp(v.Y, 0f, 1f);
            v.Z = Math.Clamp(v.Z, 0f, 1f);
            v.W = Math.Clamp(v.W, 0f, 1f);
            return Color.FromArgb((int)(v.W * 255f), (int)(v.X * 255f), (int)(v.Y * 255f), (int)(v.Z * 255f));
        }

        private static string GetEspEditorColorTargetLabel(EspEditorColorTarget t)
        {
            return t switch
            {
                EspEditorColorTarget.Line => "Line",
                EspEditorColorTarget.Box => "Box",
                EspEditorColorTarget.Name => "Name",
                EspEditorColorTarget.Health => "Health",
                EspEditorColorTarget.Weapon => "Weapon",
                EspEditorColorTarget.Hacker => "Hacker",
                _ => "Esp",
            };
        }

        private bool TryGetEspEditorTargetColor(EspEditorColorTarget t, out Vector4 c)
        {
            c = new Vector4(1f, 1f, 1f, 1f);
            switch (t)
            {
                case EspEditorColorTarget.Line: c = ColorToVec4(Config.ESPLineColor); return true;
                case EspEditorColorTarget.Box: c = ColorToVec4(Config.ESPBoxColor); return true;
                case EspEditorColorTarget.Name: c = ColorToVec4(Config.ESPNameColor); return true;
                case EspEditorColorTarget.Health: c = ColorToVec4(Config.ESPHealthColor); return true;
                case EspEditorColorTarget.Weapon: c = Config.ICONCOLOR; return true;
                case EspEditorColorTarget.Hacker: c = ColorToVec4(Config.HackerTagColor); return true;
                default: return false;
            }
        }

        private void SetEspEditorTargetColor(EspEditorColorTarget t, Vector4 c)
        {
            switch (t)
            {
                case EspEditorColorTarget.Line: Config.ESPLineColor = Vec4ToColor(c); break;
                case EspEditorColorTarget.Box: Config.ESPBoxColor = Vec4ToColor(c); break;
                case EspEditorColorTarget.Name: Config.ESPNameColor = Vec4ToColor(c); break;
                case EspEditorColorTarget.Health: Config.ESPHealthColor = Vec4ToColor(c); break;
                case EspEditorColorTarget.Weapon: Config.ICONCOLOR = c; break;
                case EspEditorColorTarget.Hacker: Config.HackerTagColor = Vec4ToColor(c); break;
            }
        }

        private const float EspNameplateBarH = 20f;
        private const float EspNameplateHealthH = 3f;
        private const float EspNameplateAboveHeadGap = 5f;
        private const float EspNameplateIdW = 18f;
        private const float EspNameplateMinWidth = 128f;

        /// <summary>Keep full player names for ESP; only trim nulls / extreme lengths (memory-safe). Surrogate-safe: never split a pair. Missing names show as ???.</summary>
        private static string SanitizeEspDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            string t = name.Trim('\0', '\u200B', '\t');
            if (t.Length > 512)
            {
                int cut = 509;
                if (cut > 0 && char.IsHighSurrogate(t[cut]) && cut + 1 < t.Length && char.IsLowSurrogate(t[cut + 1]))
                    cut++;
                else if (cut > 0 && char.IsLowSurrogate(t[cut]) && char.IsHighSurrogate(t[cut - 1]))
                    cut--;
                t = t.Substring(0, Math.Clamp(cut, 0, t.Length)) + "...";
            }
            return t;
        }

        private static Vector2 EspCalcTextSizeName(string text)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            // Unicode-aware measurement (shaped/bidi/fallback) so CJK/RTL boxes align.
            try { return EspNameRenderer.Measure(text); } catch { }
            if (TextFont.IsLoaded())
            {
                ImGui.PushFont(TextFont);
                Vector2 s = ImGui.CalcTextSize(text);
                ImGui.PopFont();
                return s;
            }
            return ImGui.CalcTextSize(text);
        }

        private static void ComputeEspNameplateSize(string nameDisplay, string distStr, out float width, out float totalHeight)
        {
            const float padMain = 5f;
            Vector2 n = EspCalcTextSizeName(nameDisplay);
            Vector2 d = ImGui.CalcTextSize(distStr);
            width = EspNameplateIdW + padMain + n.X + 8f + d.X + padMain;
            width = Math.Max(EspNameplateMinWidth, width);
            totalHeight = EspNameplateBarH;
        }

        private static void ComputeEspNameplateSizeNameOnly(string nameDisplay, out float width, out float totalHeight)
        {
            const float padMain = 5f;
            Vector2 n = EspCalcTextSizeName(nameDisplay);
            width = EspNameplateIdW + padMain + n.X + padMain;
            width = Math.Max(96f, width);
            totalHeight = EspNameplateBarH;
        }

        private static void DrawEspInfoNameplate(
            ImDrawListPtr dl,
            Vector2 topLeft,
            float nameplateW,
            string nameDisplay,
            string distStr,
            int teamDigit,
            float healthFraction,
            bool knocked,
            bool drawDistanceInBar = true)
        {
            bool popFont = TextFont.IsLoaded();
            if (popFont) ImGui.PushFont(TextFont);

            const float padMain = 6f;
            float barH = EspNameplateBarH;
            float totalH = barH;
            teamDigit = Math.Abs(teamDigit % 10);

            Vector2 tl = topLeft;
            Vector2 br = tl + new Vector2(nameplateW, totalH);

            dl.AddRectFilled(tl, tl + new Vector2(EspNameplateIdW, barH), ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            dl.AddRect(tl, tl + new Vector2(EspNameplateIdW, barH), ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.55f)), 0f, ImDrawFlags.None, 1f);

            string idStr = teamDigit.ToString();
            Vector2 idSz = ImGui.CalcTextSize(idStr);
            dl.AddText(tl + new Vector2((EspNameplateIdW - idSz.X) * 0.5f, (barH - idSz.Y) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1f)), idStr);

            Vector2 mainMin = tl + new Vector2(EspNameplateIdW, 0f);
            Vector2 mainMax = tl + new Vector2(nameplateW, barH);
            uint cTL = ImGui.ColorConvertFloat4ToU32(new Vector4(0.07f, 0.32f, 0.38f, 0.98f));
            uint cTR = ImGui.ColorConvertFloat4ToU32(new Vector4(0.14f, 0.52f, 0.56f, 0.98f));
            uint cBL = ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.26f, 0.32f, 0.98f));
            uint cBR = ImGui.ColorConvertFloat4ToU32(new Vector4(0.11f, 0.42f, 0.48f, 0.98f));
            dl.AddRectFilledMultiColor(mainMin, mainMax, cTL, cTR, cBR, cBL);

            Vector2 nameSz = EspCalcTextSizeName(nameDisplay);
            float nameX = mainMin.X + padMain;
            float textY = tl.Y + (barH - nameSz.Y) * 0.5f;
            EspNameRenderer.Draw(dl, new Vector2(nameX, textY), nameDisplay, ColorToUint32(Config.ESPNameColor));

            if (drawDistanceInBar)
            {
                Vector2 distSz = ImGui.CalcTextSize(distStr);
                float distX = mainMax.X - padMain - distSz.X;
                dl.AddText(new Vector2(distX, textY), ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.93f, 0.2f, 1f)), distStr);
            }

            uint borderBlack = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.92f));
            dl.AddRect(tl, br, borderBlack, 0f, ImDrawFlags.None, 1f);

            if (popFont) ImGui.PopFont();
        }

        // Health gradient: full = green, mid = orange, low = red. Shifts live as HP drops.
        private static void HealthGradientColors(float fraction, bool knocked, out uint main, out uint dark)
        {
            if (knocked)
            {
                main = ColorToUint32(Color.Red);
                dark = ColorToUint32(Color.DarkRed);
                return;
            }
            float f = Math.Clamp(fraction, 0f, 1f);
            float r, g;
            if (f >= 0.5f)
            {
                float k = (f - 0.5f) * 2f; // 0..1 orange->green
                r = 255f * (1f - k);
                g = 165f + 90f * k;
            }
            else
            {
                float k = f * 2f; // 0..1 red->orange
                r = 255f;
                g = 165f * k;
            }
            main = ColorToUint32(Color.FromArgb(255, (int)r, (int)g, 0));
            dark = ColorToUint32(Color.FromArgb(255, (int)(r * 0.55f), (int)(g * 0.55f), 0));
        }

        static uint ColorToUint32(Color color)
        {
            // Correct RGBA order: Red, Green, Blue, Alpha
            return ImGui.ColorConvertFloat4ToU32(new Vector4(
                color.R / 255.0f,
                color.G / 255.0f,
                color.B / 255.0f,
                color.A / 255.0f));
        }

        // HSV -> Color helper (for ESP RGB)
        private static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = System.Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (float)Math.Floor(hue / 60);
            value *= 255;
            int v = System.Convert.ToInt32(value);
            int p = System.Convert.ToInt32(value * (1 - saturation));
            int q = System.Convert.ToInt32(value * (1 - f * saturation));
            int t = System.Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }

        private int lastWidth = 0, lastHeight = 0;
        // Offset between emulator render area (Core coords origin) and overlay window coords origin.
        // Used so ESP stays aligned even if overlay window is expanded beyond the emulator.
        private Vector2 _emulatorScreenOffset = Vector2.Zero;
        private Vector2 _emulatorClipMin = Vector2.Zero;
        private Vector2 _emulatorClipMax = Vector2.Zero;
        void CreateHandle()
        {
            if (Core.Handle == IntPtr.Zero)
                return;
            if (Config.StreamMode)
            {
                SetWindowDisplayAffinity(hWnd, WDA_EXCLUDEFROMCAPTURE);
            }
            else
            {
                SetWindowDisplayAffinity(hWnd, WDA_NONE);
            }
            RECT emulatorRect;
            GetWindowRect(Core.Handle, out emulatorRect);

            int emulatorX = emulatorRect.Left;
            int emulatorY = emulatorRect.Top;
            int emulatorWidth = emulatorRect.Right - emulatorRect.Left;
            int emulatorHeight = emulatorRect.Bottom - emulatorRect.Top;

            // Expand overlay to FULL virtual desktop so UI can be dragged out from every side.
            // ESP projection stays in emulator-size; drawing positions are shifted by _emulatorScreenOffset.
            int virtualLeft = int.MaxValue;
            int virtualTop = int.MaxValue;
            int virtualRight = int.MinValue;
            int virtualBottom = int.MinValue;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                virtualLeft = Math.Min(virtualLeft, screen.Bounds.Left);
                virtualTop = Math.Min(virtualTop, screen.Bounds.Top);
                virtualRight = Math.Max(virtualRight, screen.Bounds.Right);
                virtualBottom = Math.Max(virtualBottom, screen.Bounds.Bottom);
            }

            int overlayWidth = virtualRight - virtualLeft;
            int overlayHeight = virtualBottom - virtualTop;

            if (overlayWidth != lastWidth || overlayHeight != lastHeight)
            {
                lastWidth = overlayWidth;
                lastHeight = overlayHeight;
                ImGui.SetWindowSize(new Vector2(overlayWidth, overlayHeight));
            }

            Size = new Size(overlayWidth, overlayHeight);
            Position = new Point(virtualLeft, virtualTop);

            // Core coords origin is emulator top-left, but overlay coords origin is virtual top-left.
            _emulatorScreenOffset = new Vector2(emulatorX - virtualLeft, emulatorY - virtualTop);
            _emulatorClipMin = _emulatorScreenOffset;
            _emulatorClipMax = _emulatorScreenOffset + new Vector2(emulatorWidth, emulatorHeight);

            Core.Width = emulatorWidth;
            Core.Height = emulatorHeight;
        }

        public void DrawFOVCircle(float radius)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var center = new Vector2(Core.Width / 2f, Core.Height / 2f) + _emulatorScreenOffset;
            uint color = ColorToUint32(Config.FOVColor);

            drawList.AddCircle(center, radius, color, 0, 1f);
        }


        Vector4 darkBg = new Vector4(18 / 255f, 18 / 255f, 22 / 255f, 1.0f);

        // Premium accent — red family (matches theme default)
        Vector4 accent1 = new Vector4(1f, 0.38f, 0.42f, 1.0f);
        Vector4 accent2 = new Vector4(0.85f, 0.2f, 0.24f, 1.0f);
        Vector4 accent3 = new Vector4(0.38f, 0.1f, 0.12f, 1.0f);

        private void ApplyCustomStyle()
        {
            var style = ImGui.GetStyle();
            var colors = style.Colors;

            Vector4 darkBg = new Vector4(18 / 255f, 18 / 255f, 22 / 255f, 1.0f);
            Vector4 accent1 = new Vector4(1f, 0.38f, 0.42f, 1.0f);
            Vector4 accent2 = new Vector4(0.85f, 0.2f, 0.24f, 1.0f);
            Vector4 accent3 = new Vector4(0.38f, 0.1f, 0.12f, 1.0f);



            colors[(int)ImGuiCol.WindowBg] = darkBg;
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.12f, 0.12f, 0.15f, 0.95f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.25f, 0.25f, 0.3f, 0.5f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0, 0, 0, 0.5f);

            colors[(int)ImGuiCol.Text] = new Vector4(0.98f, 0.98f, 0.98f, 1.0f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5f, 0.5f, 0.55f, 1.0f);

            colors[(int)ImGuiCol.Button] = new Vector4(0.22f, 0.22f, 0.26f, 0.8f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.28f, 0.32f, 1f);
            colors[(int)ImGuiCol.ButtonActive] = accent2;

            Vector4 sliderNeutral = new Vector4(0.52f, 0.54f, 0.58f, 1f);
            colors[(int)ImGuiCol.SliderGrab] = sliderNeutral;
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.62f, 0.64f, 0.68f, 1f);

            colors[(int)ImGuiCol.CheckMark] = accent1;
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.15f, 0.18f, 0.6f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.2f, 0.2f, 0.24f, 0.7f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.25f, 0.25f, 0.3f, 0.8f);

            colors[(int)ImGuiCol.Header] = accent3;
            colors[(int)ImGuiCol.HeaderHovered] = accent2;
            colors[(int)ImGuiCol.HeaderActive] = accent1;

            colors[(int)ImGuiCol.Separator] = new Vector4(42f / 255f, 42f / 255f, 42f / 255f, 0.9f);

            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.12f, 0.12f, 0.15f, 0.98f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0f, 0f, 0f, 0f);
            style.ScrollbarSize = 0f;

            // Crisp radii — compact outer/inner hierarchy
            style.WindowRounding = 4.0f;
            style.FrameRounding = 2.0f;
            style.GrabRounding = 2.0f;
            style.ScrollbarRounding = 2.0f;
            style.TabRounding = 2.0f;
            style.ChildRounding = 3.0f;
            style.PopupRounding = 3.0f;

            // Compact spacing (reduce gaps between functions)
            style.WindowPadding = new Vector2(8, 8);
            style.FramePadding = new Vector2(6, 4);
            style.ItemSpacing = new Vector2(2, 1);
            style.ItemInnerSpacing = new Vector2(3, 1);
            style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        }
        private Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return new Vector4(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            );
        }

        private void DrawBottomCenterTextSafe()
        {
            if (_uiAlpha <= 0.01f)
                return;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);

            ImGui.Begin("##bottom_text_overlay_cs",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            var draw = ImGui.GetWindowDrawList();

            string text = BrandTitle;

            if (LebelFont.IsLoaded())
                ImGui.PushFont(LebelFont);

            Vector2 textSize = ImGui.CalcTextSize(text);

            Vector2 pos = new Vector2(
                (ImGui.GetIO().DisplaySize.X - textSize.X) * 0.5f,
                ImGui.GetIO().DisplaySize.Y - textSize.Y - 80f
            );
            float t = (float)ImGui.GetTime() * 0.8f;

            Vector4 rainbow = new Vector4(
                0.5f + 0.5f * MathF.Sin(t),
                0.5f + 0.5f * MathF.Sin(t + 2.094f),
                0.5f + 0.5f * MathF.Sin(t + 4.188f),
                1f * _uiAlpha
            );

            // ---- SOFT GLOW (slightly reduced) ----
            for (int i = 2; i >= 1; i--)
            {
                float spread = i * 1.8f;
                float alpha = (0.12f * _uiAlpha) / i;

                draw.AddText(
                    pos + new Vector2(-spread, 0),
                    ImGui.GetColorU32(new Vector4(rainbow.X, rainbow.Y, rainbow.Z, alpha)),
                    text
                );
            }

            // ---- SHARP BLACK OUTLINE (8 directions) ----
            Vector4 outlineCol = new Vector4(0f, 0f, 0f, 0.85f * _uiAlpha);

            Vector2[] offsets = new Vector2[]
            {
        new Vector2(-1, 0),
        new Vector2(1, 0),
        new Vector2(0, -1),
        new Vector2(0, 1),
        new Vector2(-1, -1),
        new Vector2(1, -1),
        new Vector2(-1, 1),
        new Vector2(1, 1),
            };

            foreach (var offset in offsets)
            {
                draw.AddText(
                    pos + offset,
                    ImGui.GetColorU32(outlineCol),
                    text
                );
            }

            // ---- MAIN TEXT ----
            draw.AddText(
                pos,
                ImGui.GetColorU32(new Vector4(rainbow.X, rainbow.Y, rainbow.Z, _uiAlpha)),
                text
            );

            if (LebelFont.IsLoaded())
                ImGui.PopFont();

            ImGui.End();
        }

        private void ApplyThemeOverlay()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 14f;
            style.FrameRounding = 3f;
            style.ChildRounding = 6f;
            style.PopupRounding = 2f;
            style.ScrollbarRounding = 2f;
            style.GrabRounding = 2f;
            style.TabRounding = 2f;
            style.WindowBorderSize = 0f;
            style.ChildBorderSize = 0f;
            style.PopupBorderSize = 0f;
            var c = style.Colors;
            c[(int)ImGuiCol.WindowBg] = new Vector4(PanelBg.X, PanelBg.Y, PanelBg.Z, _uiAlpha);
            Vector4 panelA = new Vector4(PanelBg.X, PanelBg.Y, PanelBg.Z, _uiAlpha);
            c[(int)ImGuiCol.FrameBg] = panelA;
            c[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.14f, 0.14f, 0.16f, _uiAlpha);
            c[(int)ImGuiCol.FrameBgActive] = new Vector4(0.16f, 0.16f, 0.18f, _uiAlpha);
            c[(int)ImGuiCol.Button] = panelA;
            c[(int)ImGuiCol.ButtonHovered] = new Vector4(0.16f, 0.16f, 0.17f, _uiAlpha);
            c[(int)ImGuiCol.ButtonActive] = new Vector4(0.18f, 0.18f, 0.2f, _uiAlpha);
            c[(int)ImGuiCol.PopupBg] = new Vector4(0.1f, 0.1f, 0.12f, 0.98f * _uiAlpha);
            c[(int)ImGuiCol.Header] = new Vector4(0.12f, 0.12f, 0.14f, _uiAlpha);
            c[(int)ImGuiCol.HeaderHovered] = new Vector4(0.16f, 0.16f, 0.18f, _uiAlpha);
            c[(int)ImGuiCol.HeaderActive] = new Vector4(0.18f, 0.18f, 0.2f, _uiAlpha);
            c[(int)ImGuiCol.Text] = new Vector4(1f, 1f, 1f, _uiAlpha);
            c[(int)ImGuiCol.Separator] = new Vector4(42f / 255f, 42f / 255f, 42f / 255f, 0.92f * _uiAlpha);
            c[(int)ImGuiCol.Border] = new Vector4(0.18f, 0.18f, 0.2f, 0f);
            c[(int)ImGuiCol.ScrollbarBg] = new Vector4(0f, 0f, 0f, 0f);
            c[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0f, 0f, 0f, 0f);
            c[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0f, 0f, 0f, 0f);
            c[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0f, 0f, 0f, 0f);
            style.ScrollbarSize = 0f;

            style.ItemSpacing = new Vector2(4f, 3f);
        }

        private void DrawWatermark()
        {
            if (_uiAlpha <= 0.001f) return;

            Vector2 viewport = ImGui.GetMainViewport().Size;
            ImGui.SetNextWindowPos(new Vector2(viewport.X * 0.5f, 14f), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowBgAlpha(0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

            ImGui.Begin("##watermark_bg",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoMove);

            var draw = ImGui.GetWindowDrawList();
            Vector2 p = ImGui.GetWindowPos();

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);

            string brand = "RIGLO CHEAT";
            string sep = " | ";
            string link = "discord.gg/FXNINZA";
            Vector2 brandSz = ImGui.CalcTextSize(brand);
            Vector2 sepSz = ImGui.CalcTextSize(sep);
            Vector2 linkSz = ImGui.CalcTextSize(link);

            draw.AddText(p + new Vector2(1f, 1f), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.85f * _uiAlpha)), brand);
            draw.AddText(p, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.96f * _uiAlpha)), brand);

            Vector2 sepPos = p + new Vector2(brandSz.X, 0f);
            draw.AddText(sepPos, ImGui.GetColorU32(new Vector4(0.55f, 0.55f, 0.58f, 0.85f * _uiAlpha)), sep);

            Vector2 linkPos = sepPos + new Vector2(sepSz.X, 0f);
            draw.AddText(linkPos, ImGui.GetColorU32(new Vector4(0.55f, 0.55f, 0.58f, 0.85f * _uiAlpha)), link);

            if (TextFont.IsLoaded()) ImGui.PopFont();

            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        private static void ApplyTitleBarDrag()
        {
            if (!ImGui.IsItemActive() || !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                return;

            Vector2 delta = ImGui.GetIO().MouseDelta;
            if (delta.X == 0f && delta.Y == 0f)
                return;

            ImGui.SetWindowPos(ImGui.GetWindowPos() + delta, ImGuiCond.Always);
        }

        /// <summary>Re-submit title hitbox last so drag wins over overlapping widgets in the same window.</summary>
        private void SubmitTitleBarDragHitbox(Vector2 wsize)
        {
            float chromeH = UiMetrics.ChromeH;
            float innerW = wsize.X - UiMetrics.OuterPad * 2f;
            const float closeBtnSize = 20f;
            const float closeReserve = closeBtnSize + 12f;
            float dragW = MathF.Max(40f, innerW - closeReserve);

            ImGui.SetCursorPos(new Vector2(UiMetrics.OuterPad, UiMetrics.OuterPad));
            ImGui.InvisibleButton("##drag_region_top", new Vector2(dragW, chromeH));
            ApplyTitleBarDrag();
        }

        private void DrawWindowChrome(Vector2 wpos, Vector2 wsize)
        {
            var draw = ImGui.GetWindowDrawList();
            float chromeH = UiMetrics.ChromeH;
            float innerW = wsize.X - UiMetrics.OuterPad * 2f;
            Vector2 barMin = wpos + new Vector2(UiMetrics.OuterPad, UiMetrics.OuterPad);
            Vector2 barMax = barMin + new Vector2(innerW, chromeH);
            const float closeBtnSize = 20f;
            float closeReserve = closeBtnSize + 12f;

            draw.AddRectFilled(
                barMin, barMax,
                ImGui.GetColorU32(new Vector4(10f / 255f, 11f / 255f, 15f / 255f, _uiAlpha)),
                6f,
                ImDrawFlags.RoundCornersTop);

            draw.AddLine(
                barMin + new Vector2(0f, chromeH),
                barMax,
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.90f * _uiAlpha)),
                1f);

            bool hasTitleFont = LebelFont.IsLoaded();
            if (hasTitleFont) ImGui.PushFont(LebelFont);

            Vector2 whiteSz = ImGui.CalcTextSize(BrandNameWhite);
            Vector2 purpleSz = ImGui.CalcTextSize(BrandNamePurple);
            const float brandGap = 5f;
            float brandBlockH = MathF.Max(whiteSz.Y, purpleSz.Y);
            float textY = barMin.Y + (chromeH - brandBlockH) * 0.5f;
            float textX = barMin.X + 14f;

            draw.AddText(
                new Vector2(textX, textY),
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.98f * _uiAlpha)),
                BrandNameWhite);

            draw.AddText(
                new Vector2(textX + whiteSz.X + brandGap, textY + (whiteSz.Y - purpleSz.Y) * 0.5f),
                ImGui.GetColorU32(new Vector4(BrandAccentBlue.X, BrandAccentBlue.Y, BrandAccentBlue.Z, _uiAlpha)),
                BrandNamePurple);

            if (hasTitleFont) ImGui.PopFont();

            float dragLocalY = UiMetrics.OuterPad;
            float dragLocalX = UiMetrics.OuterPad;
            float dragW = MathF.Max(40f, innerW - closeReserve);

            ImGui.SetCursorPos(new Vector2(dragLocalX, dragLocalY));
            ImGui.InvisibleButton("##drag_region", new Vector2(dragW, chromeH));
            ApplyTitleBarDrag();

            float closeLocalX = wsize.X - UiMetrics.OuterPad - closeBtnSize - 6f;
            float closeLocalY = dragLocalY + (chromeH - closeBtnSize) * 0.5f;
            Vector2 closeHitPos = wpos + new Vector2(closeLocalX, closeLocalY);

            ImGui.SetCursorPos(new Vector2(closeLocalX, closeLocalY));
            ImGui.InvisibleButton("##close_btn", new Vector2(closeBtnSize, closeBtnSize));
            bool closeHovered = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked())
                _uiVisible = false;

            Vector2 closeCenter = closeHitPos + new Vector2(closeBtnSize * 0.5f, closeBtnSize * 0.5f);
            const float xArm = 5f;
            const float xThick = 1.35f;
            Vector2 x0 = closeCenter + new Vector2(-xArm, -xArm);
            Vector2 x1 = closeCenter + new Vector2(xArm, xArm);
            Vector2 x2 = closeCenter + new Vector2(xArm, -xArm);
            Vector2 x3 = closeCenter + new Vector2(-xArm, xArm);

            Vector4 xColor = closeHovered
                ? new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)
                : new Vector4(1f, 1f, 1f, 0.50f * _uiAlpha);
            uint xCol = ImGui.GetColorU32(xColor);
            draw.AddLine(x0, x1, xCol, xThick);
            draw.AddLine(x2, x3, xCol, xThick);
        }

        private void DrawSidebarPanel()
        {
            Vector2 sidebarPos = ImGui.GetWindowPos();
            Vector2 sidebarSize = ImGui.GetWindowSize();
            var draw = ImGui.GetWindowDrawList();

            draw.AddRectFilled(
                sidebarPos,
                sidebarPos + sidebarSize,
                ImGui.GetColorU32(new Vector4(8f / 255f, 9f / 255f, 13f / 255f, _uiAlpha)),
                6f,
                ImDrawFlags.RoundCornersLeft);

            draw.AddLine(
                sidebarPos + new Vector2(sidebarSize.X, 0f),
                sidebarPos + new Vector2(sidebarSize.X, sidebarSize.Y),
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.90f * _uiAlpha)),
                1f);

            EnsureDragonLogoLoaded();
            IntPtr logoPtr = CustomNotification.DragonLogoPtr;
            float logoBox = UiMetrics.SidebarLogo;
            float logoTop = UiMetrics.SidebarLogoTop;
            float logoX = (sidebarSize.X - logoBox) * 0.5f;

            ImGui.SetCursorPos(new Vector2(logoX, logoTop));
            if (logoPtr != IntPtr.Zero)
            {
                Vector2 logoScreen = ImGui.GetCursorScreenPos();
                draw.AddImage(logoPtr, logoScreen, logoScreen + new Vector2(logoBox, logoBox),
                    Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, _uiAlpha)));
                ImGui.Dummy(new Vector2(logoBox, logoBox));
            }
            else
            {
                if (LebelFont.IsLoaded()) ImGui.PushFont(LebelFont);
                string logoText = "KC";
                Vector2 logoTextSz = ImGui.CalcTextSize(logoText);
                ImGui.SetCursorPos(new Vector2((sidebarSize.X - logoTextSz.X) * 0.5f, logoTop + 8f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, _uiAlpha));
                ImGui.TextUnformatted(logoText);
                ImGui.PopStyleColor();
                if (LebelFont.IsLoaded()) ImGui.PopFont();
                ImGui.Dummy(new Vector2(0f, logoBox - logoTextSz.Y));
            }

            ImGui.Dummy(new Vector2(0f, UiMetrics.SidebarLogoBottom));
            DrawTabs();
        }

        private static void DrawHeader()
        {
            float headerHeight = 34f;
            float extendDown = 0f;

            ImGui.BeginChild("##header", new Vector2(0, headerHeight + extendDown));
            var draw = ImGui.GetWindowDrawList();

            Vector2 pos = ImGui.GetWindowPos();
            Vector2 size = ImGui.GetWindowSize();

            Vector2 bgMin = pos;
            Vector2 bgMax = pos + new Vector2(size.X, headerHeight);

            // ---- GLASS HEADER BACKGROUND ----
            uint bgTop = ImGui.GetColorU32(new Vector4(0.04f, 0.04f, 0.08f, 0.50f * _uiAlpha));
            uint bgBot = ImGui.GetColorU32(new Vector4(0.06f, 0.06f, 0.12f, 0.70f * _uiAlpha));
            draw.AddRectFilledMultiColor(bgMin, bgMax, bgTop, bgTop, bgBot, bgBot);

            // Bottom border with neon cyan glow
            Vector2 lineStart = new Vector2(bgMin.X, bgMax.Y);
            Vector2 lineEnd = bgMax;
            uint lineCol = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.75f * _uiAlpha));
            draw.AddLine(lineStart, lineEnd, lineCol, 1.2f);
            
            // Subtle soft glow under the separator line
            for (int i = 1; i <= 6; i++)
            {
                float falloff = MathF.Exp(-i * i * 0.4f);
                uint glowCol = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.18f * falloff * _uiAlpha));
                draw.AddLine(lineStart + new Vector2(0, i * 0.5f), lineEnd + new Vector2(0, i * 0.5f), glowCol, 1.0f);
            }

            bool hasHeaderFont = HeaderFont.IsLoaded();

            if (hasHeaderFont) ImGui.PushFont(HeaderFont);

            string hexWord = "FX";
            string corpWord = BrandTitleAccent;

            Vector2 hexSz = ImGui.CalcTextSize(hexWord);
            Vector2 corpSz = ImGui.CalcTextSize(corpWord);

            float textY = pos.Y + (headerHeight - hexSz.Y) * 0.5f;
            float leftPad = 14f;
            float gap = 5f;

            Vector2 hexPos = new(pos.X + leftPad, textY);
            Vector2 corpPos = new(hexPos.X + hexSz.X + gap, textY);

            // Soft, dynamic professional glow behind "HEX CORPORATION" text
            {
                float time = (float)ImGui.GetTime();
                float pulse = 0.5f + 0.5f * MathF.Sin(time * 2.0f);
                int layers = 12;
                float maxSpread = 18f;
                // Center glow ONLY behind HEX
                Vector2 hexGlowCenter = new Vector2(hexPos.X + hexSz.X * 0.5f, hexPos.Y + hexSz.Y * 0.5f);
                Vector2 hexGlowHalfSz = new Vector2(hexSz.X * 0.5f + 2f, hexSz.Y * 0.5f + 2f);
                // Center glow ONLY behind CORPORATION
                Vector2 corpGlowCenter = new Vector2(corpPos.X + corpSz.X * 0.5f, corpPos.Y + corpSz.Y * 0.5f);
                Vector2 corpGlowHalfSz = new Vector2(corpSz.X * 0.5f + 2f, corpSz.Y * 0.5f + 2f);

                for (int i = 0; i < layers; i++)
                {
                    float t = i / (float)layers;
                    float falloff = MathF.Exp(-t * t * 4.5f);
                    float spread = t * 24f; // softer spread
                    float alphaHex = falloff * (0.015f + 0.01f * pulse) * _uiAlpha;
                    float alphaCorp = falloff * (0.008f + 0.004f * pulse) * _uiAlpha; // Subtle white glow
                    
                    // HEX (RED GLOW)
                    draw.AddRectFilled(
                        hexGlowCenter - hexGlowHalfSz - new Vector2(spread, spread),
                        hexGlowCenter + hexGlowHalfSz + new Vector2(spread, spread),
                        ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, alphaHex)),
                        8f + spread);
                        
                    // CORPORATION (WHITE GLOW)
                    draw.AddRectFilled(
                        corpGlowCenter - corpGlowHalfSz - new Vector2(spread, spread),
                        corpGlowCenter + corpGlowHalfSz + new Vector2(spread, spread),
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, alphaCorp)),
                        8f + spread);
                }
            }

            // HEX (red = ThemeColor)
            ImGui.SetCursorScreenPos(hexPos);
            ImGui.PushStyleColor(ImGuiCol.Text,
                new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, _uiAlpha));
            ImGui.TextUnformatted(hexWord);
            ImGui.PopStyleColor();

            // CORPORATION (white)
            ImGui.SetCursorScreenPos(corpPos);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, _uiAlpha));
            ImGui.TextUnformatted(corpWord);
            ImGui.PopStyleColor();

            if (hasHeaderFont) ImGui.PopFont();

            // ---- CLOSE BUTTON  ("X" text style) ----
            float closeHitW = 22f;
            float closeHitH = 22f;
            Vector2 closeHitPos = new(
                bgMax.X - closeHitW - 10f,
                pos.Y + (headerHeight - closeHitH) * 0.5f
            );

            ImGui.SetCursorScreenPos(closeHitPos);
            ImGui.InvisibleButton("##close_btn", new Vector2(closeHitW, closeHitH));
            bool closeHovered = ImGui.IsItemHovered();
            bool closeClicked = ImGui.IsItemClicked();

            if (closeClicked)
                _uiVisible = false;

            // Draw a clean X using two lines (matches reference screenshot style)
            float xPad = 5f;
            float xThick = 1.5f;
            Vector2 x0 = closeHitPos + new Vector2(xPad, xPad);
            Vector2 x1 = closeHitPos + new Vector2(closeHitW - xPad, closeHitH - xPad);
            Vector2 x2 = closeHitPos + new Vector2(closeHitW - xPad, xPad);
            Vector2 x3 = closeHitPos + new Vector2(xPad, closeHitH - xPad);

            Vector4 xColor = closeHovered
                ? new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)
                : new Vector4(1f, 1f, 1f, 0.60f * _uiAlpha);
            uint xCol = ImGui.GetColorU32(xColor);

            draw.AddLine(x0, x1, xCol, xThick);
            draw.AddLine(x2, x3, xCol, xThick);

            ImGui.EndChild();
        }

        private void DrawHorizontalHeaderAndTabs(Vector2 pos, float width, float height)
        {
            var draw = ImGui.GetWindowDrawList();
            Vector2 bgMin = pos;
            Vector2 bgMax = pos + new Vector2(width, height);

            // ---- GLASS HEADER BACKGROUND ----
            uint bgTop = ImGui.GetColorU32(new Vector4(0.04f, 0.04f, 0.08f, 0.50f * _uiAlpha));
            uint bgBot = ImGui.GetColorU32(new Vector4(0.06f, 0.06f, 0.12f, 0.70f * _uiAlpha));
            draw.AddRectFilledMultiColor(bgMin, bgMax, bgTop, bgTop, bgBot, bgBot);

            // Bottom border with neon cyan glow
            Vector2 lineStart = new Vector2(bgMin.X, bgMax.Y);
            Vector2 lineEnd = bgMax;
            uint lineCol = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.75f * _uiAlpha));
            draw.AddLine(lineStart, lineEnd, lineCol, 1.2f);
            
            // Glow under the separator line
            for (int i = 1; i <= 6; i++)
            {
                float falloff = MathF.Exp(-i * i * 0.4f);
                uint glowCol = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.18f * falloff * _uiAlpha));
                draw.AddLine(lineStart + new Vector2(0, i * 0.5f), lineEnd + new Vector2(0, i * 0.5f), glowCol, 1.0f);
            }

            // Draw Brand Title
            bool hasHeaderFont = HeaderFont.IsLoaded();
            if (hasHeaderFont) ImGui.PushFont(HeaderFont);

            string hexWord = "FX";
            string corpWord = BrandTitleAccent;
            Vector2 hexSz = ImGui.CalcTextSize(hexWord);
            Vector2 corpSz = ImGui.CalcTextSize(corpWord);

            float textY = pos.Y + (height - hexSz.Y) * 0.5f;
            float leftPad = 14f;
            float gap = 5f;

            Vector2 hexPos = new(pos.X + leftPad, textY);
            Vector2 corpPos = new(hexPos.X + hexSz.X + gap, textY);

            // Draw text
            ImGui.SetCursorScreenPos(hexPos);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, _uiAlpha));
            ImGui.TextUnformatted(hexWord);
            ImGui.PopStyleColor();

            ImGui.SetCursorScreenPos(corpPos);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, _uiAlpha));
            ImGui.TextUnformatted(corpWord);
            ImGui.PopStyleColor();

            if (hasHeaderFont) ImGui.PopFont();

            // Draw Horizontal Tabs on the right side of the header
            if (_activeTab >= UiMetrics.MainSidebarTabCount)
                _activeTab = 0;

            float tabStartLocalX = hexPos.X + hexSz.X + corpSz.X + 45f;
            float tabY = pos.Y + (height - UiMetrics.TabH) * 0.5f;
            float slotW = UiMetrics.TabW + 12f;

            // Draw each tab
            _tabDropTargetValid = false;
            DrawTab("Aim", 0, tabStartLocalX - pos.X, tabY - pos.Y);
            DrawTab("Esp", 1, tabStartLocalX + slotW - pos.X, tabY - pos.Y);
            DrawTab("Misc", 2, tabStartLocalX + slotW * 2f - pos.X, tabY - pos.Y);
            DrawTab("Hooks", 3, tabStartLocalX + slotW * 3f - pos.X, tabY - pos.Y);
            DrawTab("Settings", 4, tabStartLocalX + slotW * 4f - pos.X, tabY - pos.Y);

            // Animate sliding tab pill
            var io = ImGui.GetIO();
            int fadeActive = _activeTab;
            foreach (var key in _tabFade.Keys)
            {
                float target = (fadeActive == key) ? 1f : 0f;
                _tabFade[key] += (target - _tabFade[key]) * io.DeltaTime * 14f;
                _tabFade[key] = Math.Clamp(_tabFade[key], 0f, 1f);
            }

            if (_tabDropTargetValid)
            {
                if (_tabDropLocalY < 0f)
                {
                    _tabDropLocalY = _tabDropTargetLocalY;
                    _tabDropLocalX = _tabDropTargetLocalX;
                }

                float smooth = 1f - MathF.Exp(-16f * io.DeltaTime);
                _tabDropLocalY += (_tabDropTargetLocalY - _tabDropLocalY) * smooth;
                _tabDropLocalX += (_tabDropTargetLocalX - _tabDropLocalX) * smooth;

                float barX = pos.X + _tabDropLocalX;
                float barY = pos.Y + _tabDropLocalY;

                Vector4 lineRgb = new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 1f);
                uint mainCol = ImGui.GetColorU32(new Vector4(lineRgb.X, lineRgb.Y, lineRgb.Z, 0.12f * _uiAlpha));
                uint borderCol = ImGui.GetColorU32(new Vector4(lineRgb.X, lineRgb.Y, lineRgb.Z, 0.85f * _uiAlpha));

                // Glow
                for (int i = 1; i <= 6; i++)
                {
                    float falloff = MathF.Exp(-i * i * 0.45f);
                    uint glowCol = ImGui.GetColorU32(new Vector4(lineRgb.X, lineRgb.Y, lineRgb.Z, 0.14f * falloff * _uiAlpha));
                    draw.AddRect(
                        new Vector2(barX - i * 0.5f, barY - i * 0.5f),
                        new Vector2(barX + _tabDropLocalW + i * 0.5f, barY + _tabDropLocalH + i * 0.5f),
                        glowCol, 6f, ImDrawFlags.None, 1.0f);
                }

                draw.AddRectFilled(new Vector2(barX, barY), new Vector2(barX + _tabDropLocalW, barY + _tabDropLocalH), mainCol, 6f);
                draw.AddRect(new Vector2(barX, barY), new Vector2(barX + _tabDropLocalW, barY + _tabDropLocalH), borderCol, 6f, ImDrawFlags.None, 1.2f);
            }

            // Close button on the far right
            float closeHitW = 22f;
            float closeHitH = 22f;
            Vector2 closeHitPos = new Vector2(bgMax.X - closeHitW - 12f, pos.Y + (height - closeHitH) * 0.5f);

            ImGui.SetCursorScreenPos(closeHitPos);
            ImGui.InvisibleButton("##close_btn", new Vector2(closeHitW, closeHitH));
            bool closeHovered = ImGui.IsItemHovered();
            if (ImGui.IsItemClicked())
                _uiVisible = false;

            float xPad = 5f;
            float xThick = 1.5f;
            Vector2 x0 = closeHitPos + new Vector2(xPad, xPad);
            Vector2 x1 = closeHitPos + new Vector2(closeHitW - xPad, closeHitH - xPad);
            Vector2 x2 = closeHitPos + new Vector2(closeHitW - xPad, xPad);
            Vector2 x3 = closeHitPos + new Vector2(xPad, closeHitH - xPad);

            Vector4 xColor = closeHovered
                ? new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)
                : new Vector4(1f, 1f, 1f, 0.60f * _uiAlpha);
            uint xCol = ImGui.GetColorU32(xColor);

            draw.AddLine(x0, x1, xCol, xThick);
            draw.AddLine(x2, x3, xCol, xThick);
        }

        private void FillPanelBackground(float rounding, ImDrawFlags roundFlags = ImDrawFlags.None)
        {
            var draw = ImGui.GetWindowDrawList();
            Vector2 p = ImGui.GetWindowPos();
            Vector2 s = ImGui.GetWindowSize();
            draw.AddRectFilled(p, p + s,
                ImGui.GetColorU32(new Vector4(CardBodyBg.X, CardBodyBg.Y, CardBodyBg.Z, UiMetrics.PanelGlassAlpha * _uiAlpha)),
                rounding,
                roundFlags);
        }

        private static void PushPanelContentPadding()
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(UiMetrics.PanelPad, UiMetrics.PanelPad));
        }

        private void DrawPanelBackground(float rounding)
        {
            FillPanelBackground(rounding);
            PushPanelContentPadding();
        }

        private void DrawLoginGateMessage()
        {
            float a = Math.Clamp(_uiAlpha, 0f, 1f);
            Vector2 avail = ImGui.GetContentRegionAvail();
            var draw = ImGui.GetWindowDrawList();
            Vector2 winPos = ImGui.GetWindowPos();

            // -- Centered in available area --
            float lockIconSize = 28f;
            float textGap = 14f;

            // Use FA lock glyph if available, else fallback to a text symbol
            string lockGlyph = "\uF023"; // FA lock
            bool hasFa = TextFont.IsLoaded() && _faMergedIntoTextFont;

            // Measure text
            if (hasFa) ImGui.PushFont(TextFont);
            Vector2 lockSz = ImGui.CalcTextSize(lockGlyph);
            if (hasFa) ImGui.PopFont();

            if (LebelFont.IsLoaded()) ImGui.PushFont(LebelFont);
            Vector2 mainSz = ImGui.CalcTextSize("Login first to use features");
            if (LebelFont.IsLoaded()) ImGui.PopFont();

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            Vector2 subSz = ImGui.CalcTextSize("Select the account tab to login");
            if (TextFont.IsLoaded()) ImGui.PopFont();

            float totalH = lockIconSize + textGap + mainSz.Y + 6f + subSz.Y;
            float startY = (avail.Y - totalH) * 0.5f;
            float centerX = avail.X * 0.5f;

            Vector2 cp = ImGui.GetCursorPos();

            // Soft glow behind lock icon
            Vector2 glowCenter = winPos + cp + new Vector2(centerX, startY + lockIconSize * 0.5f);
            for (int layer = 5; layer >= 1; layer--)
            {
                float spread = layer * 5f;
                float alpha = (0.04f / layer) * a;
                draw.AddCircleFilled(glowCenter,
                    lockIconSize * 0.6f + spread,
                    ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, alpha)));
            }

            // Lock icon
            float iconX = centerX - (hasFa ? lockSz.X : lockIconSize) * 0.5f;
            float iconY = startY + (lockIconSize - (hasFa ? lockSz.Y : lockIconSize)) * 0.5f;
            ImGui.SetCursorPos(new Vector2(cp.X + iconX, cp.Y + iconY));
            if (hasFa)
            {
                ImGui.PushFont(TextFont);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.9f * a));
                ImGui.TextUnformatted(lockGlyph);
                ImGui.PopStyleColor();
                ImGui.PopFont();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.9f * a));
                ImGui.TextUnformatted("[ ]");
                ImGui.PopStyleColor();
            }

            // Main text
            float mainY = startY + lockIconSize + textGap;
            ImGui.SetCursorPos(new Vector2(cp.X + centerX - mainSz.X * 0.5f, cp.Y + mainY));
            if (LebelFont.IsLoaded()) ImGui.PushFont(LebelFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 0.92f * a));
            ImGui.TextUnformatted("Login first to use features");
            ImGui.PopStyleColor();
            if (LebelFont.IsLoaded()) ImGui.PopFont();

            float subY = mainY + mainSz.Y + 6f;
            ImGui.SetCursorPos(new Vector2(cp.X + centerX - subSz.X * 0.5f, cp.Y + subY));
            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.58f, 0.88f * a));
            ImGui.TextUnformatted("Select the account tab to login");
            ImGui.PopStyleColor();
            if (TextFont.IsLoaded()) ImGui.PopFont();
        }

        private void DrawSidebarTabRailDecor(
            ImDrawListPtr draw,
            Vector2 sidebarScreenPos,
            float sidebarW,
            float blockStartLocalY,
            float tabBlockH,
            int activeIndex,
            int tabCount)
        {
            if (tabCount <= 0)
                return;

            float centerX = sidebarScreenPos.X + sidebarW * 0.5f;
            float blockTop = sidebarScreenPos.Y + blockStartLocalY;
            float blockBottom = blockTop + tabBlockH;
            float slotH = UiMetrics.TabH + UiMetrics.TabGap;
            float tri = UiMetrics.SidebarTabChevronSize;
            float pad = UiMetrics.SidebarTabChevronPad;

            uint col = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.88f * _uiAlpha));
            uint colSoft = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.45f * _uiAlpha));

            Vector2 topC = new Vector2(centerX, blockTop - pad);
            draw.AddTriangleFilled(
                topC + new Vector2(0f, -tri),
                topC + new Vector2(-tri * 0.95f, tri * 0.55f),
                topC + new Vector2(tri * 0.95f, tri * 0.55f),
                col);

            Vector2 botC = new Vector2(centerX, blockBottom + pad);
            draw.AddTriangleFilled(
                botC + new Vector2(0f, tri),
                botC + new Vector2(-tri * 0.95f, -tri * 0.55f),
                botC + new Vector2(tri * 0.95f, -tri * 0.55f),
                col);

            int idx = Math.Clamp(activeIndex, 0, tabCount - 1);
            float activeMidY = blockTop + slotH * idx + UiMetrics.TabH * 0.5f;
            float sideX = tri + 3f;

            Vector2 leftC = new Vector2(sidebarScreenPos.X + sideX, activeMidY);
            draw.AddTriangleFilled(
                leftC + new Vector2(tri, 0f),
                leftC + new Vector2(-tri * 0.55f, -tri * 0.9f),
                leftC + new Vector2(-tri * 0.55f, tri * 0.9f),
                colSoft);

            Vector2 rightC = new Vector2(sidebarScreenPos.X + sidebarW - sideX, activeMidY);
            draw.AddTriangleFilled(
                rightC + new Vector2(-tri, 0f),
                rightC + new Vector2(tri * 0.55f, -tri * 0.9f),
                rightC + new Vector2(tri * 0.55f, tri * 0.9f),
                colSoft);
        }

        private void DrawTab(string text, int index, float tabX, float localY)
        {
            float tabWidth = UiMetrics.TabW;
            float tabHeight = UiMetrics.TabH;

            if (!_tabAnim.ContainsKey(index)) _tabAnim[index] = 0f;
            if (!_tabFade.ContainsKey(index)) _tabFade[index] = (index == _activeTab) ? 1f : 0f;

            bool active = _activeTab == index;
            var draw = ImGui.GetWindowDrawList();
            var io = ImGui.GetIO();

            ImGui.SetCursorPos(new Vector2(tabX, localY));
            Vector2 screenPos = ImGui.GetCursorScreenPos();
            Vector2 tabSize = new(tabWidth, tabHeight);

            ImGui.InvisibleButton($"##tab_cs_{index}", tabSize);
            bool hovered = ImGui.IsItemHovered();
            bool clicked = ImGui.IsItemClicked();

            if (clicked && index != _activeTab)
            {
                _previousTab = _activeTab;
                _activeTab = index;
                if (_flowPhase == FlowPhase.Main && index < UiMetrics.LoginTabIndex)
                    _showTabPopup = false;
            }

            float targetAnim = active ? 1f : 0f;
            _tabAnim[index] += (targetAnim - _tabAnim[index]) * io.DeltaTime * 12f;
            _tabAnim[index] = Math.Clamp(_tabAnim[index], 0f, 1f);
            float anim = _tabAnim[index];

            if (active)
            {
                _tabDropTargetLocalX = tabX;
                _tabDropTargetLocalY = localY;
                _tabDropLocalW = 3f;
                _tabDropLocalH = tabHeight;
                _tabDropTargetValid = true;

                draw.AddRectFilled(
                    new Vector2(screenPos.X, screenPos.Y + 3f),
                    new Vector2(screenPos.X + 3f, screenPos.Y + tabHeight - 3f),
                    ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)),
                    1.5f);
            }

            Vector4 inactiveGlyph = new(0.40f, 0.40f, 0.42f, _uiAlpha);
            Vector4 activeGlyph = new(1f, 1f, 1f, _uiAlpha);
            Vector4 glyphColor = new Vector4(
                inactiveGlyph.X + (activeGlyph.X - inactiveGlyph.X) * anim,
                inactiveGlyph.Y + (activeGlyph.Y - inactiveGlyph.Y) * anim,
                inactiveGlyph.Z + (activeGlyph.Z - inactiveGlyph.Z) * anim,
                _uiAlpha);

            if (active)
            {
                draw.AddRectFilled(screenPos, screenPos + tabSize,
                    ImGui.GetColorU32(new Vector4(14f / 255f, 15f / 255f, 21f / 255f, _uiAlpha)), 4f);
            }

            // Hover highlight
            if (hovered && !active)
            {
                draw.AddRectFilled(screenPos, screenPos + tabSize,
                    ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.04f * _uiAlpha)), 4f);
            }

            bool useFa = TextFont.IsLoaded() && _faMergedIntoTextFont && index >= 0 && index < TabFaGlyphs.Length;
            if (useFa)
            {
                ImGui.PushFont(TextFont);
                string glyph = TabFaGlyphs[index];
                Vector2 gsz = ImGui.CalcTextSize(glyph);
                Vector2 iconCenter = screenPos + tabSize * 0.5f;
                Vector2 gpos = new Vector2(
                    iconCenter.X - gsz.X * 0.5f,
                    iconCenter.Y - gsz.Y * 0.5f);

                draw.AddText(gpos + new Vector2(1f, 1f),
                    ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.3f * _uiAlpha)),
                    glyph);
                draw.AddText(gpos, ImGui.GetColorU32(glyphColor), glyph);
                ImGui.PopFont();
            }
            else
            {
                IntPtr iconPtr = _tabIconPtrs.TryGetValue(index, out var ptr) ? ptr : IntPtr.Zero;
                if (iconPtr != IntPtr.Zero)
                {
                    float iconSize = 16f;
                    Vector2 iconPos = screenPos + (tabSize - new Vector2(iconSize, iconSize)) * 0.5f;
                    draw.AddImage(iconPtr, iconPos, iconPos + new Vector2(iconSize, iconSize));
                }
                else
                {
                    Vector2 textSz = ImGui.CalcTextSize(text);
                    Vector2 tpos = screenPos + (tabSize - textSz) * 0.5f;
                    draw.AddText(tpos, ImGui.GetColorU32(glyphColor), text);
                }
            }
        }

        private void DrawTabs()
        {
            _tabDropTargetValid = false;
            ImGui.Dummy(new Vector2(1f, UiMetrics.SidebarTabTopPad));

            if (_flowPhase == FlowPhase.Main)
            {
                if (_activeTab >= UiMetrics.MainSidebarTabCount)
                    _activeTab = 0;

                int visibleTabCount = UiMetrics.MainSidebarTabCount;

                float sidebarW = ImGui.GetWindowSize().X;
                float tabX = MathF.Max(0f, (sidebarW - UiMetrics.TabW) * 0.5f);
                float slotH = UiMetrics.TabH + UiMetrics.TabGap;
                float tabBlockH = slotH * visibleTabCount - UiMetrics.TabGap;

                float availH = ImGui.GetContentRegionAvail().Y;
                float blockPad = MathF.Max(0f, (availH - tabBlockH) * UiMetrics.SidebarTabBlockBias);
                float blockStartY = ImGui.GetCursorPos().Y + blockPad;

                DrawTab("Aim", 0, tabX, blockStartY);
                DrawTab("Esp", 1, tabX, blockStartY + slotH);
                DrawTab("Misc", 2, tabX, blockStartY + slotH * 2f);
                DrawTab("Ext", 3, tabX, blockStartY + slotH * 3f);
                DrawTab("Set", 4, tabX, blockStartY + slotH * 4f);

                ImGui.SetCursorPos(new Vector2(0f, blockStartY + tabBlockH + 2f));
            }
            if (TextFont.IsLoaded())
                ImGui.PushFont(TextFont);

            var io = ImGui.GetIO();
            int fadeActive = _activeTab;
            foreach (var key in _tabFade.Keys)
            {
                float target = (fadeActive == key) ? 1f : 0f;
                _tabFade[key] += (target - _tabFade[key]) * io.DeltaTime * 14f;
                _tabFade[key] = Math.Clamp(_tabFade[key], 0f, 1f);
            }

            // Draw clean royal-blue active bar inside the sidebar tab slot
            if (_tabDropTargetValid)
            {
                if (_tabDropLocalY < 0f)
                {
                    _tabDropLocalY = _tabDropTargetLocalY;
                    _tabDropLocalX = _tabDropTargetLocalX;
                }

                float smooth = 1f - MathF.Exp(-16f * io.DeltaTime);
                _tabDropLocalY += (_tabDropTargetLocalY - _tabDropLocalY) * smooth;
                _tabDropLocalX += (_tabDropTargetLocalX - _tabDropLocalX) * smooth;

                Vector2 winPos = ImGui.GetWindowPos();
                float barX = winPos.X + _tabDropLocalX;
                float barY = winPos.Y + _tabDropLocalY;

                var draw = ImGui.GetWindowDrawList();
                draw.AddRectFilled(
                    new Vector2(barX, barY + 3f),
                    new Vector2(barX + _tabDropLocalW, barY + _tabDropLocalH - 3f),
                    ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)),
                    1.5f);
            }

            if (TextFont.IsLoaded())
                ImGui.PopFont();
        }

        private void DrawContentFade(Action drawAction)
        {
            float fade = _tabFade.ContainsKey(_activeTab) ? _tabFade[_activeTab] : 1f;
            if (fade <= 0f) return;

            float combinedAlpha = fade * _uiAlpha;
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, combinedAlpha));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(1f, 1f, 1f, combinedAlpha));
            // Subtle dark divider under tab titles (~#2A2A2A), matches reference
            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(42f / 255f, 42f / 255f, 42f / 255f, combinedAlpha));
            drawAction.Invoke();
            ImGui.PopStyleColor(3);
        }

        private void DrawMainContent()
        {
            if (_flowPhase == FlowPhase.Login)
            {
                float a = Math.Clamp(_uiAlpha, 0f, 1f);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, a));
                DrawEmbeddedLoginPanel();
                ImGui.PopStyleColor();
                return;
            }

            if (_flowPhase != FlowPhase.Main)
                return;

            DrawContentFade(() =>
            {
                if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
                switch (_activeTab)
                {
                    case 0:
                        RenderCombatAimTab();
                        break;
                    case 1:
                        RenderVisualsTab();
                        break;
                    case 2:
                        RenderCombatMovementTab();
                        break;
                    case 3:
                        RenderExtremeHooksTab();
                        break;
                    case 4:
                        RenderSettingsTab();
                        break;
                    default:
                        RenderCombatAimTab();
                        break;
                }
                if (TextFont.IsLoaded()) ImGui.PopFont();
            });
        }

        // === Combat / movement / kill UI sections (moved from CombatFeatureUi.cs into main ESP.cs) ===

        private void BeginFeatureScrollArea(string scrollId = "main")
        {
            float h = ImGui.GetContentRegionAvail().Y;
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(1f, 1f, 1f, 0.04f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.65f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.85f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 1f * _uiAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 8f);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
            ImGui.BeginChild($"##feature_scroll_{scrollId}", new Vector2(0f, h), ImGuiChildFlags.None,
                ImGuiWindowFlags.NoBackground);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 5f));
        }

        private void EndFeatureScrollArea()
        {
            ImGui.PopStyleVar();
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(4);
        }

        private bool FeatureCheckbox(string label, ref bool value, Action<bool>? onChanged = null)
        {
            bool pressed = CustomCheckbox.CaxCustomCheckbox(label, ref value);
            if (pressed && onChanged != null)
                onChanged(value);
            return pressed;
        }

        private bool FeatureCheckboxStackRow(float anchorX, float rowWidth, string label, ref bool value, Action<bool>? onChanged = null, string? bindId = null)
        {
            const float bindW = 74f;
            const float bindH = 22f;
            const float bindGap = 6f;

            ImGui.SetCursorPosX(anchorX);
            Vector2 rowStart = ImGui.GetCursorScreenPos();
            float labelClipW = string.IsNullOrEmpty(bindId) ? rowWidth : rowWidth - bindW - bindGap;

            ImGui.PushClipRect(rowStart, rowStart + new Vector2(labelClipW, 28f), true);
            bool result = FeatureCheckbox(label, ref value, onChanged);
            ImGui.PopClipRect();

            if (!string.IsNullOrEmpty(bindId))
            {
                FeatureKeybinds.EnsureInitialized();
                Vector2 rowEnd = ImGui.GetItemRectMax();
                float bindY = rowStart.Y + MathF.Max(0f, (rowEnd.Y - rowStart.Y - bindH) * 0.5f);
                ImGui.SetCursorScreenPos(new Vector2(rowStart.X + rowWidth - bindW, bindY));
                AnimatedKeybind.Draw(bindId, new Vector2(bindW, bindH));
            }

            Vector2 screenP = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(
                screenP + new Vector2(0f, 2f),
                screenP + new Vector2(rowWidth, 2f),
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.40f * _uiAlpha)),
                1f);
            ImGui.Dummy(new Vector2(rowWidth, 8f));

            return result;
        }

        private bool FeatureCheckboxKeybindRow(float rowX, float rowWidth, string label, ref bool value, Action<bool>? onChanged, string bindId)
        {
            return FeatureCheckboxStackRow(rowX, rowWidth, label, ref value, onChanged, bindId);
        }

        private void DrawAimbotHoldKeyRow(float rowWidth)
        {
            const float bindW = 74f;
            const float bindH = 22f;
            float rowX = ImGui.GetCursorPosX();

            FeatureKeybinds.EnsureInitialized();
            if (FeatureKeybinds.GetKey("AimbotKey") == Keys.None && Config.AimbotKey != Keys.None)
                FeatureKeybinds.SetKey("AimbotKey", Config.AimbotKey);

            ImGui.SetCursorPosX(rowX);
            Vector2 rowStart = ImGui.GetCursorScreenPos();
            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Aimbot Hold Key");
            if (TextFont.IsLoaded()) ImGui.PopFont();

            Vector2 rowEnd = ImGui.GetItemRectMax();
            float bindY = rowStart.Y + MathF.Max(0f, (rowEnd.Y - rowStart.Y - bindH) * 0.5f);
            ImGui.SetCursorScreenPos(new Vector2(rowStart.X + rowWidth - bindW, bindY));
            AnimatedKeybind.Draw("AimbotKey", new Vector2(bindW, bindH));
            Config.AimbotKey = FeatureKeybinds.GetKey("AimbotKey");
            if (Config.AimbotKey == Keys.None)
                Config.AimbotKey = Keys.LButton;

            Vector2 screenP = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(
                screenP + new Vector2(0f, 2f),
                screenP + new Vector2(rowWidth, 2f),
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.40f * _uiAlpha)),
                1f);
            ImGui.Dummy(new Vector2(rowWidth, 8f));
        }

        private static bool DrawKeyBind(string id, ref System.Windows.Forms.Keys key, Vector2 size)
        {
            string label = key.ToString();
            if (label == "LButton") label = "LMouse";
            else if (label == "RButton") label = "RMouse";
            else if (label == "MButton") label = "MMouse";
            else if (label == "XButton1") label = "Mouse4";
            else if (label == "XButton2") label = "Mouse5";

            bool isBinding = _activeBindingId == id;
            if (isBinding)
            {
                label = "...";
                ImGuiIOPtr io = ImGui.GetIO();
                for (int i = 1; i < 256; i++)
                {
                    if (ImGui.IsKeyDown((ImGuiKey)i))
                    {
                        key = (System.Windows.Forms.Keys)i;
                        _activeBindingId = null;
                        break;
                    }
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) { key = System.Windows.Forms.Keys.LButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) { key = System.Windows.Forms.Keys.RButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle)) { key = System.Windows.Forms.Keys.MButton; _activeBindingId = null; }
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(12f / 255f, 13f / 255f, 18f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(20f / 255f, 21f / 255f, 28f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.PushStyleColor(ImGuiCol.Border, isBinding ? new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha) : new Vector4(30f / 255f, 32f / 255f, 40f / 255f, _uiAlpha));

            string text = $"{label}  \uF11C";
            bool pressed = ImGui.Button(text + "##btn_" + id, size);
            if (pressed)
            {
                _activeBindingId = isBinding ? null : id;
            }

            ImGui.PopStyleColor(4);
            ImGui.PopStyleVar(2);
            return isBinding;
        }

        private static bool FeatureKeyBindStackRow(float anchorX, float rowWidth, string label, ref System.Windows.Forms.Keys key)
        {
            ImGui.SetCursorPosX(anchorX);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SameLine(0, 0);
            
            float btnW = 68f;
            float btnH = 20f;
            ImGui.SetCursorPosX(anchorX + rowWidth - btnW);
            
            bool result = DrawKeyBind(label, ref key, new Vector2(btnW, btnH));
            
            Vector2 screenP = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(screenP + new Vector2(0f, 2f), screenP + new Vector2(rowWidth, 2f), ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.40f * _uiAlpha)), 1f);
            ImGui.Dummy(new Vector2(rowWidth, 8f));
            
            return result;
        }

        private void ApplySpinBotState(bool enabled)
        {
            if (enabled)
                RapidSpin.Activate();
            else
                RapidSpin.Deactivate();
        }

        private void ApplyFlyInvisible(bool enabled)
        {
            Config.FlyHack50x = enabled;
            Config.FlyHack50xx = enabled;
        }

        private void ApplyFlyX80Int(bool enabled)
        {
            Config.FLYHACKINTTTT = enabled;
            Config.FLYHACKINTTT = enabled;
        }

        private void RenderCombatAimTab()
        {
            GetSingleColumnLayout(out float colW, out float innerW);
            BeginFeatureScrollArea("aim");

            DrawCard("Aim Functions", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Combat Assist (Hold LMB)", ref enableAimBot, bindId: "enableAimBot");
                FeatureCheckboxStackRow(rowX, innerW, "Rage Aim", ref AimBotRage, bindId: "AimBotRage");
                FeatureCheckboxStackRow(rowX, innerW, "Silent Aim (Safe)", ref kcsafeSilentAim, bindId: "kcsafeSilentAim");
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.15f, 0.15f, 1f));
                FeatureCheckboxStackRow(rowX, innerW, "Silent Aim (Brutal) (RISK)", ref kcbrutasilnetaim, bindId: "kcbrutasilnetaim");
                FeatureCheckboxStackRow(rowX, innerW, "Silent one weapon only", ref SilentWeaponOnly, bindId: "SilentWeaponOnly");
                string[] silentGuns = { "AK47", "M4A1", "SCAR", "XM8", "AN94", "FAMAS", "GROZA", "AUG", "PARAFAL", "M14", "SKS", "SVD", "KAR98K", "AWM", "M82B", "M249", "MP40", "MP5", "UMP", "VECTOR", "THOMPSON", "P90", "MAG7", "M1014", "M1887", "SPAS12", "G18", "USP", "DESERT EAGLE", "M500", "KINGFISHER", "TROGON", "WOODPECKER", "M24", "SKORP", "RPK", "M7", "HAWK", "UZI", "MAC10", "M60", "VSS", "KORD" };
                int sgi = Array.IndexOf(silentGuns, Config.SilentWeaponKey);
                if (sgi < 0) sgi = 0;
                if (ImGui.Combo("Silent weapon", ref sgi, silentGuns, silentGuns.Length) && sgi >= 0 && sgi < silentGuns.Length)
                    Config.SilentWeaponKey = silentGuns[sgi];
                if (WeaponCatalog.TryGetIcon(this, Config.SilentWeaponKey, out var stex) && stex != IntPtr.Zero)
                {
                    Vector2 pvPos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddRectFilled(pvPos, pvPos + new Vector2(76f, 52f),
                        ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.55f * _uiAlpha)), 4f);
                    ImGui.Image(stex, new Vector2(72, 24));
                }
                ImGui.TextUnformatted("Target: " + WeaponCatalog.GetDisplayName(Config.SilentWeaponKey));
                ImGui.PopStyleColor();
                FeatureCheckboxStackRow(rowX, innerW, "Aimbot Visible", ref AimbotVisible, v => AimbotRubix = v, bindId: "AimbotVisible");
                FeatureCheckboxStackRow(rowX, innerW, "Unlimited Ammo", ref UnlimitedAmmo, bindId: "UnlimitedAmmo");
                FeatureCheckboxStackRow(rowX, innerW, "Rapid Fire", ref RapidFire, bindId: "RapidFire");
                FeatureCheckboxStackRow(rowX, innerW, "Auto Fire", ref AutoFire, bindId: "AutoFire");
                FeatureCheckboxStackRow(rowX, innerW, "No Recoil", ref NoRecoil, bindId: "NoRecoil");
                FeatureCheckboxStackRow(rowX, innerW, "Fast Reload", ref FastReload, bindId: "FastReload");
                FeatureCheckboxStackRow(rowX, innerW, "Up Player", ref UpPlayer, bindId: "UpPlayer");
                FeatureCheckboxStackRow(rowX, innerW, "Forced Aim Lock", ref forceaim, bindId: "forceaim");
                FeatureCheckboxStackRow(rowX, innerW, "Ignore Knocked", ref IgnoreKnocked, bindId: "IgnoreKnocked");
                FeatureCheckboxStackRow(rowX, innerW, "Draw FOV Circle", ref FOVEnabled, bindId: "FOVEnabled");
                if (DrawPremiumSlider("Field of View", ref AimFov, 0f, 1000f, innerW))
                    AimBotFov = (int)MathF.Round(AimFov);
                string[] targetItems = { "Closest To Crosshair", "Farthest From Crosshair", "Target360", "Closest To Player", "Lowest Health" };
                int[] targetMap = { (int)TargetingMode.ClosestToCrosshair, (int)TargetingMode.FarthestFromCrosshair, (int)TargetingMode.Target360, (int)TargetingMode.ClosestToPlayer, (int)TargetingMode.LowestHealth };
                int tii = Array.IndexOf(targetMap, (int)Config.TargetingMode);
                if (tii < 0) tii = 0;
                if (ImGui.Combo("Target", ref tii, targetItems, targetItems.Length) && tii >= 0 && tii < targetMap.Length)
                    Config.TargetingMode = (TargetingMode)targetMap[tii];
                float aimDist = AimBotMaxDistance;
                if (DrawPremiumSlider("Aim Max Distance", ref aimDist, 50f, 500f, innerW))
                    AimBotMaxDistance = (int)aimDist;
                ImGui.Dummy(new Vector2(0f, 4f));
                float aimSmooth = AimbotSmoothness;
                if (DrawPremiumSlider("Aimbot Smoothness", ref aimSmooth, 0f, 100f, innerW))
                    AimbotSmoothness = (int)aimSmooth;
                DrawPremiumSlider("Rapid Fire Speed", ref RapidFireSpeed, 0.1f, 2f, innerW);
                DrawPremiumSlider("Auto Fire Distance", ref AutoFireMaxDistance, 10f, 200f, innerW);
                ImGui.Dummy(new Vector2(0f, 4f));
                DrawPremiumSlider("Head Rate", ref Config.HeadRate, 0f, 100f, innerW);
                DrawPremiumSlider("Chest Rate", ref Config.ChestRate, 0f, 100f, innerW);
                FeatureCheckboxStackRow(rowX, innerW, "Shuriken Multi Damage", ref KXCShurikenMultiDamage, bindId: "KXCShurikenMultiDamage");
                FeatureCheckboxStackRow(rowX, innerW, "Shuriken Fake Damage", ref KXCFakeDame, bindId: "KXCFakeDame");
                FeatureCheckboxStackRow(rowX, innerW, "Spin Bot V2", ref spinbot, ApplySpinBotState, bindId: "spinbot");
                DrawPremiumSlider("Spin Speed", ref SpinBotSpeed, 0.5f, 20f, innerW);
                ImGui.Dummy(new Vector2(0f, 4f));
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull 360", ref EnemyPullEnabled, bindId: "EnemyPullEnabled");
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull X (SAFE)", ref Config.EnemyPullX, bindId: "EnemyPullX");
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull XY (TEST)", ref Config.EnemyPullXY, bindId: "EnemyPullXY");
                FeatureCheckboxStackRow(rowX, innerW, "Tele Kill (enemy to me)", ref EnemyPullEnabledv2, bindId: "EnemyPullEnabledv2");
                ImGui.Dummy(new Vector2(0f, 4f));
                DrawAimbotHoldKeyRow(innerW);
            });

            EndFeatureScrollArea();
        }

        private void RenderCombatMovementTab()
        {
            GetSingleColumnLayout(out float colW, out float innerW);
            BeginFeatureScrollArea("misc");

            DrawCard("Misc Functions", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Fly X80 (Altitude)", ref FLYHACKINTTTT, ApplyFlyX80Int, "FLYHACKINTTTT");
                FeatureCheckboxStackRow(rowX, innerW, "Fly X80 Matrix", ref FlyHack80xx, null, "FlyHack80xx");
                FeatureCheckboxStackRow(rowX, innerW, "Fly X40", ref Config.FLYHACKX400, v =>
                {
                    Config.FLYHACKX400 = v;
                    Config.FLYHACKX40 = v;
                }, "FLYHACKX400");
                FeatureCheckboxStackRow(rowX, innerW, "Invisible Fly (50m)", ref Config.FlyHack50x, ApplyFlyInvisible, "FlyHack50x");
                FeatureCheckboxStackRow(rowX, innerW, "Zero-Gravity Fly", ref FlyHack, KCNOGRAVITYFLY.SetState, "FlyHack");
                FeatureCheckboxStackRow(rowX, innerW, "Climb Assist", ref ClimbUpEnabled, null, "ClimbUpEnabled");
                FeatureCheckboxStackRow(rowX, innerW, "Climb Up V2", ref ClimbUpV2Enabled, null, "ClimbUpV2Enabled");
                DrawPremiumSlider("Climb Height", ref ClimbUpV2Value, 0f, 80f, innerW);
                FeatureCheckboxStackRow(rowX, innerW, "Speed Boost (Timer)", ref speedint, null, "speedint");
                FeatureCheckboxStackRow(rowX, innerW, "FPS Boost (emulator)", ref Config.FpsBoost, null, "FpsBoost");
                FeatureCheckboxStackRow(rowX, innerW, "Teleport Kill (10m)", ref tele, null, "tele");
                FeatureCheckboxStackRow(rowX, innerW, "Shake Kill", ref ShakeKill, null, "ShakeKill");
                FeatureCheckboxStackRow(rowX, innerW, "Down Player", ref Config.DownPlayer, null, "DownPlayer");
                FeatureCheckboxStackRow(rowX, innerW, "Underground Kill", ref undergroundkill, null, "undergroundkill");
                FeatureCheckboxStackRow(rowX, innerW, "Spawn Kill", ref Kcspawnkill, null, "Kcspawnkill");
                FeatureCheckboxStackRow(rowX, innerW, "Quick Teleport", ref teliport, null, "teliport");
                FeatureCheckboxStackRow(rowX, innerW, "AI Teleport Wall", ref teliportwall, null, "teliportwall");
                FeatureCheckboxStackRow(rowX, innerW, "Mark Map Teleport", ref teleportmap, null, "teleportmap");
            });

            EndFeatureScrollArea();
        }

        /// <summary>Re-applies thread toggles after config load or hook success.</summary>
        internal void ApplyLoadedFeatureRuntimeState()
        {
            Config.FlyHack50xx = Config.FlyHack50x;
            Config.FLYHACKINTTT = Config.FLYHACKINTTTT;
            Config.FLYHACKX40 = Config.FLYHACKX400;

            // Anti-crash on entry: movement writers never auto-start from a saved config.
            // They write transforms every frame; any stale offset would kill the game at menu.
            // Re-enable them manually per session.
            Config.spinbot = false;
            ApplySpinBotState(false);
            Config.FlyHack = false;
            KCNOGRAVITYFLY.SetState(false);
            Config.FlyHack50x = false;
            Config.FlyHack50xx = false;
            Config.FlyHack80xx = false;
            Config.FLYHACKINTTTT = false;
            Config.FLYHACKINTTT = false;
            Config.FLYHACKX400 = false;
            Config.FLYHACKX40 = false;
            Config.ClimbUpEnabled = false;
            Config.ClimbUpV2Enabled = false;
        }

        // Legacy names kept for any external references
        private void RenderAimTab() => RenderCombatAimTab();
        private void RenderMiscTab() => RenderCombatMovementTab();

        private static string FormatKeyLabel(System.Windows.Forms.Keys key)
        {
            if (key == System.Windows.Forms.Keys.None)
                return "None";

            string label = key.ToString();
            if (label == "LButton") return "LMouse";
            if (label == "RButton") return "RMouse";
            if (label == "MButton") return "MMouse";
            if (label == "XButton1") return "Mouse4";
            if (label == "XButton2") return "Mouse5";
            return label;
        }

        private static bool DrawKeyLabelButton(string id, ref System.Windows.Forms.Keys key, Vector2 size)
        {
            bool isBinding = _activeBindingId == id;
            if (isBinding)
            {
                ImGuiIOPtr io = ImGui.GetIO();
                for (int i = 1; i < 256; i++)
                {
                    if (ImGui.IsKeyDown((ImGuiKey)i))
                    {
                        key = (System.Windows.Forms.Keys)i;
                        _activeBindingId = null;
                        break;
                    }
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) { key = System.Windows.Forms.Keys.LButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) { key = System.Windows.Forms.Keys.RButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle)) { key = System.Windows.Forms.Keys.MButton; _activeBindingId = null; }
            }

            string label = isBinding ? "..." : FormatKeyLabel(key);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(12f / 255f, 13f / 255f, 18f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(20f / 255f, 21f / 255f, 28f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.PushStyleColor(ImGuiCol.Border, isBinding
                ? new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha)
                : new Vector4(30f / 255f, 32f / 255f, 40f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.62f, 0.62f, 0.66f, _uiAlpha));

            bool pressed = ImGui.Button(label + "##keylbl_" + id, size);
            if (pressed)
                _activeBindingId = isBinding ? null : id;

            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);
            return pressed;
        }

        private static bool DrawKeyBindIconButton(string id, ref System.Windows.Forms.Keys key, Vector2 size)
        {
            bool isBinding = _activeBindingId == id;
            if (isBinding)
            {
                ImGuiIOPtr io = ImGui.GetIO();
                for (int i = 1; i < 256; i++)
                {
                    if (ImGui.IsKeyDown((ImGuiKey)i))
                    {
                        key = (System.Windows.Forms.Keys)i;
                        _activeBindingId = null;
                        break;
                    }
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) { key = System.Windows.Forms.Keys.LButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) { key = System.Windows.Forms.Keys.RButton; _activeBindingId = null; }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle)) { key = System.Windows.Forms.Keys.MButton; _activeBindingId = null; }
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(12f / 255f, 13f / 255f, 18f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(20f / 255f, 21f / 255f, 28f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.PushStyleColor(ImGuiCol.Border, isBinding
                ? new Vector4(10f / 255f, 105f / 255f, 245f / 255f, _uiAlpha)
                : new Vector4(30f / 255f, 32f / 255f, 40f / 255f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.78f, 0.78f, 0.82f, _uiAlpha));

            bool pressed = ImGui.Button("\uF11C##keyicon_" + id, size);
            if (pressed)
                _activeBindingId = isBinding ? null : id;

            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);
            return pressed;
        }

        private void FeatureHookRow(float rowWidth, string label, ref bool value, ref System.Windows.Forms.Keys key, string bindId)
        {
            const float noneBtnW = 52f;
            const float keyBtnW = 26f;
            const float gap = 6f;

            float anchorX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(anchorX);

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.88f, 0.88f, 0.90f, _uiAlpha));
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();

            float noneX = anchorX + rowWidth - noneBtnW - keyBtnW - gap;
            ImGui.SetCursorPosX(noneX);
            DrawKeyLabelButton(bindId + "_lbl", ref key, new Vector2(noneBtnW, 20f));

            ImGui.SameLine(0f, gap);
            DrawKeyBindIconButton(bindId + "_ico", ref key, new Vector2(keyBtnW, 20f));

            Vector2 screenP = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(
                screenP + new Vector2(0f, 2f),
                screenP + new Vector2(rowWidth, 2f),
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.55f * _uiAlpha)),
                1f);
            ImGui.Dummy(new Vector2(rowWidth, 8f));

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                Vector2 mouse = ImGui.GetMousePos();
                Vector2 rowMin = screenP - new Vector2(0f, 22f);
                Vector2 rowMax = screenP + new Vector2(noneX - anchorX, 0f);
                if (mouse.X >= rowMin.X && mouse.X <= rowMax.X && mouse.Y >= rowMin.Y && mouse.Y <= rowMax.Y)
                    value = !value;
            }
        }

        private void DrawCrossArrowsGraphic(Vector2 center, float size)
        {
            var draw = ImGui.GetWindowDrawList();
            uint col = ImGui.GetColorU32(new Vector4(40f / 255f, 245f / 255f, 40f / 255f, 0.95f * _uiAlpha));
            float arm = size * 0.5f;
            float head = size * 0.22f;
            float thick = 2.2f;

            draw.AddLine(center + new Vector2(-arm, 0f), center + new Vector2(arm, 0f), col, thick);
            draw.AddLine(center + new Vector2(0f, -arm), center + new Vector2(0f, arm), col, thick);

            draw.AddTriangleFilled(center + new Vector2(arm + head, 0f), center + new Vector2(arm - head * 0.2f, head * 0.55f), center + new Vector2(arm - head * 0.2f, -head * 0.55f), col);
            draw.AddTriangleFilled(center + new Vector2(-arm - head, 0f), center + new Vector2(-arm + head * 0.2f, head * 0.55f), center + new Vector2(-arm + head * 0.2f, -head * 0.55f), col);
            draw.AddTriangleFilled(center + new Vector2(0f, -arm - head), center + new Vector2(head * 0.55f, -arm + head * 0.2f), center + new Vector2(-head * 0.55f, -arm + head * 0.2f), col);
            draw.AddTriangleFilled(center + new Vector2(0f, arm + head), center + new Vector2(head * 0.55f, arm - head * 0.2f), center + new Vector2(-head * 0.55f, arm - head * 0.2f), col);
        }

        private void RenderExtremeHooksTab()
        {
            GetSingleColumnLayout(out float colW, out float innerW);
            BeginFeatureScrollArea("hooks");

            DrawCard("Hook Functions", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Teleport Kill (10m)", ref tele, bindId: "tele");
                FeatureCheckboxStackRow(rowX, innerW, "Shake Kill", ref ShakeKill, bindId: "ShakeKill");
                FeatureCheckboxStackRow(rowX, innerW, "Down Player", ref Config.DownPlayer, bindId: "DownPlayer");
                FeatureCheckboxStackRow(rowX, innerW, "Underground Kill", ref undergroundkill, bindId: "undergroundkill");
                FeatureCheckboxStackRow(rowX, innerW, "Spawn Kill", ref Kcspawnkill, bindId: "Kcspawnkill");
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull 360", ref EnemyPullEnabled, bindId: "EnemyPullEnabled");
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull X (SAFE)", ref Config.EnemyPullX, bindId: "EnemyPullX");
                FeatureCheckboxStackRow(rowX, innerW, "Enemy Pull XY (TEST)", ref Config.EnemyPullXY, bindId: "EnemyPullXY");
                FeatureCheckboxStackRow(rowX, innerW, "Tele Kill (enemy to me)", ref EnemyPullEnabledv2, bindId: "EnemyPullEnabledv2");
                FeatureCheckboxStackRow(rowX, innerW, "Quick Teleport", ref teliport, bindId: "teliport");
                FeatureCheckboxStackRow(rowX, innerW, "AI Teleport Wall", ref teliportwall, bindId: "teliportwall");
                FeatureCheckboxStackRow(rowX, innerW, "Mark Map Teleport", ref teleportmap, bindId: "teleportmap");
                FeatureCheckboxStackRow(rowX, innerW, "Spin Bot V2", ref spinbot, ApplySpinBotState, bindId: "spinbot");
            });

            EndFeatureScrollArea();
        }

        private static string _feedbackText = "";
        private static bool _fbEveryone;
        private static bool _fbHere;
        private static string _fbStatus = "";
        private static bool _panelKeyWaiting;
        private static float _panelKeyWaitT;
        private static bool _fbKeyWaiting;
        private static float _fbKeyWaitT;
        private static bool _fbKeyLast;
        private static IntPtr _discordLogoPtr = IntPtr.Zero;
        private static bool _discordLogoTried;

        private void DrawKeyBinder(string label, ref System.Windows.Forms.Keys key, ref bool waiting, ref float waitT)
        {
            ImGui.TextUnformatted(label);
            ImGui.SameLine();
            string btn = waiting ? "press key..." : key.ToString();
            if (ImGui.Button(btn + "##kb_" + label, new Vector2(140, 24))) { waiting = true; waitT = (float)ImGui.GetTime(); }
            if (waiting && (float)ImGui.GetTime() - waitT > 0.3f)
            {
                for (int vk = 0x08; vk <= 0xFE; vk++)
                {
                    if (vk == 0x01 || vk == 0x02 || vk == 0x04 || vk == 0x05 || vk == 0x06) continue;
                    if ((WinAPI.GetAsyncKeyState((System.Windows.Forms.Keys)vk) & 0x8000) != 0)
                    {
                        if (vk != 0x1B) key = (System.Windows.Forms.Keys)vk;
                        waiting = false;
                        break;
                    }
                }
            }
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n");
        }

        private static byte[]? CaptureScreenPng()
        {
            try
            {
                var b = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                using var bmp = new System.Drawing.Bitmap(b.Width, b.Height);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                    g.CopyFromScreen(b.Location, System.Drawing.Point.Empty, b.Size);
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch { return null; }
        }

        private static void PostFeedbackAsync(string text, bool shot)
        {
            _fbStatus = "Sending...";
            Task.Run(async () =>
            {
                try
                {
                    string content = ((_fbEveryone ? "@everyone " : "") + (_fbHere ? "@here " : "") + (text ?? "")).Trim();
                    if (content.Length > 1900) content = content.Substring(0, 1900);
                    using var http = new System.Net.Http.HttpClient();
                    http.DefaultRequestHeaders.Add("Authorization", "Bot " + Config.DiscordBotToken);
                    using var form = new System.Net.Http.MultipartFormDataContent();
                    if (shot)
                    {
                        var bytes = CaptureScreenPng();
                        if (bytes != null)
                            form.Add(new System.Net.Http.ByteArrayContent(bytes), "files[0]", "esp.png");
                    }
                    string json = "{\"content\":\"" + EscapeJson(content) + "\",\"allowed_mentions\":{\"parse\":[" + ((_fbEveryone || _fbHere) ? "\"everyone\"" : "") + "]}}";
                    form.Add(new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json"), "payload_json");
                    var resp = await http.PostAsync("https://discord.com/api/v10/channels/" + Config.DiscordChannelId + "/messages", form);
                    string body = "";
                    try { body = await resp.Content.ReadAsStringAsync(); } catch { }
                    try { File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ob54_feedback.txt"), DateTime.Now.ToString("HH:mm:ss") + " HTTP " + (int)resp.StatusCode + " " + (body.Length > 300 ? body.Substring(0, 300) : body) + "\r\n"); } catch { }
                    _fbStatus = resp.IsSuccessStatusCode ? "Sent!" : ("Failed " + (int)resp.StatusCode + " (see ob54_feedback.txt)");
                }
                catch (Exception ex) { _fbStatus = "Error: " + ex.GetType().Name; }
            });
        }

        private void EnsureDiscordLogo()
        {
            if (_discordLogoPtr != IntPtr.Zero || _discordLogoTried) return;
            _discordLogoTried = true;
            string[] c =
            {
                Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "discord.png"),
                Path.Combine(AppContext.BaseDirectory, "Components", "discord.png"),
                Path.Combine(AppContext.BaseDirectory, "discord.png"),
            };
            foreach (var p in c)
            {
                if (!File.Exists(p)) continue;
                try
                {
                    AddOrGetImagePointer(p, false, out var ptr, out _, out _);
                    if (ptr != IntPtr.Zero) { _discordLogoPtr = ptr; return; }
                }
                catch { }
            }
        }

        private void AppendFb(string s) { _feedbackText += s; }

        private void FbBtn(string label, string insert)
        {
            if (ImGui.Button(label, new Vector2(44, 22))) AppendFb(insert);
            ImGui.SameLine(0f, 4f);
        }

        private void RenderSettingsTab()
        {
            GetSingleColumnLayout(out float colW, out float innerW);
            BeginFeatureScrollArea("settings");

            DrawCard("Panel", new Vector2(colW, 1f), () =>
            {
                DrawKeyBinder("Hide / Show Panel", ref Config.PanelToggleKey, ref _panelKeyWaiting, ref _panelKeyWaitT);
                ImGui.TextUnformatted("Current: " + Config.PanelToggleKey);
            });

            DrawCard("Stream", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Stream Mode (hide overlay from capture)", ref Config.StreamMode, null, "StreamMode");
            });

            DrawCard("Discord", new Vector2(colW, 1f), () =>
            {
                EnsureDiscordLogo();
                if (_discordLogoPtr != IntPtr.Zero)
                {
                    ImGui.Image(_discordLogoPtr, new Vector2(44, 44));
                    ImGui.SameLine();
                }
                ImGui.BeginGroup();
                if (ImGui.Button("Join Discord", new Vector2(160, 28)))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Config.DiscordInvite) { UseShellExecute = true }); } catch { }
                }
                ImGui.TextUnformatted(Config.DiscordInvite);
                ImGui.EndGroup();
                ImGui.Dummy(new Vector2(0f, 4f));
                ImGui.TextUnformatted("Feedback (screenshot + text):");
                ImGui.SetNextItemWidth(innerW);
                ImGui.InputTextMultiline("##fb_text", ref _feedbackText, 500, new Vector2(innerW, 70));
                FbBtn("B", "**"); FbBtn("I", "*"); FbBtn("U", "__"); FbBtn("S", "~~"); FbBtn("<>", "`"); FbBtn(">", "> "); FbBtn("H1", "# "); FbBtn("H2", "## ");
                ImGui.NewLine();
                FbBtn("||", "||"); FbBtn("```", "```"); FbBtn("@ev", "@everyone "); FbBtn("@hr", "@here ");
                ImGui.NewLine();
                string[] emojis = { "fire", "skull", "laugh", "cool", "heart", "star", "gg", "cry" };
                string[] chars = { "\U0001F525", "\U0001F480", "\U0001F602", "\U0001F60E", "❤️", "⭐", "🎮", "😢" };
                for (int i = 0; i < emojis.Length; i++)
                {
                    if (ImGui.Button(emojis[i], new Vector2(44, 22))) AppendFb(chars[i]);
                    if (i < emojis.Length - 1) ImGui.SameLine(0f, 4f);
                }
                ImGui.Dummy(new Vector2(0f, 2f));
                bool ev = _fbEveryone;
                if (ImGui.Checkbox("Mention @everyone", ref ev)) _fbEveryone = ev;
                ImGui.SameLine();
                bool hr = _fbHere;
                if (ImGui.Checkbox("Mention @here", ref hr)) _fbHere = hr;
                if (ImGui.Button("Send Feedback", new Vector2(innerW, 28)))
                    PostFeedbackAsync(_feedbackText, true);
                DrawKeyBinder("Publish key", ref Config.FeedbackKey, ref _fbKeyWaiting, ref _fbKeyWaitT);
                ImGui.TextUnformatted("Status: " + _fbStatus);
            });

            DrawCard("Config", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxKeybindRow(rowX, innerW, "Auto Load Config", ref AutoLoadConfig, null, "AutoLoadConfig");
                ImGui.Dummy(new Vector2(0f, 8f));
                Vector2 btnSz = new Vector2(innerW, 28f);
                if (CustomButton.CaxCustomButton("Save Config", btnSz, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.85f)))
                    SaveSingleConfigFromUi();
                ImGui.Dummy(new Vector2(0f, 3f));
                if (CustomButton.CaxCustomButton("Load Config", btnSz, new Vector4(0.36f, 0.36f, 0.38f, 1f)))
                    LoadSingleConfigFromUi();
                ImGui.Dummy(new Vector2(0f, 3f));
                if (CustomButton.CaxCustomButton("Delete Config", btnSz, new Vector4(0.65f, 0.15f, 0.15f, 0.85f)))
                    DeleteSingleConfigFromUi();
            });

            EndFeatureScrollArea();
        }

        private void DrawTabPopup()
        {
            var io = ImGui.GetIO();
            float speed = 18f;
            _tabPopupAnim += ((_showTabPopup ? 1f : 0f) - _tabPopupAnim) * io.DeltaTime * speed;
            _tabPopupAnim = Math.Clamp(_tabPopupAnim, 0f, 1f);

            if (_tabPopupAnim < 0.01f) return;

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            Vector2 popupSize = new Vector2(220, 100) * _tabPopupAnim;
            var draw = ImGui.GetWindowDrawList();
            Vector2 p = center - popupSize * 0.5f;

            draw.AddRectFilled(p, p + popupSize,
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.15f, 0.95f * _tabPopupAnim * _uiAlpha)),
                3f);
            draw.AddRect(p, p + popupSize,
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.6f * _tabPopupAnim * _uiAlpha)),
                3f);

            Vector2 textSize = ImGui.CalcTextSize("Tab Changed!");
            draw.AddText(p + (popupSize - textSize) * 0.5f,
                ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, _tabPopupAnim * _uiAlpha)),
                "Tab Changed!");

            if (_tabPopupAnim >= 0.99f)
                _showTabPopup = false;
        }

        private void EnsureTabIconsLoaded()
        {
            if (_tabIconsInitialized)
                return;
            _tabIconsInitialized = true;
            // Primary: Font Awesome glyphs via TextFont (fa-solid-900 merged). PNGs only if TextFont unavailable.
        }

        private IntPtr LoadTabIcon(string fileName)
        {
            try
            {
            string[] candidatePaths =
            {
                Path.Combine(AppContext.BaseDirectory, "Components", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Components", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName)
            };

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (!File.Exists(candidatePaths[i]))
                    continue;

                IntPtr texturePtr = IntPtr.Zero;
                uint width = 0;
                uint height = 0;

                try
                {
                    AddOrGetImagePointer(candidatePaths[i], false, out texturePtr, out width, out height);
                }
                catch { continue; }
                if (texturePtr != IntPtr.Zero)
                    return texturePtr;
            }

            return IntPtr.Zero;
            }
            catch { return IntPtr.Zero; }
        }

        private static string ResolveWav(string fileName)
        {
            string p = Path.Combine(AppContext.BaseDirectory, fileName);
            return File.Exists(p) ? p : "";
        }

        private static void PlayWav(string path)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var audioFile = new AudioFileReader(path))
                    using (var output = new WaveOutEvent())
                    {
                        output.Init(audioFile);
                        output.Play();
                        while (output.PlaybackState == PlaybackState.Playing)
                            Task.Delay(10).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Sound playback error: " + ex.Message);
                }
            });
        }

        private static void ResetLoaderVisualState()
        {
            _loaderTime = 0f;
            _connected = false;
            _connectedFade = 0f;
            _soundPlayed = false;
            _loaderAngle = 0f;
            _pulseAlpha = 0.5f;
            _pulseIncreasing = true;
        }

        private static void ResetFlowToLogin()
        {
            _flowPhase = FlowPhase.Login;
            _emulatorInitTask = null;
            _activeTab = UiMetrics.LoginTabIndex;
            _showHookSuccessScreen = false;
            _hookSuccessAnimTime = 0f;
            ResetLoaderVisualState();
        }

        private static void AdvancePostLoginIfReady()
        {
            if (_flowPhase != FlowPhase.PostLoginLoad)
                return;
            if (!_connected || _loaderTime < 2.0f)
                return;
            if (_emulatorInitTask == null)
                return;
            if (!_emulatorInitTask.IsCompleted)
                return;

            if (_emulatorInitTask.IsFaulted)
            {
                _loginError = _emulatorInitTask.Exception?.GetBaseException().Message ?? "Init failed";
                ResetFlowToLogin();
                return;
            }

            string? err = _emulatorInitTask.Result;
            if (!string.IsNullOrEmpty(err))
            {
                _loginError = err;
                ResetFlowToLogin();
                return;
            }

            _showHookSuccessScreen = true;
            _hookSuccessAnimTime = 0f;
        }

        private static void DrawEmperorPanelBackground1(Vector2 position, Vector2 size, float rounding)
        {
            var draw = ImGui.GetWindowDrawList();
            Vector2 endPos = position + size;
            draw.AddRectFilled(position, endPos,
                ImGui.GetColorU32(new Vector4(PanelBg.X, PanelBg.Y, PanelBg.Z, PanelBg.W * _uiAlpha)),
                rounding);
            // Border removed for compactness
            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(6, 6));
        }


        private static void DrawEmperorLoader()
        {
            if (_flowPhase != FlowPhase.PostLoginLoad || _showHookSuccessScreen)
                return;

            float dt = ImGui.GetIO().DeltaTime;
            _loaderAngle += dt * 2.4f; // smooth speed

            var draw = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetWindowPos();
            Vector2 size = ImGui.GetWindowSize();

            Vector2 panelCenter = new Vector2(
                pos.X + size.X * 0.5f,
                pos.Y + size.Y * 0.5f
            );

            bool done = _connected && _emulatorInitTask != null && _emulatorInitTask.IsCompleted;

            const float textGap = 26f;
            float radius = 22f;
            string text;
            Vector4 color;

            if (!_connected)
            {
                text = "Initializing...";
                color = new Vector4(1f, 1f, 1f, _uiAlpha);
            }
            else if (_emulatorInitTask != null && !_emulatorInitTask.IsCompleted)
            {
                text = "ADB / injection running…";
                color = new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, _uiAlpha);
            }
            else
            {
                text = "Connected";
                color = new Vector4(0.2f, 1f, 0.2f, _connectedFade * _uiAlpha);
            }

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            Vector2 textSize = ImGui.CalcTextSize(text);
            if (TextFont.IsLoaded()) ImGui.PopFont();

            float iconBlockH = done ? 28f : radius * 2f;
            float totalBlockH = iconBlockH + textGap + textSize.Y;
            float blockTopY = panelCenter.Y - totalBlockH * 0.5f;
            Vector2 iconCenter = new Vector2(panelCenter.X, blockTopY + iconBlockH * 0.5f);

            // =========================
            // 🌙 BOLD FAT SMOOTH SPINNER
            // =========================
            if (!done)
            {
                float thickness = 6.0f; // BOLD fat spinner

                int segments = 100; // high = smooth
                float arcLength = MathF.PI * 1.5f;

                for (int i = 0; i < segments; i++)
                {
                    float t0 = (float)i / segments;
                    float t1 = (float)(i + 1) / segments;

                    float angle0 = _loaderAngle + t0 * arcLength;
                    float angle1 = _loaderAngle + t1 * arcLength;

                    // Fade tail backward instead of forward: t0=1 is the bright head!
                    float alpha = MathF.Pow(t0, 3.0f) * 0.9f * _uiAlpha;

                    Vector2 p0 = new Vector2(
                        iconCenter.X + MathF.Cos(angle0) * radius,
                        iconCenter.Y + MathF.Sin(angle0) * radius
                    );

                    Vector2 p1 = new Vector2(
                        iconCenter.X + MathF.Cos(angle1) * radius,
                        iconCenter.Y + MathF.Sin(angle1) * radius
                    );

                    draw.AddLine(
                        p0,
                        p1,
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, alpha)),
                        thickness
                    );
                }
            }

            // =========================
            // ✅ CLEAN TICK (LIKE IMAGE)
            // =========================
            if (done)
            {
                float t = Math.Clamp(_connectedFade, 0f, 1f);
                t = 1f - MathF.Pow(1f - t, 3f); // smooth ease

                float thickness = 5.5f; // bolder stroke

                // perfect proportions longer professional checkmark
                Vector2 p0 = iconCenter + new Vector2(-12f, -1f);
                Vector2 p1 = iconCenter + new Vector2(-3f, 10f); // sharp drop down
                Vector2 p2 = iconCenter + new Vector2(17f, -14f); // extremely long sweeping tail upwards

                uint col = ImGui.GetColorU32(new Vector4(0.2f, 1f, 0.2f, t * _uiAlpha));

                // animate stroke
                float t1 = Math.Clamp(t * 1.3f, 0f, 1f);
                Vector2 a = Vector2.Lerp(p0, p1, t1);
                draw.AddLine(p0, a, col, thickness);

                if (t > 0.45f)
                {
                    float t2 = Math.Clamp((t - 0.45f) * 1.8f, 0f, 1f);
                    Vector2 b = Vector2.Lerp(p1, p2, t2);
                    draw.AddLine(p1, b, col, thickness);
                }
            }

            // =========================
            // 📝 TEXT (centered under icon as one block)
            // =========================
            Vector2 textPos = new Vector2(
                panelCenter.X - textSize.X * 0.5f,
                blockTopY + iconBlockH + textGap
            );

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            draw.AddText(textPos, ImGui.GetColorU32(color), text);
            if (TextFont.IsLoaded()) ImGui.PopFont();
        }





        private static void DrawHookSuccessScreen()
        {
            if (_flowPhase != FlowPhase.PostLoginLoad || !_showHookSuccessScreen)
                return;

            float dt = ImGui.GetIO().DeltaTime;
            _hookSuccessAnimTime += dt;
            float t = _hookSuccessAnimTime;

            float availH = ImGui.GetContentRegionAvail().Y;
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(PanelBg.X, PanelBg.Y, PanelBg.Z, 0f));
            ImGui.BeginChild("##hook_success_body", new Vector2(0, availH),
                ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground);

            var draw = ImGui.GetWindowDrawList();
            Vector2 wp = ImGui.GetWindowPos();
            Vector2 ws = ImGui.GetWindowSize();
            DrawEmperorPanelBackground1(wp, ws, 3f);

            Vector2 center = new Vector2(wp.X + ws.X * 0.5f, wp.Y + ws.Y * 0.46f);

            static float EaseOutBack(float x)
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                return 1f + c3 * MathF.Pow(x - 1f, 3f) + c1 * MathF.Pow(x - 1f, 2f);
            }

            static float EaseOutCubic(float x) => 1f - MathF.Pow(1f - x, 3f);

            // No circular background: only animated green tick.
            float tCircle = Math.Min(1f, t / 0.38f);
            float circleScale = EaseOutBack(tCircle);
            // Compact (less “stretched” ring) + slightly bolder tick.
            circleScale = Math.Min(circleScale, 1.07f);

            const float baseR = 41f;
            float ringR = baseR * circleScale;

            float tStrokeRaw = Math.Max(0f, t - 0.32f) / 0.62f;
            float strokeProg = EaseOutCubic(Math.Clamp(tStrokeRaw, 0f, 1f));
            float chkAlpha = MathF.Min(1f, (t - 0.28f) / 0.15f) * _uiAlpha;
            uint chkCol = ImGui.GetColorU32(new Vector4(0.2f, 1f, 0.2f, chkAlpha));

            float k = ringR * 0.52f;
            // Compact tick geometry (tighter angles/lengths).
            Vector2 p1 = center + new Vector2(-0.46f * k, 0.00f * k);
            Vector2 p2 = center + new Vector2(-0.06f * k, 0.36f * k);
            Vector2 p3 = center + new Vector2(0.50f * k, -0.38f * k);

            float len1 = Vector2.Distance(p1, p2);
            float len2 = Vector2.Distance(p2, p3);
            float total = len1 + len2;
            float drawLen = total * strokeProg;
            // Match spinner “bold” feel (thicker stroke).
            const float lineThick = 6.25f;

            if (drawLen > 0f && len1 > 0.001f)
            {
                float u = MathF.Min(drawLen, len1) / len1;
                Vector2 q = p1 + (p2 - p1) * u;
                draw.AddLine(p1, q, chkCol, lineThick);
            }
            if (drawLen > len1 && len2 > 0.001f)
            {
                float u = MathF.Min(drawLen - len1, len2) / len2;
                Vector2 q = p2 + (p3 - p2) * u;
                draw.AddLine(p2, q, chkCol, lineThick);
            }

            // Soft round join to avoid a “sharp corner” at p2.
            if (drawLen > len1 + 0.01f)
            {
                float joinT = Math.Clamp((drawLen - len1) / (len2 * 0.25f), 0f, 1f);
                // Slightly lower alpha = smoother join (less “blob”).
                float joinA = chkAlpha * 0.32f * joinT * _uiAlpha;
                uint joinCol = ImGui.GetColorU32(new Vector4(0.2f, 1f, 0.2f, joinA));
                float rad = lineThick * 0.60f;
                draw.AddCircleFilled(p2, rad, joinCol, 14);
            }

            float capA = strokeProg * chkAlpha * 0.45f;
            if (capA > 0.01f && drawLen > 0.01f)
            {
                uint capCol = ImGui.GetColorU32(new Vector4(0.2f, 1f, 0.2f, capA));
                float rad = lineThick * 0.5f;
                if (drawLen < len1)
                {
                    float u = MathF.Min(drawLen, len1) / len1;
                    Vector2 tip = p1 + (p2 - p1) * u;
                    draw.AddCircleFilled(tip, rad, capCol, 12);
                }
                else
                {
                    float u = MathF.Min(drawLen - len1, len2) / len2;
                    Vector2 tip = p2 + (p3 - p2) * u;
                    draw.AddCircleFilled(tip, rad, capCol, 12);
                }
            }

            float textT = Math.Clamp((t - 0.58f) / 0.42f, 0f, 1f);
            float textEase = 1f - MathF.Pow(1f - textT, 2.5f);
            float textAlpha = textEase * _uiAlpha;
            float yLift = (1f - textEase) * 18f;
            string hookMsg = "Successfully Hooked!";
            ImGui.PushFont(LebelFont.IsLoaded() ? LebelFont : TextFont);
            Vector2 msgSz = ImGui.CalcTextSize(hookMsg);
            Vector2 msgPos = new Vector2(
                center.X - msgSz.X * 0.5f,
                // Compact message placement under the tick.
                center.Y + ringR + 22f - yLift);
            for (int i = 3; i >= 1; i--)
            {
                float a = textAlpha * 0.12f / i;
                draw.AddText(
                    msgPos + new Vector2(i, i * 0.5f),
                    ImGui.GetColorU32(new Vector4(0f, 0f, 0f, a)),
                    hookMsg);
            }
            draw.AddText(msgPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, textAlpha)), hookMsg);
            ImGui.PopFont();

            ImGui.EndChild();
            ImGui.PopStyleColor();

            if (t >= HookSuccessAnimDuration)
            {
                _flowPhase = FlowPhase.Main;
                _activeTab = 0;
                _showHookSuccessScreen = false;
                _hookSuccessAnimTime = 0f;
                Config.ApplyStartupFromSingleConfigFile();
                _pendingFeatureRuntimeApply = true;
                _ = BootstrapRuntime.StartNINZAAsync();
                CustomNotification.Notify("Notification", "Success — emulator ready (port 5555)", 4.5f);
            }
        }

        private void DrawEmbeddedLoginPanel()
        {
            float contentW = ImGui.GetContentRegionAvail().X;
            float contentH = ImGui.GetContentRegionAvail().Y;
            var draw = ImGui.GetWindowDrawList();

            const float logoSize = 58f;
            const float labelH = 18f;
            const float fieldH = 32f;
            const float fieldGap = 10f;
            const float btnH = 34f;
            const float logoGap = 10f;
            const float titleGap = 6f;
            const float btnGap = 14f;
            const float statusGap = 8f;

            float formW = MathF.Min(268f, MathF.Max(200f, contentW - 32f));
            float totalH = logoSize + logoGap + labelH + titleGap + labelH + fieldH + fieldGap + labelH + fieldH + btnGap + btnH + statusGap;

            float blockTop = MathF.Max(0f, (contentH - totalH) * 0.5f);
            ImGui.SetCursorPosY(blockTop);
            ImGui.SetCursorPosX(MathF.Max(0f, (contentW - formW) * 0.5f));
            ImGui.BeginGroup();

            Vector2 blockStart = ImGui.GetCursorScreenPos();

            EnsureDragonLogoLoaded();
            IntPtr dragonLogoId = CustomNotification.DragonLogoPtr;
            if (dragonLogoId != IntPtr.Zero)
            {
                float logoX = blockStart.X + (formW - logoSize) * 0.5f;
                draw.AddImage(
                    dragonLogoId,
                    new Vector2(logoX, blockStart.Y),
                    new Vector2(logoX + logoSize, blockStart.Y + logoSize));
            }

            ImGui.Dummy(new Vector2(formW, logoSize + logoGap));

            if (LebelFont.IsLoaded()) ImGui.PushFont(LebelFont);
            Vector2 welcomeSz = ImGui.CalcTextSize("Welcome Back");
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (formW - welcomeSz.X) * 0.5f);
            ImGui.TextUnformatted("Welcome Back");
            if (LebelFont.IsLoaded()) ImGui.PopFont();

            ImGui.Dummy(new Vector2(0f, titleGap));

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            Vector2 subSz = ImGui.CalcTextSize("Sign in to continue");
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (formW - subSz.X) * 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.58f, 0.90f * _uiAlpha));
            ImGui.TextUnformatted("Sign in to continue");
            ImGui.PopStyleColor();
            if (TextFont.IsLoaded()) ImGui.PopFont();

            ImGui.Dummy(new Vector2(0f, fieldGap));

            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.02f, 0.02f, 0.04f, 0.92f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.03f, 0.03f, 0.06f, 0.96f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.04f, 0.04f, 0.08f, _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.28f * _uiAlpha));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, _uiAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 7f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);

            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            ImGui.TextUnformatted("Username");
            ImGui.SetNextItemWidth(formW);
            ImGui.InputText("##UsernameInput", ref _loginUsername, 256);
            ImGui.Dummy(new Vector2(0f, fieldGap));
            ImGui.TextUnformatted("Password");
            ImGui.SetNextItemWidth(formW);
            ImGui.InputText("##PasswordInput", ref _loginPassword, 256, ImGuiInputTextFlags.Password);
            if (TextFont.IsLoaded()) ImGui.PopFont();

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(5);

            ImGui.Dummy(new Vector2(0f, btnGap));

            Vector2 btnPos = ImGui.GetCursorScreenPos();
            Vector2 btnSize = new Vector2(formW, btnH);
            Vector2 btnMax = btnPos + btnSize;

            ImGui.SetCursorScreenPos(btnPos);
            ImGui.InvisibleButton("##login_btn_active", btnSize);
            bool btnClicked = ImGui.IsItemClicked();
            bool hovered = ImGui.IsItemHovered();
            bool pressed = ImGui.IsItemActive();

            Vector4 colorLeft = new Vector4(ThemeColor.X * 0.85f, ThemeColor.Y * 0.85f, ThemeColor.Z, 1f);
            Vector4 colorRight = ThemeColor;
            if (pressed)
            {
                colorLeft *= 0.85f;
                colorRight *= 0.85f;
            }
            else if (hovered)
            {
                colorLeft = new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 1f);
                colorRight = new Vector4(
                    MathF.Min(1f, ThemeColor.X + 0.08f),
                    MathF.Min(1f, ThemeColor.Y + 0.08f),
                    MathF.Min(1f, ThemeColor.Z + 0.08f),
                    1f);
            }

            uint btnColLeft = ImGui.GetColorU32(new Vector4(colorLeft.X, colorLeft.Y, colorLeft.Z, colorLeft.W * _uiAlpha));
            uint btnColRight = ImGui.GetColorU32(new Vector4(colorRight.X, colorRight.Y, colorRight.Z, colorRight.W * _uiAlpha));

            draw.AddRectFilledMultiColor(btnPos, btnMax, btnColLeft, btnColRight, btnColRight, btnColLeft);
            draw.AddRect(btnPos, btnMax, ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.75f * _uiAlpha)), 5f, ImDrawFlags.None, 1f);

            if (LebelFont.IsLoaded()) ImGui.PushFont(LebelFont);
            else if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            Vector2 loginSz = ImGui.CalcTextSize("Login");
            draw.AddText(
                btnPos + (btnSize - loginSz) * 0.5f,
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, _uiAlpha)),
                "Login");
            if (LebelFont.IsLoaded() || TextFont.IsLoaded()) ImGui.PopFont();

            if (btnClicked && !_loginBusy)
            {
                _loginOk = true;
                _loginDone = true;
                _loginError = "";
            }


            string statusLine = "";
            if (_loginBusy)
                statusLine = "Signing in...";
            else if (!string.IsNullOrEmpty(_loginError))
                statusLine = _loginError;
            else if (_loginDone && !_loginOk)
                statusLine = _loginResultMsg;

            if (!string.IsNullOrEmpty(statusLine))
            {
                ImGui.Dummy(new Vector2(0f, statusGap));
                if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
                Vector2 statusSz = ImGui.CalcTextSize(statusLine);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (formW - statusSz.X) * 0.5f);
                Vector4 statusCol = _loginBusy
                    ? new Vector4(0.65f, 0.65f, 0.70f, _uiAlpha)
                    : new Vector4(1f, 0.40f, 0.40f, _uiAlpha);
                ImGui.TextColored(statusCol, statusLine);
                if (TextFont.IsLoaded()) ImGui.PopFont();
            }

            ImGui.EndGroup();
        }

        public void KillProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private static byte[]? LoadFaSolidFontBytes()
        {
            if (_retainedFaSolidBytes != null)
                return _retainedFaSolidBytes;
            try
            {
                using var s = typeof(ESP).Assembly.GetManifestResourceStream("fa_solid_900.ttf");
                if (s != null)
                {
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    _retainedFaSolidBytes = ms.ToArray();
                    return _retainedFaSolidBytes;
                }
            }
            catch { }
            try
            {
                if (File.Exists(UiFonts.FaSolidPath))
                    _retainedFaSolidBytes = File.ReadAllBytes(UiFonts.FaSolidPath);
            }
            catch { }
            return _retainedFaSolidBytes;
        }

        private static string? ResolveUiBodyFontPath()
        {
            if (File.Exists(UiFonts.TahomabdPath))
                return UiFonts.TahomabdPath;

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string[] names =
            {
                "TAHOMABD.TTF", "tahoma.ttf", "TAHOMA.TTF",
                "segoeuib.ttf", "seguisb.ttf", "seguiui.ttf", "arialbd.ttf", "arial.ttf"
            };
            foreach (string name in names)
            {
                string p = Path.Combine(dir, name);
                if (File.Exists(p))
                    return p;
            }
            return null;
        }

        private static void DrawRotatedTexturedQuad(ImDrawListPtr dl, IntPtr tex, Vector2 center, Vector2 halfSize, float angleRad, uint col)
        {
            float c = MathF.Cos(angleRad);
            float s = MathF.Sin(angleRad);
            Vector2 R(Vector2 v) => new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
            Vector2 p0 = center + R(new Vector2(-halfSize.X, -halfSize.Y));
            Vector2 p1 = center + R(new Vector2(halfSize.X, -halfSize.Y));
            Vector2 p2 = center + R(new Vector2(halfSize.X, halfSize.Y));
            Vector2 p3 = center + R(new Vector2(-halfSize.X, halfSize.Y));
            dl.AddImageQuad(tex, p0, p1, p2, p3, Vector2.Zero, new Vector2(1f, 0f), Vector2.One, new Vector2(0f, 1f), col);
        }

        /// <summary>Dragon rain on a single menu section (tabs rail or content panel), above section fill, under widgets.</summary>
        private void DrawDragonParticleRainInSection(
            ImDrawListPtr dl,
            Vector2 panelOrigin,
            Vector2 panelSize,
            ref DragonRainParticle[] particles,
            ref bool inited,
            ref Vector2 lastSize,
            int count,
            float sizeMin,
            float sizeMax)
        {
            if (!Config.UiDragonParticleRain)
                return;

            if (panelSize.X < 28f || panelSize.Y < 48f || count <= 0)
                return;

            var io = ImGui.GetIO();
            float dt = Math.Clamp(io.DeltaTime, 0f, 0.05f);
            float driftT = (float)ImGui.GetTime();

            float pw = panelSize.X;
            float ph = panelSize.Y;
            float sizeDelta = Math.Abs(pw - lastSize.X) + Math.Abs(ph - lastSize.Y);
            if (sizeDelta > 8f)
                inited = false;

            if (!inited || particles.Length != count)
            {
                particles = new DragonRainParticle[count];
                float span = Math.Max(sizeMax - sizeMin, 0.5f);
                for (int i = 0; i < count; i++)
                {
                    ref DragonRainParticle p = ref particles[i];
                    p.X = (float)_hackerRandom.NextDouble() * pw;
                    p.Y = (float)(_hackerRandom.NextDouble() * (ph + ph * 0.85f)) - ph * 0.85f;
                    p.Speed = 3f + (float)_hackerRandom.NextDouble() * 5f;
                    p.Spin = (_hackerRandom.Next(2) == 0 ? 1f : -1f) * (0.12f + (float)_hackerRandom.NextDouble() * 0.22f);
                    p.Angle = (float)(_hackerRandom.NextDouble() * MathF.PI * 2f);
                    p.Size = sizeMin + (float)_hackerRandom.NextDouble() * span;
                    p.Phase = (float)(_hackerRandom.NextDouble() * MathF.PI * 2f);
                }
                inited = true;
                lastSize = new Vector2(pw, ph);
            }

            float a = 0.58f * Math.Clamp(_uiAlpha, 0f, 1f);
            uint col = ImGui.ColorConvertFloat4ToU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, a));

            dl.PushClipRect(panelOrigin, panelOrigin + panelSize, true);

            for (int i = 0; i < count; i++)
            {
                ref DragonRainParticle p = ref particles[i];
                p.Y += p.Speed * dt;
                p.Y += MathF.Sin(driftT * 0.85f + p.Phase) * 12f * dt;
                p.Angle += p.Spin * dt;
                p.X += MathF.Sin(driftT * 1.15f + p.Phase * 1.3f) * 14f * dt;
                p.X += MathF.Cos(driftT * 0.7f + p.Phase) * 6f * dt;

                float m = p.Size;
                if (p.Y > ph + m)
                {
                    p.Y = -m - (float)_hackerRandom.NextDouble() * ph * 0.45f;
                    p.X = (float)_hackerRandom.NextDouble() * pw;
                }

                if (p.X < -m)
                    p.X = pw + m * 0.25f;
                else if (p.X > pw + m)
                    p.X = -m * 0.25f;

                Vector2 center = panelOrigin + new Vector2(p.X, p.Y);
                float rad = p.Size * 0.5f;

                float c = MathF.Cos(p.Angle);
                float s = MathF.Sin(p.Angle);

                Vector2 o0 = new Vector2(0f, -rad);
                Vector2 o1 = new Vector2(-rad * 0.866f, rad * 0.5f);
                Vector2 o2 = new Vector2(rad * 0.866f, rad * 0.5f);

                Vector2 r0 = new Vector2(o0.X * c - o0.Y * s, o0.X * s + o0.Y * c);
                Vector2 r1 = new Vector2(o1.X * c - o1.Y * s, o1.X * s + o1.Y * c);
                Vector2 r2 = new Vector2(o2.X * c - o2.Y * s, o2.X * s + o2.Y * c);

                dl.AddTriangleFilled(center + r0, center + r1, center + r2, col);
            }

            dl.PopClipRect();
        }

        private void EnsureDragonLogoLoaded()
        {
            if (CustomNotification.DragonLogoPtr != IntPtr.Zero)
                return;

            // Some overlay backends only allow texture creation after the renderer/device
            // is fully initialized. Retry a few times during runtime.
            float now = (float)ImGui.GetTime();
            if (now < _dragonLogoNextTryAt)
                return;
            if (_dragonLogoLoadAttempts >= 20)
                return;
            _dragonLogoLoadAttempts++;
            _dragonLogoNextTryAt = now + 0.5f;

            const string fileName = "dragon.png";

            // 1) Prefer embedded resource (most reliable in AOT/single-file scenarios).
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream? stream = asm.GetManifestResourceStream($"pixel.Resources.Icons.{fileName}");
                if (stream == null)
                {
                    // Assembly-name agnostic suffix match.
                    string[] names = asm.GetManifestResourceNames();
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (names[i].EndsWith($"Resources.Icons.{fileName}", StringComparison.OrdinalIgnoreCase))
                        {
                            stream = asm.GetManifestResourceStream(names[i]);
                            if (stream != null)
                                break;
                        }
                    }
                }

                if (stream != null)
                {
                    using (stream)
                    using (var bitmap = new System.Drawing.Bitmap(stream))
                    {
                        // Downscale huge logos: some overlay backends choke on big textures.
                        System.Drawing.Bitmap logo = bitmap;
                        try
                        {
                            if (bitmap.Width > 512 || bitmap.Height > 512)
                            {
                                float k = 256f / Math.Max(bitmap.Width, bitmap.Height);
                                logo = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(Math.Max(1, (int)(bitmap.Width * k)), Math.Max(1, (int)(bitmap.Height * k))));
                            }
                            string tempPath = TempImages.PathFor("notif_dragon");
                            logo.Save(tempPath);
                            AddOrGetImagePointer(tempPath, true, out var ptr, out var w, out var h);
                            if (ptr != IntPtr.Zero)
                            {
                                CustomNotification.DragonLogoPtr = ptr;
                                return;
                            }
                        }
                        finally { if (!ReferenceEquals(logo, bitmap)) logo.Dispose(); }
                    }
                }
            }
            catch
            {
                // fall back to file paths
            }

            // 2) File-system fallbacks.
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Components", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Components", fileName),
                Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Icons", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
            };

            foreach (string p in candidates)
            {
                if (!File.Exists(p))
                    continue;

                try
                {
                    AddOrGetImagePointer(p, false, out var ptr, out var w, out var h);
                    if (ptr != IntPtr.Zero)
                    {
                        CustomNotification.DragonLogoPtr = ptr;
                        return;
                    }
                }
                catch { }
            }
        }

        protected override unsafe Task PostInitialized()
        {
            ReplaceFont(config =>
            {
                var io = ImGui.GetIO();
                _faMergedIntoTextFont = false;

                string? baseBodyPath = ResolveUiBodyFontPath();
                if (string.IsNullOrEmpty(baseBodyPath))
                    return;

                string impactPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "impact.ttf");
                string bodyPath = File.Exists(impactPath) ? impactPath : baseBodyPath;

                // UI body font — slightly larger for readable function labels
                const float uiBodyFontSize = 16f;
                TextFont = io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, null, io.Fonts.GetGlyphRangesDefault());

                // Merge extra Unicode blocks into TextFont so in-game names don't show as '?'
                // (each merge guarded: a single bad font must never break overlay init).
                try
                {
                unsafe
                {
                    config->MergeMode = 1;
                    config->PixelSnapH = 1;
                    io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, io.Fonts.GetGlyphRangesCyrillic());
                    io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, io.Fonts.GetGlyphRangesVietnamese());
                    io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, io.Fonts.GetGlyphRangesThai());
                    ushort[] devanagari = { 0x0900, 0x097F, 0 };
                    fixed (ushort* p = devanagari)
                        io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, (nint)p);
                    ushort[] arabic = { 0x0600, 0x06FF, 0 };
                    fixed (ushort* p = arabic)
                        io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, (nint)p);
                    ushort[] bengali = { 0x0980, 0x09FF, 0 };
                    fixed (ushort* p = bengali)
                        io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, (nint)p);
                    // Extra ranges often used in game nicknames (Tahoma/body may lack glyphs → '?' in ESP).
                    ushort[] latinExtMisc = { 0x0100, 0x024F, 0x2600, 0x26FF, 0x2700, 0x27BF, 0xFE00, 0xFE0F, 0 };
                    fixed (ushort* p = latinExtMisc)
                        io.Fonts.AddFontFromFileTTF(bodyPath, uiBodyFontSize, config, (nint)p);
                    // Segoe UI (Windows): broad fallback for symbols / CJK / extended Latin when body font lacks glyphs.
                    string segoeUi = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
                    if (File.Exists(segoeUi))
                    {
                        io.Fonts.AddFontFromFileTTF(segoeUi, uiBodyFontSize, config, io.Fonts.GetGlyphRangesDefault());
                        // Common CJK nicknames (avoid Japanese/Korean full ranges here — very large atlas).
                        io.Fonts.AddFontFromFileTTF(segoeUi, uiBodyFontSize, config, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
                    }
                    // Real fallback fonts (NOT bodyPath): body is often Impact which has no Arabic/CJK,
                    // so merging its own ranges adds nothing. Pull glyphs from fonts that have them.
                    // NOTE: kept minimal on purpose — huge ranges can break font-atlas init and kill the overlay.
                    string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                    string arial = Path.Combine(fontsDir, "arial.ttf");
                    if (File.Exists(arial))
                    {
                        ushort[] arabicCore = { 0x0600, 0x06FF, 0 };
                        fixed (ushort* p = arabicCore)
                            io.Fonts.AddFontFromFileTTF(arial, uiBodyFontSize, config, (nint)p);
                    }
                    string msyh = Path.Combine(fontsDir, "msyh.ttc");
                    if (File.Exists(msyh))
                        io.Fonts.AddFontFromFileTTF(msyh, uiBodyFontSize, config, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
                    config->MergeMode = 0;
                    config->PixelSnapH = 0;
                }
                }
                catch { }

                ushort[] faRange = { 0xF000, 0xF8FF, 0 };
                if (File.Exists(UiFonts.FaSolidPath))
                {
                    unsafe
                    {
                        fixed (ushort* gr = faRange)
                        {
                            config->MergeMode = 1;
                            config->PixelSnapH = 1;
                            io.Fonts.AddFontFromFileTTF(UiFonts.FaSolidPath, uiBodyFontSize, config, (nint)gr);
                            config->MergeMode = 0;
                            config->PixelSnapH = 0;
                        }
                    }
                    _faMergedIntoTextFont = true;
                }
                else
                {
                    byte[]? faBytes = LoadFaSolidFontBytes();
                    if (faBytes != null && faBytes.Length > 0)
                    {
                        unsafe
                        {
                            fixed (byte* fp = faBytes)
                            {
                                fixed (ushort* gr = faRange)
                                {
                                    config->MergeMode = 1;
                                    config->PixelSnapH = 1;
                                    io.Fonts.AddFontFromMemoryTTF((nint)fp, faBytes.Length, uiBodyFontSize, config, (nint)gr);
                                    config->MergeMode = 0;
                                    config->PixelSnapH = 0;
                                }
                            }
                        }
                        _faMergedIntoTextFont = true;
                    }
                }

                if (File.Exists(impactPath))
                {
                    HeaderFont = io.Fonts.AddFontFromFileTTF(impactPath, 22f, null, io.Fonts.GetGlyphRangesDefault());
                    LebelFont = io.Fonts.AddFontFromFileTTF(impactPath, 20f, null, io.Fonts.GetGlyphRangesDefault());
                }
                else
                {
                    HeaderFont = io.Fonts.AddFontFromFileTTF(baseBodyPath, 22f, null, io.Fonts.GetGlyphRangesDefault());
                    LebelFont = io.Fonts.AddFontFromFileTTF(baseBodyPath, 20f, null, io.Fonts.GetGlyphRangesDefault());
                }
                CustomNotification.TextFont = TextFont;
            });

            // Load once and reuse for every notification render.
            EnsureDragonLogoLoaded();

            return base.PostInitialized();
        }

    }


}