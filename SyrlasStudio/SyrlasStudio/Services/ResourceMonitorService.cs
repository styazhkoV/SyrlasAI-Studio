using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyrlasStudio.Services;

public class ResourceMonitorService
{
    // Событие передает: CPU %, RAM MB, Disk %, VRAM MB
    public event Action<double, double, double, double>? MetricsUpdated;

    private readonly CancellationTokenSource _cts = new();
    private readonly Process _process;
    private DateTime _lastTime;
    private TimeSpan _lastTotalProcessorTime;

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

                // 1. RAM (MB)
                double ramMb = _process.WorkingSet64 / (1024.0 * 1024.0);

                // 2. CPU (%)
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

                // 3. Disk (%)
                double diskUsage = 0;
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
                    double total = drive.TotalSize;
                    double free = drive.TotalFreeSpace;
                    diskUsage = ((total - free) / total) * 100.0;
                }
                catch { }

                // 4. VRAM (MB) через nvidia-smi
                double vramMb = GetNvidiaVramUsage();

                MetricsUpdated?.Invoke(cpuUsage, ramMb, diskUsage, vramMb);
            }
            catch { }

            await Task.Delay(3000, token);
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
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(500);
                if (double.TryParse(output.Trim(), out double usedMb))
                {
                    return usedMb;
                }
            }
        }
        catch { }
        return 0;
    }
}