using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static AotForms.WinAPI;

namespace AotForms
{
    /// <summary>Hotkeys for feature toggles — active even when the menu UI is hidden.</summary>
    internal static class FeatureKeybinds
    {
        private sealed class BindEntry
        {
            public required string Id;
            public required Func<bool> Get;
            public required Action<bool> Set;
            public Action<bool>? OnChanged;
        }

        private static readonly Dictionary<string, Keys> _keys = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, bool> _keyHeld = new(StringComparer.Ordinal);
        private static readonly List<BindEntry> _entries = new();
        private static bool _initialized;

        internal static Keys GetKey(string id)
        {
            EnsureInitialized();
            return _keys.TryGetValue(id, out Keys k) ? k : Keys.None;
        }

        internal static void SetKey(string id, Keys key)
        {
            EnsureInitialized();
            _keys[id] = key;
        }

        internal static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterAll();
        }

        private static void Register(string id, Func<bool> get, Action<bool> set, Action<bool>? onChanged = null)
        {
            _entries.Add(new BindEntry { Id = id, Get = get, Set = set, OnChanged = onChanged });
            if (!_keys.ContainsKey(id))
                _keys[id] = Keys.None;
        }

        private static void RegisterAll()
        {
            Register("enableAimBot", () => Config.enableAimBot, v => Config.enableAimBot = v);
            Register("AimBotRage", () => Config.AimBotRage, v => Config.AimBotRage = v);
            Register("kcsafeSilentAim", () => Config.kcsafeSilentAim, v => Config.kcsafeSilentAim = v);
            Register("kcbrutasilnetaim", () => Config.kcbrutasilnetaim, v => Config.kcbrutasilnetaim = v);
            Register("AimbotVisible", () => Config.AimbotVisible, v => { Config.AimbotVisible = v; Config.AimbotRubix = v; });
            Register("RefreshESP", () => Config.RefreshESP, v => Config.RefreshESP = v);
            Register("UnlimitedAmmo", () => Config.UnlimitedAmmo, v => Config.UnlimitedAmmo = v);
            Register("RapidFire", () => Config.RapidFire, v => Config.RapidFire = v);
            Register("AutoFire", () => Config.AutoFire, v => Config.AutoFire = v);
            Register("NoRecoil", () => Config.NoRecoil, v => Config.NoRecoil = v);
            Register("FastReload", () => Config.FastReload, v => Config.FastReload = v);
            Register("UpPlayer", () => Config.UpPlayer, v => Config.UpPlayer = v);
            Register("forceaim", () => Config.forceaim, v => Config.forceaim = v);
            Register("IgnoreKnocked", () => Config.IgnoreKnocked, v => Config.IgnoreKnocked = v);
            Register("FOVEnabled", () => Config.FOVEnabled, v => Config.FOVEnabled = v);
            Register("EnemyPullEnabled", () => Config.EnemyPullEnabled, v => Config.EnemyPullEnabled = v);
            Register("EnemyPullX", () => Config.EnemyPullX, v => Config.EnemyPullX = v);
            Register("EnemyPullXY", () => Config.EnemyPullXY, v => Config.EnemyPullXY = v);
            Register("spinbot", () => Config.spinbot, v => Config.spinbot = v, ApplySpinBotFromBind);

            Register("FLYHACKINTTTT", () => Config.FLYHACKINTTTT, v => { Config.FLYHACKINTTTT = v; Config.FLYHACKINTTT = v; });
            Register("FlyHack80xx", () => Config.FlyHack80xx, v => Config.FlyHack80xx = v);
            Register("FLYHACKX400", () => Config.FLYHACKX400, v => { Config.FLYHACKX400 = v; Config.FLYHACKX40 = v; });
            Register("FlyHack50x", () => Config.FlyHack50x, v => { Config.FlyHack50x = v; Config.FlyHack50xx = v; });
            Register("FlyHack", () => Config.FlyHack, v => Config.FlyHack = v, v => KCNOGRAVITYFLY.SetState(v));
            Register("ClimbUpEnabled", () => Config.ClimbUpEnabled, v => Config.ClimbUpEnabled = v);
            Register("ClimbUpV2Enabled", () => Config.ClimbUpV2Enabled, v => Config.ClimbUpV2Enabled = v);
            Register("speedint", () => Config.speedint, v => Config.speedint = v);
            Register("tele", () => Config.tele, v => Config.tele = v);
            Register("ShakeKill", () => Config.ShakeKill, v => Config.ShakeKill = v);
            Register("DownPlayer", () => Config.DownPlayer, v => Config.DownPlayer = v);
            Register("undergroundkill", () => Config.undergroundkill, v => Config.undergroundkill = v);
            Register("Kcspawnkill", () => Config.Kcspawnkill, v => Config.Kcspawnkill = v);
            Register("teliport", () => Config.teliport, v => Config.teliport = v);
            Register("teliportwall", () => Config.teliportwall, v => Config.teliportwall = v);
            Register("teleportmap", () => Config.teleportmap, v => Config.teleportmap = v);

            Register("AutoLoadConfig", () => Config.AutoLoadConfig, v => Config.AutoLoadConfig = v);
            Register("UiDragonParticleRain", () => Config.UiDragonParticleRain, v => Config.UiDragonParticleRain = v);
            Register("StreamMode", () => Config.StreamMode, v => Config.StreamMode = v);
        }

        private static void ApplySpinBotFromBind(bool enabled)
        {
            if (enabled) RapidSpin.Activate();
            else RapidSpin.Deactivate();
        }

        /// <summary>Call every frame from overlay render — works when UI is minimized.</summary>
        internal static void ProcessHotkeys(string? activeBindingId)
        {
            EnsureInitialized();

            foreach (var entry in _entries)
            {
                if (activeBindingId == entry.Id)
                    continue;

                Keys key = GetKey(entry.Id);
                if (key == Keys.None || key == Keys.Insert)
                    continue;

                bool down = (GetAsyncKeyState(key) & 0x8000) != 0;
                bool wasDown = _keyHeld.TryGetValue(entry.Id, out bool w) && w;

                if (down && !wasDown)
                {
                    bool next = !entry.Get();
                    entry.Set(next);
                    try { entry.OnChanged?.Invoke(next); } catch { }
                    Components.CustomNotification.Notify(entry.Id, next ? "Activated" : "De-Activated", 2f);
                }

                _keyHeld[entry.Id] = down;
            }
        }

        internal static void ResetHeldState() => _keyHeld.Clear();
    }
}
