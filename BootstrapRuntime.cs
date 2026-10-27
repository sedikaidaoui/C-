using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client;

namespace AotForms
{
    public static class BootstrapRuntime
    {
        public static IntPtr MainWindowHandle { get; set; }


        static BootstrapRuntime()
        {
        }

        public static IntPtr FindRenderWindow(IntPtr parent)
        {
            IntPtr renderWindow = IntPtr.Zero;
            WinAPI.EnumChildWindows(parent, (hWnd, lParam) =>
            {
                var sb = new StringBuilder(256);
                WinAPI.GetWindowText(hWnd, sb, sb.Capacity);
                string windowName = sb.ToString();
                if (!string.IsNullOrEmpty(windowName) && windowName != "HD-Player")
                    renderWindow = hWnd;
                return true;
            }, IntPtr.Zero);
            return renderWindow;
        }

        public static async Task<string?> InitEmulatorAsync()
        {
            var processes = Process.GetProcessesByName("HD-Player");
            if (processes.Length == 0)
                return "Open emulator.";

            var proc = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero) ?? processes[0];
            string? mainModulePath = Path.GetDirectoryName(proc.MainModule?.FileName);
            if (string.IsNullOrEmpty(mainModulePath))
                return "Reinstall emulator.";

            var adbPath = Path.Combine(mainModulePath, "HD-Adb.exe");
            if (!File.Exists(adbPath))
                return "Adb not Found. Reinstall emulator.";

            var adb = new Adb(adbPath);
            await adb.Kill();
            if (!await adb.Start())
                return "ADB failed to start. Restart emulator and try again.";

            // Support both standard Free Fire and Free Fire MAX
            var moduleAddr = await adb.FindModule("com.dts.freefireth", "libil2cpp.so");
            if (moduleAddr == 0)
                moduleAddr = await adb.FindModule("com.dts.freefiremax", "libil2cpp.so");

            if (moduleAddr == 0)
                return "Go to lobby, logout once, then try again.";

            Offsets.Il2Cpp = moduleAddr;
            IntPtr winHandle = MainWindowHandle != IntPtr.Zero ? MainWindowHandle : proc.MainWindowHandle;
            Core.Handle = FindRenderWindow(winHandle);
            return null;
        }

        public static async Task StartNINZAAsync()
        {
            new Thread(Data.Work) { IsBackground = true }.Start();
            new Thread(ShurikenMultiDamage.Work) { IsBackground = true }.Start();
            new Thread(FpsBoost.Work) { IsBackground = true }.Start();
            new Thread(AimbotV2.Work) { IsBackground = true }.Start();
            new Thread(AimbotAi.Work) { IsBackground = true }.Start();
            new Thread(RefreshESPWorker.Work) { IsBackground = true }.Start();
            new Thread(SafeSilent.Work) { IsBackground = true }.Start();
            new Thread(KCBRUTALSILENT.Work) { IsBackground = true }.Start();
            new Thread(UpPlayer.Work) { IsBackground = true }.Start();
            new Thread(Tele.Work) { IsBackground = true }.Start();
            new Thread(ShakeKill.Work) { IsBackground = true }.Start();
            new Thread(Telport.Work) { IsBackground = true }.Start();
            new Thread(KCSPAWNKIL.Work) { IsBackground = true }.Start();
            new Thread(ClimbUp.Start) { IsBackground = true }.Start();
            new Thread(DownPlayer.Work) { IsBackground = true }.Start();
            new Thread(EnemyPull360.Start) { IsBackground = true }.Start();
            new Thread(EnemyPull360v2.Start) { IsBackground = true }.Start();
            new Thread(EnemyPullX.Start) { IsBackground = true }.Start();
            new Thread(EnemyPullXY.Start) { IsBackground = true }.Start();
            new Thread(fprceaim.Start) { IsBackground = true }.Start();
            new Thread(undergroundkill.Work) { IsBackground = true }.Start();
            new Thread(MagnetPull.Start) { IsBackground = true }.Start();
            new Thread(KCMap_Teleport.Work) { IsBackground = true }.Start();
            new Thread(Teleportwall.Work) { IsBackground = true }.Start();
            new Thread(fkyhackint.Work) { IsBackground = true }.Start();
            new Thread(FlyHack80x.Start) { IsBackground = true }.Start();
            new Thread(FLYHACKX40.Work) { IsBackground = true }.Start();
            new Thread(KCClimbUpV2.Start) { IsBackground = true }.Start();
            new Thread(FlyHack50x.Start) { IsBackground = true }.Start();
            await Task.Delay(1000);
        }
    }
}
