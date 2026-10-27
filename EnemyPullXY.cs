using AotForms;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    /// <summary>
    /// Enemy Pull XY (TEST): aggressive instant pull, head lands on the firing line
    /// in full XYZ (left-right AND up-down). Experimental — use with care.
    /// </summary>
    internal static class EnemyPullXY
    {
        private static Thread? pullThread;
        private static CancellationTokenSource? cts;
        private static bool isRunning = false;
        private static readonly Random _rand = new();

        private static readonly Dictionary<uint, Vector3> originalPositions = new();
        private static Entity? currentTarget = null;

        private static readonly Dictionary<uint, (Vector3 pos, DateTime at)> _pendingRestore = new();
        private const int RestoreDelayMs = 2000;
        private const float MaxPull = 60f;

        public static void Start()
        {
            if (isRunning) return;

            cts = new CancellationTokenSource();
            pullThread = new Thread(() => Work(cts.Token))
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            pullThread.Start();
            isRunning = true;
        }

        public static void Stop()
        {
            if (!isRunning) return;

            try { cts?.Cancel(); } catch { }
            RestoreAllPositions();
            isRunning = false;
        }

        private static void Work(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Config.EnemyPullXY)
                    {
                        if (originalPositions.Count > 0 || _pendingRestore.Count > 0)
                            RestoreAllPositions();
                        currentTarget = null;
                        Thread.Sleep(5);
                        continue;
                    }

                    if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (EnemyPullHelpers.IsLocalFiring())
                    {
                        CancelPendingRestore();
                        currentTarget = FindBestTarget();
                        if (currentTarget != null)
                        {
                            ApplyPullXY(currentTarget);
                        }
                    }
                    else
                    {
                        ScheduleDelayedRestore();
                        currentTarget = null;
                    }
                    RestoreExpired();
                }
                catch { }

                int delay = Config.EnemyPullTickMs > 0 ? Config.EnemyPullTickMs : 6;
                Thread.Sleep(delay + _rand.Next(0, 3));
            }
        }

        private static Entity? FindBestTarget()
        {
            Entity? bestTarget = null;
            float closestDist = float.MaxValue;
            var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead) continue;
                if (Config.IgnoreKnocked && entity.IsKnocked) continue;

                var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (head2D.X < 1 || head2D.Y < 1) continue;

                float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist3D > Config.EnemyPullMaxDistance) continue;

                float crosshairDist = Vector2.Distance(screenCenter, head2D);
                if (crosshairDist > EnemyPullHelpers.GetPullFovRadius()) continue;

                if (crosshairDist < closestDist)
                {
                    closestDist = crosshairDist;
                    bestTarget = entity;
                }
            }
            return bestTarget;
        }

        private static void ApplyPullXY(Entity entity)
        {
            try
            {
                if (!InternalMemory.Read(entity.Address + (uint)Bones.Root, out uint rootBonePtr)) return;
                if (!InternalMemory.Read(rootBonePtr + 0x8, out uint transformValue)) return;
                if (!InternalMemory.Read(transformValue + 0x8, out uint transformObjPtr)) return;
                if (!InternalMemory.Read(transformObjPtr + 0x20, out uint matrixPtr)) return;

                if (!Transform.GetNodePosition(rootBonePtr, out var currentPos)) return;

                if (!originalPositions.ContainsKey(entity.Address))
                    originalPositions[entity.Address] = currentPos;

                Vector3 originalPos = originalPositions[entity.Address];

                Vector3 anchor = currentPos;
                if (InternalMemory.Read(entity.Address + (uint)Bones.Head, out uint headBonePtr) && headBonePtr != 0)
                {
                    if (Transform.GetNodePosition(headBonePtr, out var headPos))
                        anchor = headPos;
                }
                else if (entity.Head != Vector3.Zero)
                {
                    anchor = entity.Head;
                }
                Vector3 anchorOrig = originalPos + (anchor - currentPos);

                Vector3 fireDirection = new Vector3(
                    Core.CameraMatrix.M13,
                    Core.CameraMatrix.M23,
                    Core.CameraMatrix.M33
                );
                fireDirection = Vector3.Normalize(fireDirection);

                Vector3 camPos = Core.LocalMainCamera;

                Vector3 toEnemy = anchor - camPos;
                float projLength = Vector3.Dot(toEnemy, fireDirection);
                if (projLength < 1.5f) projLength = 1.5f;
                Vector3 targetPos = camPos + fireDirection * projLength;

                // TEST jitter so repeated locks never land byte-identical.
                targetPos.X += ((float)_rand.NextDouble() - 0.5f) * 0.8f;
                targetPos.Y += ((float)_rand.NextDouble() - 0.5f) * 0.4f;
                targetPos.Z += ((float)_rand.NextDouble() - 0.5f) * 0.8f;

                if (!float.IsFinite(targetPos.X + targetPos.Y + targetPos.Z))
                    return;
                Vector3 delta = targetPos - anchorOrig;
                if (delta.Length() > MaxPull)
                    targetPos = anchorOrig + Vector3.Normalize(delta) * MaxPull;

                // Rebase to root space (head and root move together).
                Vector3 rootTarget = targetPos - (anchor - currentPos);
                InternalMemory.Write((ulong)(matrixPtr + 0x60), rootTarget);
                InternalMemory.Write((ulong)(matrixPtr + 0x60), rootTarget);
            }
            catch { }
        }

        private static void RestoreAllPositions()
        {
            foreach (var entry in originalPositions)
            {
                try
                {
                    if (!InternalMemory.Read(entry.Key + (uint)Bones.Root, out uint rootBonePtr)) continue;
                    if (!InternalMemory.Read(rootBonePtr + 0x8, out uint transformValue)) continue;
                    if (!InternalMemory.Read(transformValue + 0x8, out uint transformObjPtr)) continue;
                    if (!InternalMemory.Read(transformObjPtr + 0x20, out uint matrixPtr)) continue;

                    InternalMemory.Write((ulong)(matrixPtr + 0x60), entry.Value);
                }
                catch { }
            }
            originalPositions.Clear();
            foreach (var kv in _pendingRestore)
            {
                try
                {
                    if (!InternalMemory.Read(kv.Key + (uint)Bones.Root, out uint rootBonePtr)) continue;
                    if (!InternalMemory.Read(rootBonePtr + 0x8, out uint transformValue)) continue;
                    if (!InternalMemory.Read(transformValue + 0x8, out uint transformObjPtr)) continue;
                    if (!InternalMemory.Read(transformObjPtr + 0x20, out uint matrixPtr)) continue;

                    InternalMemory.Write((ulong)(matrixPtr + 0x60), kv.Value.pos);
                }
                catch { }
            }
            _pendingRestore.Clear();
        }

        private static void ScheduleDelayedRestore()
        {
            var deadline = DateTime.Now.AddMilliseconds(RestoreDelayMs);
            foreach (var kv in originalPositions)
                _pendingRestore[kv.Key] = (kv.Value, deadline);
            originalPositions.Clear();
        }

        private static void CancelPendingRestore()
        {
            foreach (var kv in _pendingRestore)
                originalPositions[kv.Key] = kv.Value.pos;
            _pendingRestore.Clear();
        }

        private static void RestoreExpired()
        {
            if (_pendingRestore.Count == 0) return;
            var now = DateTime.Now;
            List<uint>? done = null;
            foreach (var kv in _pendingRestore)
            {
                if (kv.Value.at > now) continue;
                try
                {
                    if (!InternalMemory.Read(kv.Key + (uint)Bones.Root, out uint rootBonePtr)) continue;
                    if (!InternalMemory.Read(rootBonePtr + 0x8, out uint transformValue)) continue;
                    if (!InternalMemory.Read(transformValue + 0x8, out uint transformObjPtr)) continue;
                    if (!InternalMemory.Read(transformObjPtr + 0x20, out uint matrixPtr)) continue;

                    InternalMemory.Write((ulong)(matrixPtr + 0x60), kv.Value.pos);
                }
                catch { }
                (done ??= new List<uint>()).Add(kv.Key);
            }
            if (done != null)
                foreach (var k in done) _pendingRestore.Remove(k);
        }
    }
}
