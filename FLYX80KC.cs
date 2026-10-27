using AotForms;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal static class fkyhackint
    {
        private static Task tpTask;
        private static CancellationTokenSource cts = new();
        private static bool isRunning = false;

        // **30M EXACT REAL-TIME LIMIT**
        private static readonly float MAX_ALTITUDE = 50.0f;  // 30m height limit
        private static float groundY = 0f;                   // Reference ground height
        private static bool isInitialized = false;
        private static int teleportDelay = 1;

        // SnowSlide State tracking
        private static bool originalSlideValue = false;
        private static bool slideValueSaved = false;

        internal static void Work()
        {
            if (isRunning) return;
            isRunning = true;

            if (cts != null && cts.IsCancellationRequested)
                cts = new CancellationTokenSource();
            else if (cts == null)
                cts = new CancellationTokenSource();

            tpTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Config.FLYHACKINTTTT)
                    {
                        if (isInitialized)
                        {
                            RestoreNormalPosition();
                            isInitialized = false;
                            groundY = 0;
                            slideValueSaved = false;
                        }
                        await Task.Delay(500, cts.Token);
                        continue;
                    }

                    try
                    {
                        ulong localPlayer = Core.LocalPlayer;
                        if (localPlayer == 0)
                        {
                            await Task.Delay(10, cts.Token);
                            continue;
                        }

                        // Reverted to OLD logic (+0x60 chain) as requested
                        if (!InternalMemory.Read<uint>(localPlayer + (uint)Bones.Root, out uint rootPtr) || rootPtr == 0) continue;
                        if (!InternalMemory.Read<uint>(rootPtr + 0x8, out uint t1) || t1 == 0) continue;
                        if (!InternalMemory.Read<uint>(t1 + 0x8, out uint t2) || t2 == 0) continue;
                        if (!InternalMemory.Read<uint>(t2 + 0x20, out uint matrixPtr) || matrixPtr == 0) continue;

                        // Mutual Exclusion
                        if (Config.FlyHack80x) Config.FlyHack80x = false;
                        if (Config.FlyHack50x) Config.FlyHack50x = false;
                        if (Config.ClimbUpV2Enabled) Config.ClimbUpV2Enabled = false;

                        // Read CURRENT position from matrix + 0x60
                        if (!InternalMemory.Read<Vector3>(matrixPtr + 0x60, out Vector3 currentPos)) continue;

                        if (!isInitialized)
                        {
                            groundY = currentPos.Y;
                            isInitialized = true;
                        }

                        // If any teleport feature is active, let it handle the height
                        if (Config.TeleportAnywhere || Config.teleportmap || Config.directteleport)
                        {
                            await Task.Delay(10, cts.Token);
                            continue;
                        }

                        float targetAltitude = groundY + MAX_ALTITUDE;
                        Vector3 targetPos = currentPos;
                        targetPos.Y = targetAltitude;

                        // Write to matrix (Visual & Physics)
                        InternalMemory.Write<Vector3>(matrixPtr + 0x60, targetPos);
                        InternalMemory.Write<float>(matrixPtr + 0x60 + 0xC, 1.0f);

                        // 3. Prevent Falling / Gravity (SnowSlide Dashing Hack)
                        if (Offsets.InSnowSlideWayDashing != 0)
                        {
                            if (!slideValueSaved)
                            {
                                if (InternalMemory.Read<bool>(localPlayer + Offsets.InSnowSlideWayDashing, out originalSlideValue))
                                {
                                    slideValueSaved = true;
                                }
                            }
                            InternalMemory.Write<bool>(localPlayer + Offsets.InSnowSlideWayDashing, true);
                        }
                    }
                    catch { }

                    await Task.Delay(teleportDelay, cts.Token);
                }
            }, cts.Token);
        }

        private static void RestoreNormalPosition()
        {
            try
            {
                ulong localPlayer = Core.LocalPlayer;
                if (localPlayer == 0) return;

                // Restore Matrix Position
                if (InternalMemory.Read<uint>(localPlayer + (uint)Bones.Root, out uint rootPtr) &&
                    InternalMemory.Read<uint>(rootPtr + 0x8, out uint t1) &&
                    InternalMemory.Read<uint>(t1 + 0x8, out uint t2) &&
                    InternalMemory.Read<uint>(t2 + 0x20, out uint matrixPtr) && matrixPtr != 0)
                {
                    if (InternalMemory.Read<Vector3>(matrixPtr + 0x60, out Vector3 cur))
                    {
                        cur.Y = groundY;
                        InternalMemory.Write<Vector3>(matrixPtr + 0x60, cur);
                    }
                }

                // Restore Dashing State
                if (slideValueSaved && Offsets.InSnowSlideWayDashing != 0)
                {
                    InternalMemory.Write<bool>(localPlayer + Offsets.InSnowSlideWayDashing, originalSlideValue);
                }
            }
            catch { }
        }

        internal static void Stop()
        {
            if (!isRunning) return;
            try { cts.Cancel(); } catch { }
            RestoreNormalPosition();
            isRunning = false;
            isInitialized = false;
            groundY = 0;
        }

        internal static void SetTeleportDelay(int delay) => teleportDelay = delay;
    }
}