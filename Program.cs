using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AotForms
{
    /// <summary>
    /// Font paths aligned with CounterStrike2 overlay (Program.cs): tahomabd.ttf + fonts/fa-solid-900.ttf merge.
    /// </summary>
    internal static class UiFonts
    {
        /// <summary>C:\Windows\Fonts\tahomabd.ttf — base font for UI text, header, labels.</summary>
        public static string TahomabdPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "tahomabd.ttf");

        /// <summary>Next to Client.dll: fonts\fa-solid-900.ttf (Font Awesome merge, same relative path as CounterStrike2).</summary>
        public static string FaSolidPath =>
            Path.Combine(AppContext.BaseDirectory, "fonts", "fa-solid-900.ttf");
    }

    internal static class Program
    {
        static Program()
        {
            try
            {
                // Register DLL Import resolver so cimgui.dll and AotBst.dll are always resolved properly,
                // eliminating System.DllNotFoundException (0x80131524) crashes.
                NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, ResolveDll);
            }
            catch { }
        }

        private static IntPtr ResolveDll(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
        {
            try
            {
                IntPtr hMod = WinAPI.GetModuleHandle(libraryName);
                if (hMod != IntPtr.Zero) return hMod;

                string fileName = libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    ? libraryName
                    : libraryName + ".dll";

                string[] searchDirs = new[]
                {
                    AppContext.BaseDirectory,
                    Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "",
                    @"C:\Windows\Temp",
                    @"C:\Program Files\BlueStacks_msi5",
                    @"c:\Users\Administrator\Documents\WORKING OB54\AotForms\bin\Release\net7.0-windows7.0\win-x64\publish",
                    @"c:\Users\Administrator\Documents\WORKING OB54\AotForms\bin\Release\net7.0-windows7.0\win-x64\publish\INTERNAL",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                };

                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    string fullPath = Path.Combine(dir, fileName);
                    if (File.Exists(fullPath))
                    {
                        if (NativeLibrary.TryLoad(fullPath, out IntPtr handle))
                            return handle;
                    }
                }
            }
            catch { }

            return IntPtr.Zero;
        }

        [UnmanagedCallersOnly(EntryPoint = "Load")]
        public static void Load(nint pVM)
        {
            try
            {
                // Preload required native dependencies
                ResolveDll("cimgui.dll", typeof(Program).Assembly, null);
                ResolveDll("AotBst.dll", typeof(Program).Assembly, null);
            }
            catch { }

            if (pVM != 0)
            {
                try { InternalMemory.Initialize(pVM); } catch { }
            }
            
            var process = Process.GetCurrentProcess();
            try { ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance); } catch { }
            try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { }
            try { Application.EnableVisualStyles(); } catch { }

            try { BootstrapRuntime.MainWindowHandle = process.MainWindowHandle; } catch { }
            try { System.AppDomain.CurrentDomain.ProcessExit += (_, __) => DiscordPresence.Shutdown(); } catch { }
            try { DiscordPresence.Initialize(); } catch { }
            var uiThread = new Thread(() =>
            {
                try
                {
                    new ESP().Run();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            })
            {
                IsBackground = false
            };
            try { uiThread.SetApartmentState(ApartmentState.STA); } catch { }
            uiThread.Start();
        }
    }
}
