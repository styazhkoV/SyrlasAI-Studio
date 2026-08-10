using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyrlasStudio.Services;

public sealed class ResourceMonitorService : IDisposable
{
    public event Action<double, double, double, double>? MetricsUpdated;

    private readonly CancellationTokenSource _cts = new();
    private readonly Process _process;
    private DateTime _lastTime;
    private TimeSpan _lastTotalProcessorTime;
    private bool _disposed;

    public ResourceMonitorService()
    {
        _process = Process.GetCurrentProcess();
        _lastTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _process.TotalProcessorTime;
        
        Task.Run(() => MonitorLoop(_cts.Token));
    }

    private async Task MonitorLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _process.Refresh();

                double ramMb = _process.WorkingSet64 / (1024.0 * 1024.0);
                var currentTime = DateTime.UtcNow;
                var currentTotalProcessorTime = _process.TotalProcessorTime;
                double cpuUsage = 0;

                var timeDelta = (currentTime - _lastTime).TotalMilliseconds;
                if (timeDelta > 0)
                {
                    var cpuDelta = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                    cpuUsage = (cpuDelta / (Environment.ProcessorCount * timeDelta)) * 100.0;
                }

                _lastTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                double diskUsage = 0;
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
                    double total = drive.TotalSize;
                    double free = drive.TotalFreeSpace;
                    diskUsage = ((total - free) / total) * 100.0;
                }
                catch { }

                double vramMb = GetNvidiaVramUsage();

                MetricsUpdated?.Invoke(cpuUsage, ramMb, diskUsage, vramMb);
            }
            catch { }

            try
            {
                await Task.Delay(3000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private double GetNvidiaVramUsage()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.used --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                // Если процесс завис и не завершился за 500мс — жестко убиваем
                if (process.WaitForExit(500))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    if (double.TryParse(output.Trim(), out double usedMb))
                    {
                        return usedMb;
                    }
                }
                else
                {
                    process.Kill();
                }
            }
        }
        catch { }
        return 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _cts.Cancel();
        _cts.Dispose();
        _process.Dispose();
        _disposed = true;
    }
}