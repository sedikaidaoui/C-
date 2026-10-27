using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace AotForms
{
    internal class DownPlayer
    {
        private static bool _isRunning = false;
        private static Thread _DownPlayerThread;
        
        // Enemy down lock system (enemy ONLY — never touches local player)
        private static readonly Dictionary<uint, Vector3> _enemyOriginalPositions = new();
        private static Entity currentEnemyTarget = null;
        private const float enemyDownLockDistance = 2.1f;
        private const float undergroundEnemyDownDistance = 2.6f;
        private const int enemyLockUpdateInterval = 1; // Fast update for stability

        internal static void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            Config.DownPlayer = true;
            _DownPlayerThread = new Thread(Work)
            {
                IsBackground = true
            };
            _DownPlayerThread.Start();
        }

        internal static void Stop()
        {
            _isRunning = false;
            Config.DownPlayer = false;
            RestoreAllEnemyPositions();
            currentEnemyTarget = null;
        }

        private static bool IsLocalFiring()
        {
            try
            {
                bool value;
                if (InternalMemory.Read<bool>(Core.LocalPlayer + Offsets.IsFiring, out value))
                    return value;
                return false;
            }
            catch { return false; }
        }

        private static Entity FindBestEnemyTarget()
        {
            Entity bestTarget = null;
            float closestDist = float.MaxValue;
            var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

            foreach (var entity in Core.Entities.Values)
            {
                if (!entity.IsKnown || entity.IsDead) continue;
                if (entity.IsKnocked) continue;

                var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                if (head2D.X < 1 || head2D.Y < 1 || head2D.X > Core.Width || head2D.Y > Core.Height) continue;

                float dist3D = Vector3.Distance(Core.LocalMainCamera, entity.Head);
                if (dist3D > 200f) continue;

                float crosshairDist = Vector2.Distance(screenCenter, head2D);
                if (crosshairDist > Config.AimFovCircle) continue;

                if (crosshairDist < closestDist)
                {
                    closestDist = crosshairDist;
                    bestTarget = entity;
                }
            }

            return bestTarget;
        }

        private static void ApplyEnemyDownLock(Entity entity)
        {
            try
            {
                if (!InternalMemory.Read<uint>(entity.Address + (uint)Bones.Root, out uint rootBonePtr) || rootBonePtr == 0) return;
                if (!InternalMemory.Read<uint>(rootBonePtr + 0x8, out uint transformValue) || transformValue == 0) return;
                if (!InternalMemory.Read<uint>(transformValue + 0x8, out uint transformObjPtr) || transformObjPtr == 0) return;
                if (!InternalMemory.Read<uint>(transformObjPtr + 0x20, out uint matrixPtr) || matrixPtr == 0) return;

                // Read current position
                if (!InternalMemory.Read<Vector3>(matrixPtr + 0x60, out Vector3 currentPos)) return;

                // Store original position on first lock
                if (!_enemyOriginalPositions.ContainsKey(entity.Address))
                {
                    _enemyOriginalPositions[entity.Address] = currentPos;
                }

                Vector3 originalPos = _enemyOriginalPositions[entity.Address];

                float downDist = Config.undergroundkill ? undergroundEnemyDownDistance : enemyDownLockDistance;
                Vector3 lockedPos = originalPos;
                lockedPos.Y = originalPos.Y - downDist;

                InternalMemory.Write<Vector3>(matrixPtr + 0x60, lockedPos);
            }
            catch { }
        }

        private static void RestoreEnemyPosition(uint entityAddress)
        {
            if (!_enemyOriginalPositions.ContainsKey(entityAddress)) return;

            try
            {
                if (!InternalMemory.Read<uint>(entityAddress + (uint)Bones.Root, out uint rootBonePtr) || rootBonePtr == 0) return;
                if (!InternalMemory.Read<uint>(rootBonePtr + 0x8, out uint transformValue) || transformValue == 0) return;
                if (!InternalMemory.Read<uint>(transformValue + 0x8, out uint transformObjPtr) || transformObjPtr == 0) return;
                if (!InternalMemory.Read<uint>(transformObjPtr + 0x20, out uint matrixPtr) || matrixPtr == 0) return;

                InternalMemory.Write<Vector3>(matrixPtr + 0x60, _enemyOriginalPositions[entityAddress]);
                _enemyOriginalPositions.Remove(entityAddress);
            }
            catch { }
        }

        private static void RestoreAllEnemyPositions()
        {
            foreach (var entry in _enemyOriginalPositions)
            {
                try
                {
                    if (!InternalMemory.Read<uint>(entry.Key + (uint)Bones.Root, out uint rootBonePtr) || rootBonePtr == 0) continue;
                    if (!InternalMemory.Read<uint>(rootBonePtr + 0x8, out uint transformValue) || transformValue == 0) continue;
                    if (!InternalMemory.Read<uint>(transformValue + 0x8, out uint transformObjPtr) || transformObjPtr == 0) continue;
                    if (!InternalMemory.Read<uint>(transformObjPtr + 0x20, out uint matrixPtr) || matrixPtr == 0) continue;

                    InternalMemory.Write<Vector3>(matrixPtr + 0x60, entry.Value);
                }
                catch { }
            }

            _enemyOriginalPositions.Clear();
        }

        internal static void Work()
        {
            while (true)
            {
                try
                {
                    if (!Config.DownPlayer && !Config.undergroundkill)
                    {
                        if (currentEnemyTarget != null)
                        {
                            RestoreEnemyPosition(currentEnemyTarget.Address);
                            currentEnemyTarget = null;
                        }
                        RestoreAllEnemyPositions();
                        Thread.Sleep(enemyLockUpdateInterval);
                        continue;
                    }

                    if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                    {
                        Thread.Sleep(enemyLockUpdateInterval);
                        continue;
                    }

                    if (!IsLocalFiring())
                    {
                        // Fire released - restore all enemy positions
                        if (currentEnemyTarget != null)
                        {
                            RestoreEnemyPosition(currentEnemyTarget.Address);
                            currentEnemyTarget = null;
                        }
                        RestoreAllEnemyPositions();
                        Thread.Sleep(enemyLockUpdateInterval);
                        continue;
                    }

                    // Maintain lock if valid
                    if (currentEnemyTarget != null)
                    {
                        if (currentEnemyTarget.IsDead || currentEnemyTarget.IsKnocked || !currentEnemyTarget.IsKnown)
                        {
                            RestoreEnemyPosition(currentEnemyTarget.Address);
                            currentEnemyTarget = null;
                        }
                        else
                        {
                            float dist3D = Vector3.Distance(Core.LocalMainCamera, currentEnemyTarget.Head);
                            if (dist3D > 200f)
                            {
                                RestoreEnemyPosition(currentEnemyTarget.Address);
                                currentEnemyTarget = null;
                            }
                        }
                    }

                    // Find new target if not locked
                    if (currentEnemyTarget == null)
                        currentEnemyTarget = FindBestEnemyTarget();

                    // Apply down lock to enemy
                    if (currentEnemyTarget != null)
                        ApplyEnemyDownLock(currentEnemyTarget);
                }
                catch { }

                Thread.Sleep(enemyLockUpdateInterval);
            }

            // Restore all positions when stopping
            RestoreAllEnemyPositions();
        }
    }
}
