using System;
using System.Diagnostics;
using System.Threading;

namespace AotForms
{
    /// <summary>
    /// FPS Boost: gently raises the emulator process priority so the game gets more CPU.
    /// AboveNormal only (never High/Realtime) to avoid watchdog kicks. Restores Normal when off.
    /// </summary>
    internal static class FpsBoost
    {
        private static bool applied;

        internal static void Work()
        {
            while (true)
            {
                try
                {
                    if (Config.FpsBoost)
                    {
                        if (!applied)
                        {
                            try
                            {
                                foreach (var p in Process.GetProcessesByName("HD-Player"))
                                {
                                    try { p.PriorityClass = ProcessPriorityClass.AboveNormal; } catch { }
                                }
                            }
                            catch { }
                            applied = true;
                        }
                    }
                    else if (applied)
                    {
                        try
                        {
                            foreach (var p in Process.GetProcessesByName("HD-Player"))
                            {
                                try { p.PriorityClass = ProcessPriorityClass.Normal; } catch { }
                            }
                        }
                        catch { }
                        applied = false;
                    }
                }
                catch { }

                Thread.Sleep(2000);
            }
        }
    }
}
