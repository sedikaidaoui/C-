using System.Diagnostics;
using System.Text;
using System.Collections.Concurrent;

namespace AotForms
{
    internal class Adb
    {
        string _path;
        ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();
        Process? _process;

        internal Adb(string path)
        {
            _path = path;
        }

        // Graceful reset: Never execute 'kill-server' because BlueStacks relies on
        // its background ADB daemon. Killing it severs the emulator bridge and kicks you out!
        // Only our own session process handle is terminated.
        internal async Task Kill()
        {
            await Task.Run(() =>
            {
                try
                {
                    var self = _process;
                    if (self != null && !self.HasExited)
                    {
                        try { self.Kill(); } catch { }
                    }
                }
                catch { }
            });
        }

        internal async Task<bool> Start()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    ExecuteAdbCommand("devices");

                    _process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _path,
                            Arguments = "shell \"getprop ro.secure ; /boot/android/android/system/xbin/bstk/su || su || sh\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        },
                        EnableRaisingEvents = true
                    };
                    _process.OutputDataReceived += Receiver;
                    _process.ErrorDataReceived += Receiver;

                    _process.Start();
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    _process.StandardInput.AutoFlush = true;

                    // Wait for output with timeout
                    for (int i = 0; i < 5000; i++)  // Max wait: 5 sec
                    {
                        if (!_outputQueue.IsEmpty) return true;
                        await Task.Delay(1);
                    }
                }
                catch { }

                return false;
            });
        }

        void Receiver(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                _outputQueue.Enqueue(e.Data);
        }

        internal async Task<uint> FindModule(string process, string module)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var proc = _process;
                    if (proc == null || proc.HasExited) return 0U;

                    _outputQueue = new ConcurrentQueue<string>();

                    proc.StandardInput.WriteLine($"pidof {process} || ps -A | grep {process} || ps | grep {process}");

                    string? pid = await WaitForOutputAndExtractPID(process);
                    if (string.IsNullOrEmpty(pid)) return 0U;

                    if (proc.HasExited) return 0U;
                    _outputQueue = new ConcurrentQueue<string>();
                    proc.StandardInput.WriteLine($"cat /proc/{pid}/maps | grep {module}");

                    string? address = await WaitForOutput(module);
                    if (string.IsNullOrEmpty(address)) return 0U;

                    try { proc.Kill(); } catch { }
                    try { await proc.WaitForExitAsync(); } catch { }

                    string hexStr = address.Split('-')[0].Trim();
                    return Convert.ToUInt32(hexStr, 16);
                }
                catch
                {
                    return 0U;
                }
            });
        }

        async Task<string?> WaitForOutput(string expectedSubstring)
        {
            for (int i = 0; i < 5000; i++)  // Max wait: 5 sec
            {
                if (_outputQueue.TryDequeue(out string? result) && result != null)
                {
                    if (result.Contains("-") && (string.IsNullOrEmpty(expectedSubstring) || result.Contains(expectedSubstring)))
                        return result;
                }
                await Task.Delay(1);
            }
            return null;
        }

        async Task<string?> WaitForOutputAndExtractPID(string process)
        {
            for (int i = 0; i < 5000; i++)
            {
                if (_outputQueue.TryDequeue(out string? result) && result != null)
                {
                    var procLines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawLine in procLines)
                    {
                        var procLine = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(procLine)) continue;

                        // Case 1: Pure PID output from pidof
                        var tokens = procLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length > 0 && int.TryParse(tokens[0], out int numericPid) && numericPid > 100)
                        {
                            return tokens[0];
                        }

                        // Case 2: Process list line containing package name
                        if (procLine.Contains(process))
                        {
                            if (tokens.Length > 1 && int.TryParse(tokens[1], out _)) return tokens[1];
                            if (tokens.Length > 0 && int.TryParse(tokens[0], out _)) return tokens[0];
                        }
                    }
                }
                await Task.Delay(1);
            }
            return null;
        }

        void ExecuteAdbCommand(string command)
        {
            try
            {
                using (var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _path,
                        Arguments = command,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true
                    }
                })
                {
                    proc.Start();
                    proc.WaitForExit(3000);
                }
            }
            catch { }
        }
    }
}
