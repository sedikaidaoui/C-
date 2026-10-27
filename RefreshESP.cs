using System;
using System.Threading;

namespace AotForms
{
    internal static class RefreshESPWorker
    {
        internal static void Work()
        {
            while (true)
            {
                if (Config.RefreshESP)
                {
                    try
                    {
                        Core.Entities = new();
                        InternalMemory.Cache = new();
                    }
                    catch { }
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
}
