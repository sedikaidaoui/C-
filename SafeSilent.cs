using System;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    internal static class SafeSilent
    {

        internal static void Work()
        {

            while (true)
            {
                if (!Config.kcsafeSilentAim)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if ((WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) == 0)
                {
                    Thread.Sleep(0);
                    continue;
                }

                Entity? target = null;
                float bestScore = Config.TargetingMode == TargetingMode.FarthestFromCrosshair ? float.MinValue : float.MaxValue;

                if (Core.Width == -1 || Core.Height == -1) { Thread.Sleep(1); continue; }
                if (!Core.HaveMatrix) { Thread.Sleep(1); continue; }

                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

                foreach (var entity in Core.Entities.Values)
                {
                    if (entity.IsDead) continue;

                    if (entity.IsKnocked) continue;

                    var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);

                    if (head2D.X < 1 || head2D.Y < 1) continue;

                    var playerDistance = Vector3.Distance(Core.LocalMainCamera, entity.Head);

                    if (playerDistance > Config.AimBotMaxDistance) continue;

                    var x = head2D.X - screenCenter.X;
                    var y = head2D.Y - screenCenter.Y;
                    var crosshairDist = (float)Math.Sqrt(x * x + y * y);

                    float score = Config.TargetingMode switch
                    {
                        TargetingMode.FarthestFromCrosshair => crosshairDist,
                        TargetingMode.LowestHealth => entity.Health,
                        TargetingMode.ClosestToPlayer or TargetingMode.Target360 => playerDistance,
                        _ => crosshairDist,
                    };
                    bool better = Config.TargetingMode == TargetingMode.FarthestFromCrosshair
                        ? score > bestScore
                        : score < bestScore;

                    if (better)
                    {
                        bestScore = score;
                        target = entity;
                    }
                }

                if (target != null)
                {

                    // Secure: fire-gate on the real firing flag (sAim1), single gentle write,
                    // human-paced loop. Never spams like brutal.
                    // Silent single-weapon filter.
                    if (Config.SilentWeaponOnly)
                    {
                        string held = Data.GetLocalWeaponName();
                        if (string.IsNullOrEmpty(Config.SilentWeaponKey) == false
                            && (string.IsNullOrEmpty(held) || held.StartsWith("Unknown")
                                || !held.Equals(Config.SilentWeaponKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                    }
                    var firecheck = InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.sAim1, out var firecheck2);

                    if (firecheck2)
                    {

                        var Unknown1 = InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.sAim2, out var unknown1);
                        if (unknown1 != 0)
                        {
                            InternalMemory.Read<Vector3>(unknown1 + Offsets.sAim3, out var Pos);
                            InternalMemory.Write<Vector3>(unknown1 + Offsets.sAim4, target.Head - Pos);
                        }
                    }
                    Thread.Sleep(2);
                }

                Thread.Sleep(1);
            }
        }
    }
}