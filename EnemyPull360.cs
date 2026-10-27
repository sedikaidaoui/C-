using AotForms;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    internal static class EnemyPull360
    {
        private static Thread pullThread;
        private static CancellationTokenSource cts;
        private static bool isRunning = false;

        private static readonly Dictionary<uint, Vector3> originalPositions = new();
        private static Entity currentTarget = null;

        // Hard lock — computed once per target per fire burst, then frozen until fire stops
        private static uint _lockedEntityId;
        private static Vector3 _lockedPullPos;
        private static bool _hasLockedPos;

        // Delayed restore: on release the enemy stays pulled 1s, then returns (no sudden snap).
        private static readonly Dictionary<uint, (Vector3 pos, DateTime at)> _pendingRestore = new();
        private const int RestoreDelayMs = 2000;

        private const float MaxSide = 4.5f;
        private const float MaxForward = 7.5f;
        private const float MaxUp = 0.50f;
        private const float PullYOffset = 0.08f;
        private const float MinProjDistance = 1.5f;
        private const int LockWritesPerTick = 4;

        internal static void Start()
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

        internal static void Stop()
        {
            if (!isRunning) return;

            cts?.Cancel();
            ClearLockState();
            RestoreAllPositions();
            isRunning = false;
        }

        private static void Work(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool pullingThisFrame = false;
                try
                {
                    if (!Config.EnemyPullEnabled)
                    {
                        if (originalPositions.Count > 0 || _hasLockedPos || _pendingRestore.Count > 0)
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
                        pullingThisFrame = true;
                        CancelPendingRestore();

                        if (currentTarget == null || !IsTargetStillValid(currentTarget))
                            currentTarget = FindBestTarget();

                        if (currentTarget != null && IsTargetStillValid(currentTarget))
                            ApplyStableLock(currentTarget);
                        else
                        {
                            currentTarget = null;
                            ClearLockState();
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

                int delay = Config.EnemyPullTickMs > 0 ? Config.EnemyPullTickMs : 2;
                if (pullingThisFrame)
                    delay = 0;
                if (delay > 0)
                    Thread.Sleep(delay);
            }
        }

        private static bool IsTargetStillValid(Entity entity)
        {
            if (entity == null || entity.IsDead) return false;
            if (!entity.IsKnown) return false;
            if (Config.IgnoreKnocked && entity.IsKnocked) return false;
            if (!Core.Entities.ContainsKey(entity.Address)) return false;

            float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
            if (dist3D > Config.EnemyPullMaxDistance) return false;

            return true;
        }

        private static Entity FindBestTarget()
        {
            Entity bestTarget = null;
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

        private static Vector3 ComputeLockPosition(Vector3 currentPos, Vector3 originalPos)
        {
            Vector3 camPos = Core.LocalMainCamera;

            Vector3 fireDirection = new Vector3(
                Core.CameraMatrix.M13,
                Core.CameraMatrix.M23,
                Core.CameraMatrix.M33
            );
            if (fireDirection.LengthSquared() < 1e-8f)
                return currentPos;
            fireDirection = Vector3.Normalize(fireDirection);

            // Crosshair-head pull: enemy HEAD goes exactly to your crosshair point
            // (any side, up/down allowed). Head-anchored via caller rebase.
            Vector3 toEnemy = currentPos - camPos;
            float projLength = Vector3.Dot(toEnemy, fireDirection);
            if (projLength < MinProjDistance)
                projLength = MinProjDistance;

            Vector3 targetPos = camPos + fireDirection * projLength;

            // Sanity: finite numbers + clamp so a stale read can't fling anyone across the map.
            if (!float.IsFinite(targetPos.X + targetPos.Y + targetPos.Z))
                return currentPos;
            Vector3 delta = targetPos - originalPos;
            const float MaxPull = 60f;
            if (delta.Length() > MaxPull)
                targetPos = originalPos + Vector3.Normalize(delta) * MaxPull;

            return targetPos;
        }

        private static void ApplyStableLock(Entity entity)
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
                uint eid = entity.Address;

                if (!_hasLockedPos || _lockedEntityId != eid)
                {
                    // Head-accurate: measure from the HEAD (not root/chest), so the pull lands
                    // the enemy head on the crosshair line; apply the same XZ delta to the root.
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
                    _lockedPullPos = ComputeLockPosition(anchor, originalPos + (anchor - currentPos));
                    // Rebase back to root space (head and root move together).
                    _lockedPullPos -= (anchor - currentPos);
                    _lockedEntityId = eid;
                    _hasLockedPos = true;
                }

                ulong writeAddr = (ulong)(matrixPtr + 0x60);
                for (int i = 0; i < LockWritesPerTick; i++)
                    InternalMemory.Write(writeAddr, _lockedPullPos);
            }
            catch { }
        }

        private static void ClearLockState()
        {
            _hasLockedPos = false;
            _lockedEntityId = 0;
            _lockedPullPos = Vector3.Zero;
        }

        private static void ScheduleDelayedRestore()
        {
            var deadline = DateTime.Now.AddMilliseconds(RestoreDelayMs);
            foreach (var kv in originalPositions)
                _pendingRestore[kv.Key] = (kv.Value, deadline);
            originalPositions.Clear();
            ClearLockState();
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
            ClearLockState();
        }
    }
}
