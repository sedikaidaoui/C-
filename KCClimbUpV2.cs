using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace AotForms
{
    /// <summary>
    /// Climb Up V2 implementation.
    /// Uses the +0x60 matrix chain and SnowSlide Dashing hack for gravity bypass.
    /// Height is dynamically controlled by a slider value from Config.
    /// </summary>
    internal static class KCClimbUpV2
    {
        private static bool _isRunning = false;
        private static float _groundY = 0f;
        private static bool _isInitialized = false;
        
        // SnowSlide State tracking for gravity bypass
        private static bool _originalSlideValue = false;
        private static bool _slideValueSaved = false;

        internal static void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(() => Loop());
        }

        private static void Loop()
        {
            while (true)
            {
                try
                {
                    if (Config.ClimbUpV2Enabled)
                    {
                        ApplyClimb();
                    }
                    else if (_isInitialized)
                    {
                        RestoreState();
                    }
                }
                catch 
                { 
                    // Suppress memory access exceptions
                }

                Thread.Sleep(5);
            }
        }

        private static void ApplyClimb()
        {
            ulong localPlayer = Core.LocalPlayer;
            if (localPlayer == 0) return;

            // Use the OLD matrix logic (+0x60 chain) as requested
            if (!InternalMemory.Read<uint>(localPlayer + (uint)Bones.Root, out uint rootPtr) || rootPtr == 0) return;
            if (!InternalMemory.Read<uint>(rootPtr + 0x8, out uint t1) || t1 == 0) return;
            if (!InternalMemory.Read<uint>(t1 + 0x8, out uint t2) || t2 == 0) return;
            if (!InternalMemory.Read<uint>(t2 + 0x20, out uint matrixPtr) || matrixPtr == 0) return;

            // Mutual Exclusion: Disable other fly hacks to prevent glitches
            if (Config.FlyHack80x) Config.FlyHack80x = false;
            if (Config.FlyHack50x) Config.FlyHack50x = false;
            if (Config.FLYHACKINTTT) Config.FLYHACKINTTT = false;
            if (Config.FLYHACKX40) Config.FLYHACKX40 = false;
            if (Config.FlyHack) Config.FlyHack = false;
            if (Config.ClimbUpEnabled) Config.ClimbUpEnabled = false;

            // Read CURRENT position from matrix + 0x60
            if (!InternalMemory.Read<Vector3>(matrixPtr + 0x60, out Vector3 currentPos)) return;

            if (!_isInitialized)
            {
                _groundY = currentPos.Y;
                _isInitialized = true;
            }

            // Calculate target position based on ground reference and slider
            Vector3 targetPos = currentPos;
            targetPos.Y = _groundY + Config.ClimbUpV2Value;

            // Write to matrix (Visual & Physics)
            InternalMemory.Write<Vector3>(matrixPtr + 0x60, targetPos);
            InternalMemory.Write<float>(matrixPtr + 0x60 + 0xC, 1.0f); // W = 1

            // Prevent Falling / Gravity (SnowSlide Dashing Hack)
            if (Offsets.InSnowSlideWayDashing != 0)
            {
                if (!_slideValueSaved)
                {
                    if (InternalMemory.Read<bool>(localPlayer + Offsets.InSnowSlideWayDashing, out _originalSlideValue))
                    {
                        _slideValueSaved = true;
                    }
                }
                InternalMemory.Write<bool>(localPlayer + Offsets.InSnowSlideWayDashing, true);
            }
        }

        private static void RestoreState()
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
                        cur.Y = _groundY;
                        InternalMemory.Write<Vector3>(matrixPtr + 0x60, cur);
                    }
                }

                // Restore Dashing State
                if (_slideValueSaved && Offsets.InSnowSlideWayDashing != 0)
                {
                    InternalMemory.Write<bool>(localPlayer + Offsets.InSnowSlideWayDashing, _originalSlideValue);
                }
            }
            catch { }

            _isInitialized = false;
            _slideValueSaved = false;
            _groundY = 0;
        }
    }
}
