using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AotForms
{
    internal static class Config
    {
        internal static bool VisualsEnabled = false;

        internal static bool speed = false;
        internal static bool rgb = false;
        internal static float iconsize = 1.2f;
        public static Vector4 ICONCOLOR = new Vector4(1f, 1f, 1f, 1f);
        internal static bool espweapon = false;  
        internal static float AimBotSmooth = 0f;
        internal static AimBotType AimBotType;
        internal static bool enableAimBot = false;
        internal static bool AimLock = false;
        internal static bool TriggerBot = false;
        internal static bool DrawClosest = false;
        internal static bool AutoLoadConfig = false;
        internal static bool AimBotRage = false;
        internal static bool SilentAim = false;
        internal static bool AimbotRubix = false;
        internal static bool AimbotVisible = false;
        internal static bool RefreshESP = false;
        internal static bool kcsafeSilentAim = false;
        internal static bool kcbrutasilnetaim = false;
        internal static bool RapidFire = false;
        internal static float RapidFireSpeed = 0.5f;
        internal static bool AutoFire = false;
        internal static float AutoFireMaxDistance = 50f;
        internal static bool KXCShurikenMultiDamage = false;
        internal static bool KXCFakeDame = false;
        internal static bool SilentWeaponOnly = false;
        internal static string SilentWeaponKey = "AK47";
        internal static System.Windows.Forms.Keys PanelToggleKey = System.Windows.Forms.Keys.Insert;
        internal static System.Windows.Forms.Keys FeedbackKey = System.Windows.Forms.Keys.F9;
        // WARNING: this bot token was shared publicly — regenerate it in the
        // Discord Developer Portal and replace it here, or anyone can control your bot.
        internal static string DiscordBotToken = "MTUyMDE0OTc4MDc4MzgyOTEyNQ.GoluX8.lbdu72Doqx3YaLsbDP302RgXVtZGCECik1DtQY";
        internal static string DiscordChannelId = "1516545206483419316";
        internal static string DiscordInvite = "https://discord.gg/xEgpAVJSVe";
        internal static float HeadRate = 100f;
        internal static float ChestRate = 0f;
        internal static bool FpsBoost = false;
        internal static bool FastReload = false;
        internal static bool UpPlayer = false;
        internal static bool tele = false;
        internal static bool ShakeKill = false;
        internal static bool UnlimitedAmmo = false;
        internal static bool teliportwall = false;
        internal static bool alignment = true;


        internal static Keys AimbotKey = Keys.LButton; 

        internal static int AimBotMaxDistance = 200;

        internal static bool IgnoreKnocked = false;
        internal static bool UpdateEntities = false;
        internal static bool NoRecoil = false;
        internal static bool NoCache = false;
        internal static int AimbotSmoothness = 0;
        internal static int AimBotFov = 100000;
        internal static bool speedint = false;

        internal static TargetingMode TargetingMode  = TargetingMode.ClosestToCrosshair;

        internal static bool ESPLine = false;
        internal static bool EspShowDragonLogo = false;
        internal static Color ESPLineColor = Color.White;
        internal static float AimFov = 200f;
        internal static int AimFovCircle = 500;
        internal static float Esprender = 200f;
        /// <summary>Max distance (m) for ESP match timer enemy detection (reference: espran).</summary>
        internal static float espran = 200f;
        internal static bool StreamMode = false;
        /// <summary>Fullscreen slow-falling rotating dragon icons (decorative UI particles).</summary>
        internal static bool UiDragonParticleRain = true;

        internal static Color NameNINZA = Color.Red;
        internal static bool ESPBox = false;
        internal static float speedVal = 5.0f;
        internal static bool Speed = false;

        internal static Color ESPBoxColor = Color.White;
        internal static bool teliport = false;
        internal static bool Kcspawnkill = false;
        internal static bool ClimbUpEnabled = false;
        internal static bool ClimbUpV2Enabled = false;
        internal static float ClimbUpV2Value = 12f;
        internal static bool DownPlayer = false;
        internal static bool FlyHack50x = false;
        internal static bool FlyHack50xx = false;
        internal static bool FlyHack80x = false;
        internal static bool FlyHack80xx = false;
        internal static bool FLYHACKINTTTT = false;
        internal static bool FLYHACKINTTT = false;
        internal static bool FLYHACKX400 = false;
        internal static bool FLYHACKX40 = false;
        internal static bool TeleportAnywhere = false;
        internal static bool directteleport = false;
        internal static bool EnemyPullEnabled = false;
        internal static bool EnemyPullEnabledv2 = false;
        internal static bool EnemyPullX = false;
        internal static bool EnemyPullXY = false;
        internal static bool DebugLogging = false;
        internal static bool forceaim = false;
        internal static bool undergroundkill = false;
        internal static bool MAGNETPULL = false;
        internal static bool FlyHack = false;
        internal static bool spinbot = false;
        internal static float SpinBotSpeed = 4.2f;
        internal static bool teleportmap = false;

        internal static bool enableAndroidNINZA = false;
        internal static bool androidSilentKill = false;
        internal static bool androidAutoFire = false;
        internal static bool androidAimkillSend = false;
        internal static bool androidAutoKill = false;
        internal static bool androidSpeedRun = false;
        internal static bool androidFlyHack = false;
        internal static float androidFov = 5000f;
        internal static bool IgnoreBots = false;
        internal static bool ShowTeammates = false;
        internal static bool MuteSound = false;

        internal static int EnemyPullTickMs = 6;
        internal static float EnemyPullMaxDistance = 150f;
        internal static float recoilcontrol = 10f;
        internal static float UpPlayerSpeed = 20f;
        internal static float speedTP = 10f;
        internal static bool ESPInformation = false;
        internal static bool espdistance = false;
        internal static bool ESPName = false;
        internal static Color ESPNameColor = Color.White;
        internal static bool ESPHealth = false;
        internal static bool ESPHealthText = false;
        internal static Color ESPHealthColor = Color.Green;
        internal static bool ESPFillBox = false;
        internal static Color ESPFillBoxColor = Color.FromArgb(80, 255, 0, 0);
        internal static bool ESPFullBox = false;
        internal static float ESPThickness = 1.5f;
        internal static bool ESPWeaponIcon = true;
        internal static bool ESPWeaponName = true;
        internal static bool LootWeaponsESP = false;
        internal static bool LootShowIcon = true;
        internal static bool LootShowName = true;
        internal static bool LootShowDist = true;
        internal static bool LootShowLine = true;
        internal static bool LootShowBox = false;
        internal static int ESPLineStyle = 0;
        internal static int ESPLineEnd = 0;
        internal static int ESPBoxStyle = 0;
        internal static float ESPNameSize = 1.0f;
        internal static float ESPWeaponIconSize = 1.0f;
        internal static bool ESPRadar = false;
        internal static float ESPRadarSize = 150f;
        internal static float ESPRadarRange = 200f;
        internal static float RadarPosX = 0f;
        internal static float RadarPosY = 0f;
        internal static bool ESPBunnyEars = false;
        internal static bool ESPSkeleton = false;  
        public static bool EspScreenSmooth = true;
        internal static Color ESPSkeletonColor = Color.White;
        internal static bool ESPRGB = false;
        internal static bool AimTrackLine = false;
        internal static Color AimTrackLineColor = Color.Red;
        internal static bool HackerTag = false;
        internal static Color HackerTagColor = Color.Red;
        internal static bool EspTimer = false;
        internal static bool ESPEditor = false;
        internal static bool EnemyTimerActive = false;
        internal static System.DateTime EnemyTimerStart = System.DateTime.MinValue;
        internal static System.DateTime LastEnemySeen = System.DateTime.MinValue;
        /// <summary>ESP valid-match countdown length in seconds (Silent Max = 180).</summary>
        internal const int EspMatchTimerDurationSec = 180;
        internal static int EnemyTimerDuration = EspMatchTimerDurationSec;
        internal static int ResetAfterNoEnemy = 40;
        public static string InvalidText = "Game invalid! Minimum time not completed.";
        public static string ValidText = "Game completed in: {0}";
        internal static bool FOVEnabled = false;

        public static bool SilentKillEnabled = false;    // Enable/disable the main silent kill feature
        public static bool SilentKillDragEnabled = false; // Enable/disable the optional drag teleport


        internal static Color FOVColor = Color.White;
        internal static LinePosition ESPLinePosition = LinePosition.Top;

        internal static Vector2 EspWeaponIconOffsetPx = Vector2.Zero;
        internal static Vector2 EspNameOffsetPx = Vector2.Zero;
        internal static Vector2 EspHackerOffsetPx = Vector2.Zero;
        internal static Vector2 EspBoxOffsetPx = Vector2.Zero;
        internal static Vector2 EspHealthBarOffsetPx = Vector2.Zero;
        internal static Vector2 EspSnapLineStartOffsetPx = Vector2.Zero;
        internal static Vector2 EspSnapLineEndOffsetPx = Vector2.Zero;
        /// <summary>Fine offset for distance text when it is drawn outside the nameplate (editor + in-game).</summary>
        internal static Vector2 EspDistanceOffsetPx = Vector2.Zero;

        internal static EspNameBoxAnchor EspNameAnchor = EspNameBoxAnchor.BoxTopCenter;
        internal static EspDistanceBoxAnchor EspDistanceAnchor = EspDistanceBoxAnchor.BoxMiddleLeft;
        internal static EspHealthBoxAnchor EspHealthAnchor = EspHealthBoxAnchor.BoxRight;
        internal static EspWeaponBoxAnchor EspWeaponAnchor = EspWeaponBoxAnchor.BoxBottomCenter;

        internal static float cameraVal = 1.0f;
        internal static float visionVal = 3.141592741f;

        private const string ProfileFileExt = ".profile.cfg";

        /// <summary>Fixed slot — one save file only (no named profiles).</summary>
        internal const string SingleConfigSlotName = "config";

        internal static string ProfilesDirectory
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dir = Path.Combine(baseDir, "Profiles");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        internal static string SingleConfigFilePath => GetProfilePath(SingleConfigSlotName);

        internal static bool SaveSingleConfig(out string error) => SaveProfile(SingleConfigSlotName, out error);

        internal static bool LoadSingleConfig(out string error) => LoadProfile(SingleConfigSlotName, out error);

        internal static bool DeleteSingleConfig(out string error) => DeleteProfile(SingleConfigSlotName, out error);

        internal static bool SingleConfigExists() => File.Exists(SingleConfigFilePath);

        /// <summary>Reads <see cref="AutoLoadConfig"/> from disk, then loads full config if it is true.</summary>
        internal static void ApplyStartupFromSingleConfigFile()
        {
            if (!File.Exists(SingleConfigFilePath))
                return;
            try
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in File.ReadAllLines(SingleConfigFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    int sep = line.IndexOf('=');
                    if (sep <= 0)
                        continue;
                    map[line[..sep].Trim()] = line[(sep + 1)..].Trim();
                }
                if (map.TryGetValue(nameof(AutoLoadConfig), out string? raw) &&
                    TryDeserializeValue(raw, typeof(bool), out object? v) && v is bool b)
                    AutoLoadConfig = b;
                if (AutoLoadConfig)
                    LoadSingleConfig(out _);
            }
            catch
            {
                /* ignore malformed startup file */
            }
        }

        private static bool SaveProfile(string profileName, out string error)
        {
            error = "";
            string normalized = NormalizeProfileName(profileName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Profile name is empty.";
                return false;
            }

            try
            {
                string path = GetProfilePath(normalized);
                var lines = new List<string>();
                foreach (var f in GetPersistedFields())
                {
                    object? value = f.GetValue(null);
                    lines.Add($"{f.Name}={SerializeValue(value, f.FieldType)}");
                }
                File.WriteAllLines(path, lines);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool LoadProfile(string profileName, out string error)
        {
            error = "";
            string normalized = NormalizeProfileName(profileName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Profile name is empty.";
                return false;
            }

            string path = GetProfilePath(normalized);
            if (!File.Exists(path))
            {
                error = "Profile file not found.";
                return false;
            }

            try
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    int sep = line.IndexOf('=');
                    if (sep <= 0)
                        continue;
                    string key = line[..sep].Trim();
                    string value = line[(sep + 1)..].Trim();
                    map[key] = value;
                }

                foreach (var f in GetPersistedFields())
                {
                    if (!map.TryGetValue(f.Name, out string? raw))
                        continue;
                    if (TryDeserializeValue(raw, f.FieldType, out object? parsed))
                        f.SetValue(null, parsed);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool DeleteProfile(string profileName, out string error)
        {
            error = "";
            string normalized = NormalizeProfileName(profileName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Profile name is empty.";
                return false;
            }

            string path = GetProfilePath(normalized);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static IEnumerable<FieldInfo> GetPersistedFields()
        {
            return typeof(Config)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsLiteral && !f.IsInitOnly);
        }

        private static string GetProfilePath(string normalizedName)
        {
            return Path.Combine(ProfilesDirectory, normalizedName + ProfileFileExt);
        }

        private static string NormalizeProfileName(string profileName)
        {
            string n = profileName?.Trim() ?? "";
            if (n.Length == 0)
                return "";
            foreach (char c in Path.GetInvalidFileNameChars())
                n = n.Replace(c.ToString(), "");
            return n.Trim();
        }

        private static string SerializeValue(object? value, Type t)
        {
            if (value == null)
                return "";
            if (t == typeof(bool))
                return (bool)value ? "true" : "false";
            if (t == typeof(int))
                return ((int)value).ToString(CultureInfo.InvariantCulture);
            if (t == typeof(float))
                return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            if (t.IsEnum)
                return value.ToString() ?? "";
            if (t == typeof(Color))
            {
                Color c = (Color)value;
                return $"{c.A},{c.R},{c.G},{c.B}";
            }
            if (t == typeof(Vector2))
            {
                var v = (Vector2)value;
                return $"{v.X.ToString("R", CultureInfo.InvariantCulture)},{v.Y.ToString("R", CultureInfo.InvariantCulture)}";
            }
            if (t == typeof(Vector4))
            {
                var v = (Vector4)value;
                return string.Join(",",
                    v.X.ToString("R", CultureInfo.InvariantCulture),
                    v.Y.ToString("R", CultureInfo.InvariantCulture),
                    v.Z.ToString("R", CultureInfo.InvariantCulture),
                    v.W.ToString("R", CultureInfo.InvariantCulture));
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static bool TryDeserializeValue(string raw, Type t, out object? value)
        {
            value = null;
            if (t == typeof(bool))
            {
                if (bool.TryParse(raw, out bool b)) { value = b; return true; }
                return false;
            }
            if (t == typeof(int))
            {
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) { value = i; return true; }
                return false;
            }
            if (t == typeof(float))
            {
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) { value = f; return true; }
                return false;
            }
            if (t.IsEnum)
            {
                try { value = Enum.Parse(t, raw, true); return true; } catch { return false; }
            }
            if (t == typeof(Color))
            {
                string[] p = raw.Split(',');
                if (p.Length == 4 &&
                    byte.TryParse(p[0], out byte a) &&
                    byte.TryParse(p[1], out byte r) &&
                    byte.TryParse(p[2], out byte g) &&
                    byte.TryParse(p[3], out byte b))
                {
                    value = Color.FromArgb(a, r, g, b);
                    return true;
                }
                return false;
            }
            if (t == typeof(Vector2))
            {
                string[] p = raw.Split(',');
                if (p.Length == 2 &&
                    float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    value = new Vector2(x, y);
                    return true;
                }
                return false;
            }
            if (t == typeof(Vector4))
            {
                string[] p = raw.Split(',');
                if (p.Length == 4 &&
                    float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z) &&
                    float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
                {
                    value = new Vector4(x, y, z, w);
                    return true;
                }
                return false;
            }
            return false;
        }
    }
    public enum TargetingMode
    {
        ClosestToCrosshair,
        Target360,
        ClosestToPlayer,
        LowestHealth,
        FarthestFromCrosshair,
    }
    public enum AimBotType
    {
        Silent,
        Rage
    }
    public enum LinePosition
    {
        Top,
        Center,
        Bottom
    }

    /// <summary>Where ESP name / nameplate sits relative to the corner ESP box.</summary>
    public enum EspNameBoxAnchor
    {
        BoxTopCenter,
        BoxBottomCenter,
        BoxTopLeft,
        BoxTopRight,
        BoxBottomLeft,
        BoxBottomRight,
        BoxMiddleLeft,
        BoxMiddleRight,
    }

    /// <summary>Where distance text is drawn (ESP Information). InsideNameplate = original bar layout.</summary>
    public enum EspDistanceBoxAnchor
    {
        InsideNameplate,
        AboveNameWhenNameAtTop,
        BoxTopCenter,
        BoxBottomCenter,
        BoxTopLeft,
        BoxTopRight,
        BoxBottomLeft,
        BoxBottomRight,
        BoxMiddleLeft,
        BoxMiddleRight,
    }

    public enum EspHealthBoxAnchor
    {
        BoxRight,
        BoxLeft,
        BoxTop,
        BoxBottom,
    }

    public enum EspWeaponBoxAnchor
    {
        BoxTopCenter,
        BoxBottomCenter,
        BoxTopLeft,
        BoxTopRight,
        BoxBottomLeft,
        BoxBottomRight,
        BoxMiddleLeft,
        BoxMiddleRight,
    }
   
}
