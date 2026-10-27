using AotForms;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;

namespace Client
{
    internal class KCBRUTALSILENT
    {
        private static readonly object Sync = new object();
        private static readonly Random _rand = new();
        private static Thread? workerThread;
        private static volatile bool running;
        private static Keys activationKey = Keys.F6;

        internal static void Start()
        {
            lock (Sync)
            {
                if (running)
                    return;

                running = true;
                workerThread = new Thread(Work)
                {
                    IsBackground = true,
                    Name = "KCBRUTALSILENT.Work"
                };

                workerThread.Start();
            }
        }

        internal static void Stop()
        {
            lock (Sync)
            {
                running = false;
            }

            var thread = workerThread;
            if (thread != null && thread.IsAlive)
            {
                thread.Join(500);
            }

            workerThread = null;
        }

        internal static void SetActivationKey(Keys key)
        {
            activationKey = key;
        }

        internal static Keys GetActivationKey()
        {
            return activationKey;
        }

        internal static void Work()
        {
            // BootstrapRuntime starts Work directly: own the loop flag here too.
            running = true;
            bool wasPressed = false;

            while (running)
            {
                bool pressed = (Control.ModifierKeys == Keys.None &&
                                (GetAsyncKeyState((int)activationKey) & 0x8000) != 0);

                if (pressed && wasPressed == false)
                {
                    Config.kcbrutasilnetaim = !Config.kcbrutasilnetaim;
                }

                wasPressed = pressed;

                if (!Config.kcbrutasilnetaim ||
                    Core.Width == -1 ||
                    Core.Height == -1 ||
                    !Core.HaveMatrix)
                {
                    Thread.SpinWait(10);
                    continue;
                }

                Entity? target = FindBestTarget();
                if (target == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

                // Silent single-weapon filter: only redirects while holding the chosen gun.
                if (Config.SilentWeaponOnly && !IsSilentWeaponHeld())
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (target != null)
                {
                    // First-bullet strength: the aim vector is refreshed EVERY spin while a
                    // target exists (not only while firing), so even a single tap goes to HEAD.
                    if (InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.sAim2, out uint weaponBase) && weaponBase != 0)
                    {
                        // حساب موقع الرأس بدقة متناهية (مركز الرأس تماماً)
                        Vector3 liveHead = target.Head;
                        if (InternalMemory.Read<uint>(target.Address + (uint)Bones.Head, out uint headBone) && headBone != 0)
                        {
                            if (Transform.GetNodePosition(headBone, out var hpNow))
                                liveHead = hpNow;
                        }
                        float jy = 0.09f + ((float)_rand.NextDouble() - 0.5f) * 0.04f;
                        Vector3 preciseHead = new Vector3(liveHead.X, liveHead.Y + jy, liveHead.Z);

                        if (InternalMemory.Read<Vector3>(weaponBase + Offsets.sAim3, out Vector3 startPos))
                        {
                            Vector3 aimPosition = preciseHead - startPos;

                            // كتابة القيم مباشرة وبأقصى سرعة ممكنة لمنع أي تأخير (Delay)
                            InternalMemory.Write<Vector3>(weaponBase + Offsets.sAim4, aimPosition);
                            if (InternalMemory.Read<Vector3>(weaponBase + Offsets.sAim4, out Vector3 check)
                                && Vector3.DistanceSquared(check, aimPosition) > 0.000001f)
                            {
                                InternalMemory.Write<Vector3>(weaponBase + Offsets.sAim4, aimPosition);
                                InternalMemory.Write<Vector3>(weaponBase + Offsets.sAim4, aimPosition);
                            }
                        }
                    }
                }

                // إزالة أي خمول زائد لزيادة استجابة الـ Thread بأقصى سرعة ممكنة
                Thread.SpinWait(0);
            }
        }

        private static bool IsSilentWeaponHeld()
        {
            if (string.IsNullOrEmpty(Config.SilentWeaponKey)) return true;
            string held = Data.GetLocalWeaponName();
            if (string.IsNullOrEmpty(held) || held.StartsWith("Unknown")) return false;
            return held.Equals(Config.SilentWeaponKey, StringComparison.OrdinalIgnoreCase);
        }

        private struct TargetCandidate
        {
            internal Entity Entity;
            internal float Score;
        }

        // Reused top-4 buffer: no per-cycle allocations (req: 20-30 players, zero garbage).
        private static readonly TargetCandidate[] _candBuf = new TargetCandidate[4];

        private static Entity? FindBestTarget()
        {
            // SAME FOV as the Rage "Field of View" trackbar (Config.AimFov): one value drives both.
            float fov = Config.AimFov > 0f ? Config.AimFov : 200f;
            Vector2 screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
            int n = 0;

            try
            {
                // No snapshot List: iterate live (a concurrent edit just skips this cycle).
                foreach (var entity in Core.Entities.Values)
                {
                    if (entity == null || !entity.IsKnown || entity.IsDead)
                        continue;

                    if (Config.IgnoreKnocked && entity.IsKnocked)
                        continue;

                    // Cheap world-distance reject BEFORE the expensive W2S projection.
                    if (Vector3.Distance(Core.LocalMainCamera, entity.Head) > 250f)
                        continue;

                    var head2D = W2S.WorldToScreen(
                        Core.CameraMatrix,
                        entity.Head,
                        Core.Width,
                        Core.Height);

                    const float margin = 150f;
                    if (head2D.X < -margin || head2D.Y < -margin ||
                        head2D.X > Core.Width + margin || head2D.Y > Core.Height + margin)
                        continue;
                    // Behind-camera guard: in front only.
                    if (head2D.X <= 0 && head2D.Y <= 0)
                        continue;

                    float screenDistance = Vector2.Distance(
                        screenCenter,
                        new Vector2(head2D.X, head2D.Y));

                    // In front AND inside the shared FOV.
                    if (screenDistance > fov)
                        continue;

                    // Keep max 4 closest candidates.
                    if (n < 4)
                    {
                        _candBuf[n++] = new TargetCandidate { Entity = entity, Score = screenDistance };
                    }
                    else
                    {
                        int worst = 0;
                        for (int i = 1; i < 4; i++)
                        {
                            if (_candBuf[i].Score > _candBuf[worst].Score) worst = i;
                        }
                        if (screenDistance < _candBuf[worst].Score)
                            _candBuf[worst] = new TargetCandidate { Entity = entity, Score = screenDistance };
                    }
                }
            }
            catch
            {
                return null;
            }

            // Current candidate invalid -> next eligible one.
            Entity? bestTarget = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = _candBuf[i];
                if (c.Entity == null || !c.Entity.IsKnown || c.Entity.IsDead) continue;
                if (Config.IgnoreKnocked && c.Entity.IsKnocked) continue;
                if (c.Score < bestScore) { bestScore = c.Score; bestTarget = c.Entity; }
            }

            return bestTarget;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
