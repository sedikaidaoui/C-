using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AotForms
{
    internal static class AimbotV2
    {


        internal static void Work()
        {
            while (true)
            {
                if (!Config.enableAimBot)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if ((WinAPI.GetAsyncKeyState(Config.AimbotKey) & 0x8000) == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                
                Entity target = GetTarget(Config.TargetingMode);


                if (target != null)
                {

                    var playerLook = MathUtils.GetRotationToLocation(target.Head, 0.1f, Core.LocalMainCamera);

                    if (Config.SilentAim)
                    {

                        var Isfiring = InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.sAim1, out var isfiring);

                        if (isfiring)
                        {
                            var WeaponID = InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.sAim2, out var Weaponid);
                            if (Weaponid != 0)
                            {
                                InternalMemory.Read<Vector3>(Weaponid + Offsets.sAim3, out var Pos);

                                InternalMemory.Write<Vector3>(Weaponid + Offsets.sAim4, target.Head - Pos);

                            }
                        }

                    }

              
                    else if (Config.AimBotRage)
                    {
                        // Chest default, head ceiling: normal play sticks to chest, raising the
                        // mouse follows up but the aim NEVER goes above the head (sticks to it),
                        // lowering goes back down normally.
                        Vector3 chestPos = target.Spine != Vector3.Zero ? target.Spine : target.Head;
                        var chestLook = MathUtils.GetRotationToLocation(chestPos, 0.1f, Core.LocalMainCamera);
                        var headLook = MathUtils.GetRotationToLocation(target.Head, 0.1f, Core.LocalMainCamera);
                        Quaternion useLook = chestLook;
                        if (InternalMemory.Read<Quaternion>(Core.LocalPlayer + Offsets.AimRotation, out var curRot)
                            && curRot != Quaternion.Identity)
                        {
                            Vector3 curFwd = Vector3.Transform(Vector3.UnitZ, curRot);
                            Vector3 headFwd = Vector3.Transform(Vector3.UnitZ, headLook);
                            float curPitch = MathF.Asin(Math.Clamp(curFwd.Y, -1f, 1f));
                            float headPitch = MathF.Asin(Math.Clamp(headFwd.Y, -1f, 1f));
                            if (curPitch > headPitch)
                                useLook = headLook;
                        }
                        InternalMemory.Write(Core.LocalPlayer + Offsets.AimRotation, useLook);

                    }

                    if (Config.SilentAim)
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        Thread.Sleep((int)Config.AimBotSmooth);
                    }
                }
            }
        }

        static Entity GetTarget(TargetingMode mode)
        {
            if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix) return null;

            switch (mode)
            {
                case TargetingMode.ClosestToCrosshair:
                    return GetTargetByClosestToCrosshair();
                case TargetingMode.FarthestFromCrosshair:
                    return GetTargetByFarthestFromCrosshair();
                case TargetingMode.Target360:
                    return GetTarget360();
                case TargetingMode.LowestHealth:
                    return GetTargetByLowestHealth();
                case TargetingMode.ClosestToPlayer:
                    return GetTargetByClosestToPlayer();
                default:
                    return GetTargetByClosestToCrosshair();
            }
        }

        static Entity GetTargetByClosestToCrosshair()
        {
            Entity target = null;
            float distance = float.MaxValue;
            var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;

                var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);

                Vector2 headScreenPos2 = new Vector2(head2D.X, head2D.Y);

                var playerDistance = Vector2.Distance(screenCenter, headScreenPos2);
                if (playerDistance < distance)
                {
                    distance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }

         static Entity GetTargetByFarthestFromCrosshair()
        {
            Entity target = null;
            float distance = float.MinValue;
            var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (head2D.X < 1 || head2D.Y < 1) continue;

                var playerDistance = Vector2.Distance(screenCenter, new Vector2(head2D.X, head2D.Y));
                if (playerDistance > distance)
                {
                    distance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }

        
        static Entity GetTargetByLowestHealth()
        {
            Entity target = null;
            float lowestHealth = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var target2D = W2S.WorldToScreen(Core.CameraMatrix, targetPosition, Core.Width, Core.Height);
              //  if (target2D.X < 1 || target2D.Y < 1) continue;

                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;

                
                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                var x = target2D.X - screenCenter.X;
                var y = target2D.Y - screenCenter.Y;
                var crosshairDist = (float)Math.Sqrt(x * x + y * y);
              //  if (crosshairDist > Config.AimFov) continue;

                
                if (entity.Health < lowestHealth)
                {
                    lowestHealth = entity.Health;
                    target = entity;
                }
            }

            return target;
        }

        
        static Entity GetTargetByClosestToPlayer()
        {
            Entity target = null;
            float closestDistance = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var target2D = W2S.WorldToScreen(Core.CameraMatrix, targetPosition, Core.Width, Core.Height);
                if (target2D.X < 1 || target2D.Y < 1) continue;

                
                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;

                
                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);
                var x = target2D.X - screenCenter.X;
                var y = target2D.Y - screenCenter.Y;
                var crosshairDist = (float)Math.Sqrt(x * x + y * y);
                if (crosshairDist > Config.AimFov) continue;

                
                if (playerDistance < closestDistance)
                {
                    closestDistance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }
        static Entity GetTarget360()
        {
            Entity target = null;
            float closestDistance = float.MaxValue;

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead || (Config.IgnoreKnocked && entity.IsKnocked)) continue;

                Vector3 targetPosition = entity.Head;
                var playerDistance = Vector3.Distance(Core.LocalMainCamera, targetPosition);
                if (playerDistance > Config.AimBotMaxDistance) continue;

                if (playerDistance < closestDistance)
                {
                    closestDistance = playerDistance;
                    target = entity;
                }
            }

            return target;
        }


    }
}
