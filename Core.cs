using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AotForms
{
    internal static class Core
    {

        private static Matrix4x4 _cameraMatrix;
        internal static Vector3 CameraForward => _cameraForward;
        internal static Vector3 CameraRight => _cameraRight;
        private static Vector3 _cameraForward;
        private static Vector3 _cameraRight;
        internal static uint LibUnity;

        internal static IntPtr Handle;
        internal static int Width = -1;
        internal static int Height = -1;
        internal static bool HaveMatrix = false;
        internal static Matrix4x4 CameraMatrix;
        internal static ulong LocalPlayer;
        internal static Vector3 LocalMainCamera;
        internal static DateTime LastDataUpdate;
        public static ConcurrentDictionary<long, Entity> Entities = new();
        public static ConcurrentDictionary<long, LootItem> LootItems = new();

    }
}
