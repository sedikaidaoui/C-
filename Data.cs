using System.Numerics;

namespace AotForms
{
    internal static class Data
    {
        private static bool lastRapidFireState = false;
        private static bool lastAutoFireState = false;
        private static bool lastFastReloadState = false;
        private static Dictionary<ulong, float> originalFireIntervalValues = new Dictionary<ulong, float>();
        private static int consecFails = 0;
        private static readonly HashSet<string> loggedDiag = new();
        private static int diagLines;
        private static readonly HashSet<int> loggedWeaponIds = new();
        internal static void LogDiag(string reason, Entity e)
        {
            if (!Config.DebugLogging) return;
            try
            {
                if (diagLines >= 300) return;
                string key = reason + ":" + e.Address;
                lock (loggedDiag)
                {
                    if (!loggedDiag.Add(key)) return;
                    diagLines++;
                }
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_espdiag.txt"),
                    DateTime.Now.ToString("HH:mm:ss") + " " + reason + " addr=" + e.Address + " known=" + e.IsKnown + " team=" + e.IsTeam + " dead=" + e.IsDead + " knocked=" + e.IsKnocked + " name=[" + (e.Name ?? "<null>") + "] weapon=[" + (e.WeaponName ?? "<null>") + "]\r\n");
            }
            catch { }
        }
        private static readonly HashSet<string> loggedNames = new();
        private static int lastLocalWeaponId = -1;
        internal static int LastLocalWeaponId => lastLocalWeaponId;
        // Logs every distinct weapon ID seen (pool + fallback) so unknown IDs can be mapped exactly.
        static void LogWeaponId(int id, string name)
        {
            if (!Config.DebugLogging) return;
            try
            {
                if (loggedWeaponIds.Add(id))
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_weapon_ids.txt"), DateTime.Now.ToString("HH:mm:ss") + " ID " + id + " = " + name + "\r\n");
            }
            catch { }
        }
        // Local held-weapon English name (for the silent single-weapon filter).
        internal static string GetLocalWeaponName()
        {
            try
            {
                if (Core.LocalPlayer == 0) return "";
                if (InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.Player_Data, out var dp) && dp != 0
                    && InternalMemory.Read<uint>(dp + 0x8, out var po) && po != 0
                    && InternalMemory.Read<uint>(po + 0x20, out var pl) && pl != 0
                    && InternalMemory.Read<short>(pl + 0x10, out var wid) && wid != 0)
                    return WeaponIndex.GetWeaponName(wid);
            }
            catch { }
            return "";
        }

        private static DateTime lastLootScan = DateTime.MinValue;
        private static readonly Dictionary<uint, (int id, Vector3 pos, int hits)> lootCand = new();
        // Slow (500ms) loot scan: entity-dict unknowns + object-manager dicts.
        // A candidate is listed ONLY after 3 stable observations (same id, static pos):
        // counters and moving players can never pass. Fully read-only.
        static void LootScan(uint currentGame, uint currentMatch, List<uint> dictEntities)
        {
            try
            {
                foreach (var entity in dictEntities)
                {
                    if (entity == 0 || entity == (uint)Core.LocalPlayer) continue;
                    if (Core.Entities.TryGetValue(entity, out var pl) && pl != null && (pl.IsKnown || pl.IsTeam == Bool3.True)) continue;
                    ProbePlayerShaped(entity);
                }
                // Object managers: try dict-shape AND Il2Cpp-List-shape walks on both
                // game bases. Every candidate still faces transform + known-ID + stability
                // validation, so wrong guesses simply yield nothing (read-only, safe).
                // Object managers: try dict-shape AND Il2Cpp-List-shape walks on both
                // game bases. Every candidate still faces transform + known-ID + stability
                // validation, so wrong guesses simply yield nothing (read-only, safe).
                uint[] bases = { currentGame, currentMatch };
                foreach (var b in bases)
                {
                    if (b == 0) continue;
                    foreach (var mgrOff in new uint[] { Offsets.LevelObjectManager, Offsets.LevelObjectManagerAlt })
                    {
                        if (mgrOff == 0) continue;
                        if (!InternalMemory.Read<uint>(b + mgrOff, out var mgr) || mgr == 0) continue;
                        WalkDictShaped(mgr);
                        WalkListShaped(mgr);
                    }
                }
                var now = DateTime.Now;
                foreach (var k in Core.LootItems.Keys)
                {
                    if (Core.LootItems.TryGetValue(k, out var li) && li != null && (now - li.Seen).TotalSeconds < 6) continue;
                    Core.LootItems.TryRemove(k, out _);
                }
            }
            catch { }
        }

        static void WalkDictShaped(uint mgr)
        {
            try
            {
                if (!InternalMemory.Read<int>(mgr + 0x10, out var count) || count < 1 || count > 2000) return;
                if (!InternalMemory.Read<uint>(mgr + 0xC, out var entries) || entries == 0) return;
                uint start = entries + 0x10;
                int scanned = 0;
                for (uint i = 0; i < (uint)count && scanned < 200; i++)
                {
                    uint e = start + (i * 0x10);
                    if (!InternalMemory.Read<int>(e + 0x0, out int hash) || hash < 0) continue;
                    if (!InternalMemory.Read<uint>(e + 0xC, out uint obj) || obj == 0) continue;
                    scanned++;
                    ProbeObjectShaped(obj);
                }
            }
            catch { }
        }

        static void WalkListShaped(uint mgr)
        {
            try
            {
                // Il2Cpp List<T> (32-bit): _items @ +0x8, _size @ +0xC;
                // array: length @ +0xC, elements @ +0x10.
                if (!InternalMemory.Read<uint>(mgr + 0x8, out var arr) || arr == 0) return;
                if (!InternalMemory.Read<int>(mgr + 0xC, out var size) || size < 1 || size > 2000) return;
                if (!InternalMemory.Read<int>(arr + 0xC, out var len) || len < size || len > 5000) return;
                for (uint i = 0; i < (uint)size && i < 200; i++)
                {
                    if (!InternalMemory.Read<uint>(arr + 0x10 + i * 4, out var obj) || obj == 0) continue;
                    ProbeObjectShaped(obj);
                }
            }
            catch { }
        }

        static bool LootHasPlayerMarkers(uint entity)
        {
            if (InternalMemory.Read<uint>(entity + Offsets.AvatarManager, out var am) && am != 0) return true;
            if (InternalMemory.Read<uint>(entity + Offsets.Player_ShadowBase, out var sh) && sh != 0) return true;
            if (InternalMemory.Read<uint>(entity + Offsets.Player_Data, out var hdp) && hdp != 0
                && InternalMemory.Read<uint>(hdp + 0x8, out var hpo) && hpo != 0
                && InternalMemory.Read<uint>(hpo + 0x10, out var hp) && hp != 0) return true;
            return false;
        }

        static bool TryPlayerWeaponId(uint entity, out int wid)
        {
            wid = 0;
            if (!InternalMemory.Read<uint>(entity + Offsets.Player_Data, out var dp) || dp == 0) return false;
            if (!InternalMemory.Read<uint>(dp + 0x8, out var po) || po == 0) return false;
            if (!InternalMemory.Read<uint>(po + 0x20, out var pl) || pl == 0) return false;
            if (!InternalMemory.Read<short>(pl + 0x10, out var id) || id <= 0) return false;
            wid = id;
            return true;
        }

        static void ObserveLootCandidate(uint addr, Vector3 pos, int wid)
        {
            if (!float.IsFinite(pos.X + pos.Y + pos.Z)) return;
            if (Vector3.Distance(Core.LocalMainCamera, pos) > 1000f) return;
            var nm = WeaponIndex.GetWeaponName(wid);
            if (nm.StartsWith("Unknown")) return;
            if (!lootCand.TryGetValue(addr, out var c) || c.id != wid || Vector3.Distance(c.pos, pos) > 1.0f)
            {
                lootCand[addr] = (wid, pos, 1);
                return;
            }
            c.hits++;
            lootCand[addr] = c;
            if (c.hits >= 3)
                Core.LootItems[addr] = new LootItem { Address = addr, Position = pos, WeaponId = wid, Name = nm, Seen = DateTime.Now };
        }

        static void ProbePlayerShaped(uint entity)
        {
            if (LootHasPlayerMarkers(entity)) return;
            if (!InternalMemory.Read<uint>(entity + (uint)Bones.Root, out var rb) || rb == 0) return;
            if (!Transform.GetNodePosition(rb, out var pos)) return;
            if (!TryPlayerWeaponId(entity, out var wid)) return;
            ObserveLootCandidate(entity, pos, wid);
        }

        static void ProbeObjectShaped(uint obj)
        {
            if (LootHasPlayerMarkers(obj)) return;
            Vector3 pos = Vector3.Zero;
            bool havePos = false;
            if (InternalMemory.Read<uint>(obj + (uint)Bones.Root, out var rb) && rb != 0
                && Transform.GetNodePosition(rb, out pos))
                havePos = true;
            if (!havePos)
            {
                foreach (var to in new uint[] { 0x8, 0x10, 0x18, 0x20 })
                {
                    if (InternalMemory.Read<uint>(obj + to, out var tr) && tr != 0
                        && Transform.GetPosition(tr, out pos)) { havePos = true; break; }
                }
            }
            if (!havePos) return;
            if (TryPlayerWeaponId(obj, out var wid)) { ObserveLootCandidate(obj, pos, wid); return; }
            foreach (var io in new uint[] { 0x10, 0x14, 0x18, 0x1C, 0x20, 0x24, 0x28, 0x2C, 0x30 })
            {
                if (InternalMemory.Read<int>(obj + io, out var id) && id > 0 && id < 30000)
                {
                    var nm = WeaponIndex.GetWeaponName(id);
                    if (!nm.StartsWith("Unknown")) { ObserveLootCandidate(obj, pos, id); return; }
                }
            }
        }
        // Logs YOUR held weapon on change: pick guns up one by one and the ID sequence maps them exactly.
        static void LogLocalWeapon(uint localPlayer)
        {
            try
            {
                int id = -1;
                if (InternalMemory.Read<uint>(localPlayer + Offsets.Player_Data, out var dp) && dp != 0
                    && InternalMemory.Read<uint>(dp + 0x8, out var po) && po != 0
                    && InternalMemory.Read<uint>(po + 0x20, out var pl) && pl != 0
                    && InternalMemory.Read<short>(pl + 0x10, out var wid) && wid != 0)
                    id = wid;
                if (id != lastLocalWeaponId)
                {
                    lastLocalWeaponId = id;
                    if (id > 0)
                        File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_weapon_ids.txt"), DateTime.Now.ToString("HH:mm:ss") + " LOCAL " + id + " = " + WeaponIndex.GetWeaponName(id) + "\r\n");
                }
            }
            catch { }
        }
        // Logs every distinct player name AS READ (diagnoses Arabic/Unicode reads).
        static void LogName(string name)
        {
            if (!Config.DebugLogging) return;
            try
            {
                if (loggedNames.Add(name))
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_names.txt"), name + "\r\n");
            }
            catch { }
        }
        // Transient read failure: keep last ESP frame. After ~1.5s of continuous failure
        // refresh ONLY the address translations (emulator GC invalidates them); entities
        // expire individually by age, so ESP never blinks out wholesale anymore.
        static bool ChainFail()
        {
            if (++consecFails < 60)
            {
                Thread.Sleep(25);
                return true;
            }
            consecFails = 0;
            try { InternalMemory.Cache.Clear(); } catch { }
            Core.HaveMatrix = false;
            Thread.Sleep(25);
            return true;
        }
        internal static void Work()
        {
            while (true)
            {
                try
                {

                    var rBaseGameFacade = InternalMemory.Read<uint>(Offsets.Il2Cpp + Offsets.InitBase, out var baseGameFacade);
                    if (!rBaseGameFacade || baseGameFacade == 0)
                    {
                        if (ChainFail()) continue;
                    }

                    var rGameFacade = InternalMemory.Read<uint>(baseGameFacade, out var gameFacade);
                    if (!rGameFacade || gameFacade == 0)
                    {
                        if (ChainFail()) continue;
                    }

                    var rStaticGameFacade = InternalMemory.Read<uint>(gameFacade + Offsets.StaticClass, out var staticGameFacade);
                    if (!rStaticGameFacade || staticGameFacade == 0)
                    {
                        if (ChainFail()) continue;
                    }

                    var rCurrentGame = InternalMemory.Read<uint>(staticGameFacade, out var currentGame);
                    if (!rCurrentGame || currentGame == 0)
                    {
                        if (ChainFail()) continue;
                    }

                    var rCurrentMatch = InternalMemory.Read<uint>(currentGame + Offsets.CurrentMatch, out var currentMatch);
                    if (!rCurrentMatch || currentMatch == 0)
                    {
                        if (ChainFail()) continue;
                    }

                    var rLocalPlayer = InternalMemory.Read<uint>(currentMatch + Offsets.LocalPlayer, out var localPlayer);
                    if (!rLocalPlayer || localPlayer == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    Core.LocalPlayer = localPlayer;
                    LogLocalWeapon(localPlayer);

                    var rMainTransform = InternalMemory.Read<uint>(localPlayer + Offsets.MainCameraTransform, out var mainTransform);
                    if (!rMainTransform || mainTransform == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var rMainTransformPos = Transform.GetPosition(mainTransform, out var mainPos);
                    if (rMainTransformPos)
                    {
                        Core.LocalMainCamera = mainPos;
                    }

                    var rFollowCamera = InternalMemory.Read<uint>(localPlayer + Offsets.FollowCamera, out var followCamera);
                    if (!rFollowCamera || followCamera == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var rCamera = InternalMemory.Read<uint>(followCamera + Offsets.Camera, out var camera);
                    if (!rCamera || camera == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var rCameraBase = InternalMemory.Read<uint>(camera + 0x8, out var cameraBase);
                    if (!rCameraBase || cameraBase == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    Core.HaveMatrix = true;

                    var rViewMatrix = InternalMemory.Read<Matrix4x4>(cameraBase + Offsets.ViewMatrix, out var viewMatrix);
                    if (!rViewMatrix)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    Core.CameraMatrix = viewMatrix;
                    consecFails = 0;

                    // ---- Weapon features (OB55: all on weaponData, not PlayerAttributes) ----
                    var readWeapon = InternalMemory.Read<uint>(localPlayer + Offsets.Weapon, out var weapon);
                    uint weaponData = 0;
                    if (readWeapon && weapon != 0)
                    {
                        InternalMemory.Read<uint>(weapon + Offsets.WeaponData, out weaponData);
                    }

                    if (weaponData != 0)
                    {
                        // NoRecoil: weaponData + 0xC (float -> 0)
                        if (Config.NoRecoil)
                        {
                            if (InternalMemory.Read<float>(weaponData + Offsets.WeaponRecoil, out var recoil) && recoil != 0f)
                                InternalMemory.Write(weaponData + Offsets.WeaponRecoil, 0f);
                        }

                        if (Config.FastReload)
                        {
                            if (InternalMemory.Read<uint>(localPlayer + Offsets.PlayerAttributes, out var reload))
                            {
                                InternalMemory.Write<bool>(reload + Offsets.NoReload, true);
                            }
                            lastFastReloadState = true;
                        }
                        else if (lastFastReloadState)
                        {
                            if (InternalMemory.Read<uint>(localPlayer + Offsets.PlayerAttributes, out var reload))
                            {
                                InternalMemory.Write<bool>(reload + Offsets.NoReload, false);
                            }
                            lastFastReloadState = false;
                        }

                        // UnlimitedAmmo: weaponData + 0xE0 (int 999). Burst x3 beats per-frame reset.
                        if (Config.UnlimitedAmmo)
                        {
                            for (int i = 0; i < 3; i++)
                                InternalMemory.Write(weaponData + Offsets.UnlimitedAmmo, 999);
                        }

                        // RapidFire: weaponData + 0x1C4/0x1C8/0x1D4 (fire interval scale).
                        // Old code wrote to PlayerAttributes+0x1C8 (wrong base, single addr).
                        if (InternalMemory.Read<uint>(localPlayer + Offsets.PlayerAttributes, out var attributesAddr2) && attributesAddr2 != 0)
                        {
                            ulong addrKey = weaponData; // track per-weapon so restore is correct
                            if (Config.RapidFire)
                            {
                                if (InternalMemory.Read<float>(weaponData + Offsets.RapidfirePtr, out var currentFireInterval))
                                {
                                    if (!originalFireIntervalValues.ContainsKey(addrKey))
                                        originalFireIntervalValues[addrKey] = currentFireInterval;
                                    // slider 0.1 (fastest) .. 1.0 (slowest)
                                    float rapidFireValue = 1.1f - Config.RapidFireSpeed;
                                    if (Math.Abs(currentFireInterval - rapidFireValue) > 0.001f)
                                    {
                                        InternalMemory.Write(weaponData + Offsets.RapidfirePtr, rapidFireValue);
                                        InternalMemory.Write(weaponData + Offsets.Attr_FireIntervalScale1, rapidFireValue);
                                        InternalMemory.Write(weaponData + Offsets.Attr_FireIntervalScale2, rapidFireValue);
                                    }
                                }
                                lastRapidFireState = true;
                            }
                            else if (lastRapidFireState)
                            {
                                if (originalFireIntervalValues.TryGetValue(addrKey, out var savedValue))
                                {
                                    InternalMemory.Write(weaponData + Offsets.RapidfirePtr, savedValue);
                                    InternalMemory.Write(weaponData + Offsets.Attr_FireIntervalScale1, savedValue);
                                    InternalMemory.Write(weaponData + Offsets.Attr_FireIntervalScale2, savedValue);
                                }
                                else
                                {
                                    InternalMemory.Write(weaponData + Offsets.RapidfirePtr, 1.0f);
                                }
                                lastRapidFireState = false;
                            }
                        }
                    }

                    // AutoFire: hold local IsFiring while a visible enemy is in range.
                    // Writes every frame while the condition holds (game resets the flag),
                    // releases once (edge-triggered) when it ends or the feature is off.
                    bool wantAutoFire = false;
                    if (Config.AutoFire)
                    {
                        float autoMaxDist = Math.Max(5f, Config.AutoFireMaxDistance);
                        foreach (var kv in Core.Entities)
                        {
                            var e = kv.Value;
                            if (e == null) continue;
                            if (e.Address == localPlayer) continue;
                            if (!e.IsKnown || e.IsTeam == Bool3.True) continue;
                            if (e.IsDead || e.IsKnocked) continue;
                            if (!e.isVisible) continue;
                            if (e.Distance < 1f || e.Distance > autoMaxDist) continue;
                            wantAutoFire = true;
                            break;
                        }
                        if (wantAutoFire)
                        {
                            InternalMemory.Write<bool>(localPlayer + Offsets.LocalPlayerIsFiring, true);
                            InternalMemory.Write<bool>(localPlayer + Offsets.IsFiring, true);
                            lastAutoFireState = true;
                        }
                        else if (lastAutoFireState)
                        {
                            InternalMemory.Write<bool>(localPlayer + Offsets.LocalPlayerIsFiring, false);
                            InternalMemory.Write<bool>(localPlayer + Offsets.IsFiring, false);
                            lastAutoFireState = false;
                        }
                    }
                    else if (lastAutoFireState)
                    {
                        InternalMemory.Write<bool>(localPlayer + Offsets.LocalPlayerIsFiring, false);
                        InternalMemory.Write<bool>(localPlayer + Offsets.IsFiring, false);
                        lastAutoFireState = false;
                    }

                    var rSpeedHackPtr = InternalMemory.Read<uint>(currentGame + Offsets.GameTimer, out var speedTimerAddr);
                    if (rSpeedHackPtr && speedTimerAddr != 0)
                    {
                        if (Config.speedint)
                        {
                            // Fixed 1.5x — tested best speed with no rubber-banding.
                            const float SpeedMultiplier = 1.5f;
                            float targetSpeed = 0.033f * SpeedMultiplier;

                            if (InternalMemory.Read<float>(speedTimerAddr + Offsets.FixedDeltaTime, out var currentSpeed))
                            {
                                if (Math.Abs(currentSpeed - targetSpeed) > 0.0001f)
                                {
                                    InternalMemory.Write<float>(speedTimerAddr + Offsets.FixedDeltaTime, targetSpeed);
                                }
                            }
                        }
                        else
                        {
                            if (InternalMemory.Read<float>(speedTimerAddr + Offsets.FixedDeltaTime, out var currentSpeed))
                            {
                                if (Math.Abs(currentSpeed - 0.033f) > 0.0001f)
                                {
                                    InternalMemory.Write<float>(speedTimerAddr + Offsets.FixedDeltaTime, 0.033f);
                                }
                            }
                        }
                    }

                    var dictEntities = GetEntities(currentGame, Offsets.DictionaryEntities);
                    foreach (var entity in dictEntities)
                    {
                        if (entity == 0) continue;
                        if (entity == localPlayer) continue;

                        Entity? player;

                        if (Core.Entities.TryGetValue(entity, out player) && player != null)
                        {
                            player.Address = entity;
                            player.LastSeen = DateTime.Now;

                            // Team re-validated EVERY frame (fixes stale team when addresses recycle:
                            // weaponless walkers / respawns kept old flags and lost their box).
                            {
                                var rAvatarManager = InternalMemory.Read<uint>(entity + Offsets.AvatarManager, out var avatarManager);

                                if (rAvatarManager && avatarManager != 0)
                                {
                                    var rAvatar = InternalMemory.Read<uint>(avatarManager + Offsets.Avatar, out var avatar);

                                    if (rAvatar && avatar != 0)
                                    {
                                        var rIsVisible = InternalMemory.Read<bool>(avatar + Offsets.Avatar_IsVisible, out var isVisible);
                                        player.isVisible = rIsVisible && isVisible;

                                        if (rIsVisible && isVisible)
                                        {
                                            var rAvatarData = InternalMemory.Read<uint>(avatar + Offsets.Avatar_Data, out var avatarData);

                                            if (rAvatarData && avatarData != 0)
                                            {
                                                var rIsTeam = InternalMemory.Read<bool>(avatarData + Offsets.Avatar_Data_IsTeam, out var isTeam);
                                                if (rIsTeam)
                                                {
                                                    if (isTeam)
                                                    {
                                                        player.IsTeam = Bool3.True;
                                                    }
                                                    else
                                                    {
                                                        player.IsTeam = Bool3.False;
                                                        player.IsKnown = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Names read for EVERYONE (no team gate): even unresolved players get names.
                            if (Config.ESPName || Config.ESPInformation)
                            {
                                var rNameAddr0 = InternalMemory.Read<uint>(entity + Offsets.Player_Name, out var nameAddr0);
                                if (rNameAddr0 && nameAddr0 != 0)
                                {
                                    var rNameLen0 = InternalMemory.Read<int>(nameAddr0 + 0x8, out var nameLen0);
                                    // Guard: corrupt length (<=0/huge) would throw below and abort the whole
                                    // frame -> frozen ESP that slides with the camera until manual refresh.
                                    if (rNameLen0 && nameLen0 > 0 && nameLen0 <= 2000)
                                    {
                                        int charCount0 = Math.Min(nameLen0, 512);
                                        var nm0 = InternalMemory.ReadString(nameAddr0 + 0xC, charCount0 * 2)?.Trim('\0');
                                        if (!string.IsNullOrEmpty(nm0))
                                        {
                                            player.Name = nm0;
                                            LogName(nm0);
                                        }
                                    }
                                }
                            }

                            if (player.IsTeam == Bool3.True) continue;

                            if (!player.IsKnown) continue;
                            Core.LootItems.TryRemove(entity, out _);

                            if (Config.IgnoreKnocked)
                            {
                                if (InternalMemory.Read<uint>(entity + Offsets.Player_ShadowBase, out var shadowBase))
                                {
                                    if (shadowBase != 0)
                                    {
                                        if (InternalMemory.Read<int>(shadowBase + Offsets.XPose, out var xpose))
                                        {
                                            player.IsKnocked = xpose == 8;
                                        }
                                    }
                                }
                            }

                            var rIsDead = InternalMemory.Read<bool>(entity + Offsets.Player_IsDead, out var isDead);

                            if (rIsDead)
                            {
                                bool wasAlive = !player.IsDead;
                                player.IsDead = isDead;
                                // Death purges EVERYTHING: no name, no loot marker stays on the corpse.
                                if (isDead && wasAlive)
                                {
                                    Core.LootItems.TryRemove(entity, out _);
                                    lootCand.Remove(entity);
                                }
                            }

                            // Weapon / Name / Health each gated by its own flag (weapon used to require Name/Health ON -> icon never showed).
                            if (Config.ESPHealth || Config.ESPName || Config.espweapon || Config.ESPInformation)
                            {
                                if (Config.ESPName || Config.ESPInformation)
                                {
                                var rNameAddr = InternalMemory.Read<uint>(entity + Offsets.Player_Name, out var nameAddr);
                                if (rNameAddr && nameAddr != 0)
                                {
                                    var rNameLen = InternalMemory.Read<int>(nameAddr + 0x8, out var nameLen);
                                    if (rNameLen && nameLen > 0 && nameLen <= 2000)
                                    {
                                        // UTF-16 char count at +0x8; cap read only for safety.
                                        int charCount = Math.Min(nameLen, 512);
                                        var name = InternalMemory.ReadString(nameAddr + 0xC, charCount * 2)?.Trim('\0');
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            player.Name = name;
                                            LogName(name);
                                        }
                                    }
                                }
                                }

                                if (Config.espweapon)
                                {
                                    var rDataPool2 = InternalMemory.Read<uint>(entity + Offsets.Player_Data, out var dataPool2);
                                    if (rDataPool2 && dataPool2 != 0)
                                    {
                                        var rPoolObj2 = InternalMemory.Read<uint>(dataPool2 + 0x8, out var poolObj2);
                                        if (rPoolObj2 && poolObj2 != 0)
                                        {
                                            var rPool2 = InternalMemory.Read<uint>(poolObj2 + 0x20, out var pool2);
                                            if (rPool2 && pool2 != 0)
                                            {
                                                var rweaponid = InternalMemory.Read<short>(pool2 + 0x10, out var weaponid);
                                                if (rweaponid && weaponid != 0)
                                                {
                                                    player.WeaponName = WeaponIndex.GetWeaponName(weaponid);
                                                    LogWeaponId(weaponid, player.WeaponName);
                                                }
                                            }
                                        }
                                    }
                                    // Fallback: pool chain fails for some weapons (e.g. Thompson) ->
                                    // read the enemy active-weapon object directly.
                                    if (string.IsNullOrEmpty(player.WeaponName))
                                    {
                                        if (InternalMemory.Read<uint>(entity + Offsets.Weapon, out var actW) && actW != 0)
                                        {
                                            if (InternalMemory.Read<ushort>(actW + Offsets.WeaponID, out var wid) && wid != 0)
                                            {
                                                var nm = WeaponIndex.GetWeaponName(wid);
                                                LogWeaponId(wid, "FB:" + nm);
                                                if (!nm.StartsWith("Unknown"))
                                                    player.WeaponName = nm;
                                            }
                                            if (string.IsNullOrEmpty(player.WeaponName)
                                                && InternalMemory.Read<int>(actW + Offsets.WeaponID, out var wid2) && wid2 > 0 && wid2 < 30000)
                                            {
                                                var nm2 = WeaponIndex.GetWeaponName(wid2);
                                                LogWeaponId(wid2, "FB2:" + nm2);
                                                if (!nm2.StartsWith("Unknown"))
                                                    player.WeaponName = nm2;
                                            }
                                        }
                                    }
                                }

                                if (Config.ESPHealth || Config.ESPInformation)
                                {
                                var rDataPool = InternalMemory.Read<uint>(entity + Offsets.Player_Data, out var dataPool);
                                if (rDataPool && dataPool != 0)
                                {
                                    var rPoolObj = InternalMemory.Read<uint>(dataPool + 0x8, out var poolObj);
                                    if (rPoolObj && poolObj != 0)
                                    {
                                        // OB55: pool + 0x10 (short). Old code mixed 0xC/0x10.
                                        var rPool = InternalMemory.Read<uint>(poolObj + 0x10, out var pool);
                                        if (rPool && pool != 0)
                                        {
                                            var rHealth = InternalMemory.Read<short>(pool + 0x10, out var health);
                                            if (rHealth && health != 0)
                                                player.Health = health;
                                        }
                                    }
                                }
                                }
                            }

                            var rHeadBone = InternalMemory.Read<uint>(entity + (uint)Bones.Head, out var headBone);

                            if (rHeadBone && headBone != 0)
                            {
                                var rHeadTrans = Transform.GetNodePosition(headBone, out var headTransform);

                                if (rHeadTrans)
                                {
                                    player.Head = headTransform;
                                    player.Distance = Vector3.Distance(mainPos, headTransform);
                                }
                            }

                            var rRootBone = InternalMemory.Read<uint>(entity + (uint)Bones.Root, out var rootBone);
                            if (rRootBone && rootBone != 0)
                            {
                                var rRootTrans = Transform.GetNodePosition(rootBone, out var rootTransform);

                                if (rRootTrans)
                                {
                                    player.Root = rootTransform;
                                }
                            }
                            var boneOffsets = new[]
                            {
                                Bones.Head, Bones.LeftWrist, Bones.Spine,
                                Bones.Hip, Bones.Root, Bones.RightCalf, Bones.LeftCalf,
                                Bones.RightFoot, Bones.LeftFoot, Bones.RightWrist, Bones.LeftHand,
                                Bones.LeftShoulder, Bones.RightShoulder, Bones.RightWristJoint, Bones.LeftWristJoint,
                                Bones.LeftElbow, Bones.RightElbow
                            };
                            foreach (var offset in boneOffsets)
                            {
                                var rBone = InternalMemory.Read<uint>(entity + (uint)offset, out var bone);
                                if (rBone && bone != 0)
                                {
                                    var rBonePos = Transform.GetNodePosition(bone, out var boneTransform);
                                    if (rBonePos)
                                    {
                                        switch (offset)
                                        {
                                            case Bones.Head:
                                                player.Head = boneTransform;
                                                break;
                                            case Bones.LeftWrist:
                                                player.LeftWrist = boneTransform;
                                                player.LeftWristJoint = boneTransform;
                                                break;
                                            case Bones.Spine:
                                                player.Spine = boneTransform;
                                                break;
                                            case Bones.Hip:
                                                player.Hip = boneTransform;
                                                break;
                                            case Bones.Root:
                                                player.Root = boneTransform;
                                                break;
                                            case Bones.RightCalf:
                                                player.RightCalf = boneTransform;
                                                break;
                                            case Bones.LeftCalf:
                                                player.LeftCalf = boneTransform;
                                                break;
                                            case Bones.RightFoot:
                                                player.RightFoot = boneTransform;
                                                break;
                                            case Bones.LeftFoot:
                                                player.LeftFoot = boneTransform;
                                                break;
                                            case Bones.RightWrist:
                                                player.RightWrist = boneTransform;
                                                break;
                                            case Bones.LeftHand:
                                                player.LeftHand = boneTransform;
                                                break;
                                            case Bones.LeftShoulder:
                                                player.LeftShoulder = boneTransform;
                                                break;
                                            case Bones.RightShoulder:
                                                player.RightShoulder = boneTransform;
                                                break;
                                            case Bones.RightWristJoint:
                                                player.RightWristJoint = boneTransform;
                                                break;
                                            case Bones.RightElbow:
                                                player.RightElbow = boneTransform;
                                                break;
                                            case Bones.LeftElbow:
                                                player.LeftElbow = boneTransform;
                                                break;
                                        }
                                        player.Distance = Vector3.Distance(Core.LocalMainCamera, player.Head);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Core.Entities[entity] = new Entity
                            {
                                Address = entity,
                                LastSeen = DateTime.Now,
                                IsTeam = Bool3.Unknown,
                                IsKnown = false,
                                IsDead = false,
                                Health = 0,
                                IsKnocked = false,
                                Head = Vector3.Zero,
                                LeftWrist = Vector3.Zero,

                                Spine = Vector3.Zero,

                                Root = Vector3.Zero,
                                Hip = Vector3.Zero,
                                RightCalf = Vector3.Zero,
                                LeftCalf = Vector3.Zero,
                                RightFoot = Vector3.Zero,
                                LeftFoot = Vector3.Zero,
                                RightWrist = Vector3.Zero,
                                LeftHand = Vector3.Zero,
                                RightShoulder = Vector3.Zero,
                                RightWristJoint = Vector3.Zero,
                                LeftWristJoint = Vector3.Zero,
                                RightElbow = Vector3.Zero,
                                LeftElbow = Vector3.Zero,
                                Name = ""
                            };
                        }
                    }
                    // Per-entity expiry (5s unseen): no more wholesale wipes, no more blink.
                    var expiryCutoff = DateTime.Now.AddSeconds(-5);
                    foreach (var kv in Core.Entities)
                    {
                        if (kv.Value == null || kv.Value.LastSeen < expiryCutoff)
                            Core.Entities.TryRemove(kv.Key, out _);
                    }
                    // Slow loot scan (500ms): ground items only, stability-filtered.
                    if (Config.LootWeaponsESP && (DateTime.Now - lastLootScan).TotalMilliseconds > 500)
                    {
                        lastLootScan = DateTime.Now;
                        LootScan(currentGame, currentMatch, dictEntities);
                    }
                    Core.LastDataUpdate = DateTime.Now;
                    Thread.Sleep(5);
                }
                catch
                {
                    Thread.Sleep(30);
                }
            }
        }
        static List<uint> GetEntities(uint baseGame, uint offset)
        {
            List<uint> entityList = new List<uint>();

            if (!InternalMemory.Read<uint>(baseGame + offset, out uint dict) || dict == 0)
                return entityList;

            if (!InternalMemory.Read<int>(dict + 0x10, out int count) || count < 1 || count > 1000)
                return entityList;

            if (!InternalMemory.Read<uint>(dict + 0xC, out uint entries) || entries == 0)
                return entityList;

            uint start = entries + 0x10;

            for (uint i = 0; i < count; i++)
            {
                uint entry = start + (i * 0x10);

                if (!InternalMemory.Read<int>(entry + 0x0, out int hash) || hash < 0)
                    continue;

                if (!InternalMemory.Read<uint>(entry + 0xC, out uint entity) || entity == 0)
                    continue;

                entityList.Add(entity);
            }

            return entityList;
        }
        static void ResetCache()
        {
            try { InternalMemory.Cache.Clear(); } catch { }
        }
    }
}
