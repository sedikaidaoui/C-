namespace AotForms
{
    /// <summary>
    /// Read-only bridge to the KeyAuth session (Reborn.api).
    /// The login code calls Attach once after a successful login; DiscordPresence
    /// then picks up Username automatically. Never hardcoded, never modified KeyAuth.
    /// </summary>
    internal static class KeyAuthSession
    {
        private static Reborn.api? _instance;
        private static readonly object Gate = new object();

        internal static void Attach(Reborn.api? app)
        {
            lock (Gate) { _instance = app; }
        }

        internal static void Detach()
        {
            lock (Gate) { _instance = null; }
        }

        internal static string Username
        {
            get
            {
                try
                {
                    lock (Gate)
                    {
                        return _instance?.user_data?.username ?? "";
                    }
                }
                catch { return ""; }
            }
        }
    }
}
