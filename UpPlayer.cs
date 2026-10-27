using AotForms;
using Memory;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal static class UpPlayer
    {
        private static Task upTask;
        private static CancellationTokenSource cts = new();
        private static bool isRunning = false;

        private static readonly Dictionary<uint, Vector3> originalPositions = new();
        private const float LiftStep = 0.09f;
        private const float MaxLift = 30f;

        internal static void Work()
        {
            if (isRunning) return;
            isRunning = true;

            upTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Config.UpPlayer)
                    {
                        RestoreAll();
                        await Task.Delay(5, cts.Token);
                        continue;
                    }

                    if (Core.Width == -1 || Core.Height == -1 || !Core.HaveMatrix)
                    {
                        await Task.Delay(5, cts.Token);
                        continue;
                    }

                    // Enemy only: lift while firing, restore on release.
                    if (!InternalMemory.Read(Core.LocalPlayer + Offsets.pomba, out bool isFiring) || !isFiring)
                    {
                        RestoreAll();
                        await Task.Delay(5, cts.Token);
                        continue;
                    }

                    foreach (var entity in Core.Entities.Values)
                    {
                        if (entity.IsDead || entity.IsKnocked || !entity.IsKnown)
                            continue;

                        var head2D = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                        if (head2D.X < 1 || head2D.Y < 1)
                            continue;

                        if (!IsInsideFOV((int)head2D.X, (int)head2D.Y))
                            continue;

                        try
                        {

                            if (!InternalMemory.Read(entity.Address + (uint)Bones.Root, out uint enemyRootBonePtr)) continue;
                            if (!InternalMemory.Read(enemyRootBonePtr + 0x8, out uint enemyTransformValue)) continue;
                            if (!InternalMemory.Read(enemyTransformValue + 0x8, out uint enemyTransformObjPtr)) continue;
                            if (!InternalMemory.Read(enemyTransformObjPtr + 0x20, out uint enemyMatrixValue)) continue;

                            if (!InternalMemory.Read(enemyMatrixValue + 0x60, out Vector3 enemyPos)) continue;

                            if (!originalPositions.ContainsKey(entity.Address))
                                originalPositions[entity.Address] = enemyPos;

                            Vector3 orig = originalPositions[entity.Address];
                            if (enemyPos.Y - orig.Y < MaxLift)
                            {
                                enemyPos.Y += LiftStep;
                                InternalMemory.Write(enemyMatrixValue + 0x60, enemyPos);
                                InternalMemory.Write(enemyMatrixValue + 0x60, enemyPos);
                            }
                        }
                        catch
                        {

                        }
                    }

                    await Task.Delay(1, cts.Token);
                }
            }, cts.Token);
        }

        private static void RestoreAll()
        {
            if (originalPositions.Count == 0) return;
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
        }

        private static bool IsInsideFOV(int x, int y)
        {
            int fov = 1500;
            int centerX = Core.Width / 2;
            int centerY = Core.Height / 2;
            return (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= fov * fov;
        }

        internal static void Stop()
        {
            if (!isRunning) return;

            cts.Cancel();
            cts = new CancellationTokenSource();
            RestoreAll();
            isRunning = false;
        }
    }
}
