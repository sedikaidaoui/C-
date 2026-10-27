using DiscordRPC;
using DiscordRPC.Logging;
using System.Threading;

namespace AotForms
{
    /// <summary>
    /// Discord Rich Presence (local RPC, no bot token).
    /// App: R!IGLO CHEAT INTERNAL (1558483564629200927).
    /// Details = panel title, State = KeyAuth username (live, updates on login).
    /// Safe when Discord is closed: retries quietly in the background.
    /// </summary>
    internal static class DiscordPresence
    {
        internal const string AppId = "1558483564629200927";
        internal const string PanelTitle = "RIGLO CHEAT INTERNAL PANEL";
        internal const string LargeImageKey = "logo";
        internal const string LargeImageText = "R!IGLO CHEAT INTERNAL";
        internal const string InviteUrl = "https://discord.gg/xEgpAVJSVe";

        private static DiscordRpcClient? client;
        private static Thread? _updateThread;
        private static volatile bool _running = false;
        private static string _lastState = "";
        private static string _lastLog = "";
        private static readonly object Gate = new object();

        private static void LogRpc(string msg)
        {
            if (!Config.DebugLogging) return;
            try
            {
                if (msg == _lastLog) return;
                _lastLog = msg;
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "ob54_rpc.txt"), DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
            }
            catch { }
        }

        public static void Initialize()
        {
            try
            {
                lock (Gate)
                {
                    if (_running) return;
                    _running = true;
                }
                _updateThread = new Thread(UpdateLoop) { IsBackground = true };
                _updateThread.Start();
            }
            catch { }
        }

        private static void UpdateLoop()
        {
            while (_running)
            {
                try
                {
                    lock (Gate)
                    {
                        if (client == null)
                        {
                            client = new DiscordRpcClient(AppId)
                            {
                                Logger = new ConsoleLogger { Level = LogLevel.Warning }
                            };
                            client.Initialize();
                            LogRpc("connected to Discord RPC");
                        }
                    }

                    string user = KeyAuthSession.Username;
                    string state = string.IsNullOrEmpty(user) ? "Waiting for KeyAuth login" : user;
                    if (state != _lastState)
                    {
                        SetPresence(PanelTitle, state);
                        _lastState = state;
                        LogRpc("presence set state=[" + state + "]");
                    }
                }
                catch (System.Exception ex)
                {
                    LogRpc("rpc error: " + ex.GetType().Name + " " + ex.Message);
                    DropClient();
                }

                Thread.Sleep(15000);
            }
        }

        private static void SetPresence(string details, string state)
        {
            var c = client;
            if (c == null) return;
            c.SetPresence(new RichPresence
            {
                Details = details,
                State = state,
                Timestamps = Timestamps.Now,
                Assets = new Assets
                {
                    LargeImageKey = LargeImageKey,
                    LargeImageText = LargeImageText
                },
                Buttons = new[]
                {
                    new DiscordRPC.Button
                    {
                        Label = "Join Discord",
                        Url = InviteUrl
                    }
                }
            });
        }

        private static void DropClient()
        {
            try
            {
                lock (Gate)
                {
                    try { client?.Dispose(); } catch { }
                    client = null;
                }
            }
            catch { }
        }

        public static void Shutdown()
        {
            _running = false;
            try
            {
                lock (Gate)
                {
                    try { client?.ClearPresence(); } catch { }
                    try { client?.Dispose(); } catch { }
                    client = null;
                }
            }
            catch { }
        }
    }
}
