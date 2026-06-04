/*
 * LocationGlue.exe — Keep Windows 11 "Location In Use" icon always visible.
 * .NET Framework 4.8 version — uses system runtime (~5-10MB, shared across all .NET apps).
 *
 * Uses System.Device.Location.GeoCoordinateWatcher (no WinRT needed).
 */

using System;
using System.Device.Location;
using System.Diagnostics;
using System.Threading;

class LocationGlue
{
    const string TaskName = "LocationGlue";

    static void Main(string[] args)
    {
        string cmd = args.Length > 0 ? args[0].ToLower() : "run";

        switch (cmd)
        {
            case "run":       RunForeground(); break;
            case "install":   Install(); break;
            case "uninstall": Uninstall(); break;
            case "status":    Status(); break;
            default:          PrintUsage(); break;
        }
    }

    static void RunForeground()
    {
        Console.WriteLine("[LG] LocationGlue — keeping location icon always visible");
        Console.WriteLine("[LG] Press Ctrl+C to stop.");

        bool running = true;
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            running = false;
            Console.WriteLine("\n[LG] Shutting down...");
        };

        // Named event so the uninstaller can signal a graceful shutdown
        using (var shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset, @"LocationGlue_Shutdown"))
        using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
        {
            watcher.MovementThreshold = 10000; // 10km — almost never fires
            watcher.StatusChanged += (s, e) =>
            {
                Console.WriteLine("[LG] Status: {0}", e.Status);
            };
            watcher.PositionChanged += (s, e) =>
            {
                // Discard — just keep the session alive
            };

            watcher.Start();
            Console.WriteLine("[LG] Location watcher started (status: {0})", watcher.Status);

            if (watcher.Status == GeoPositionStatus.NoData || watcher.Status == GeoPositionStatus.Ready)
            {
                Console.WriteLine("[LG] Location icon should now be visible.");
            }

            // Keep alive until shutdown signal or Ctrl+C
            while (running)
            {
                // Wait 1 second or until shutdown is signaled by the uninstaller
                int signaled = WaitHandle.WaitAny(new WaitHandle[] { shutdownEvent }, 1000);
                if (signaled == 0)
                {
                    Console.WriteLine("[LG] Shutdown signal received, stopping gracefully...");
                    break;
                }

                // Periodically check if watcher is still healthy
                if (watcher.Status == GeoPositionStatus.Disabled || watcher.Status == GeoPositionStatus.NoData)
                {
                    watcher.Stop();
                    Thread.Sleep(2000);
                    if (shutdownEvent.WaitOne(0)) break; // Don't restart if shutdown was signaled
                    watcher.Start();
                    Console.WriteLine("[LG] Restarted watcher (status was: {0})", watcher.Status);
                }
            }

            watcher.Stop();
        }

        Console.WriteLine("[LG] LocationGlue stopped.");
    }

    static void Install()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = System.IO.Path.Combine(startupDir, "LocationGlue.lnk");

        // Create shortcut via WScript.Shell COM
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        dynamic shell = Activator.CreateInstance(shellType);
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.Arguments = "run";
        shortcut.WindowStyle = 7; // Minimized
        shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);
        shortcut.Save();

        Console.WriteLine("[OK]  Startup shortcut created: {0}", shortcutPath);
        Console.WriteLine("[OK]  LocationGlue installed. Log out and back in to start.");
    }

    static void Uninstall()
    {
        int selfPid = Process.GetCurrentProcess().Id;

        // Helper: count OTHER LocationGlue instances (excluding this uninstaller)
        Func<int> otherCount = () =>
        {
            int n = 0;
            foreach (var p in Process.GetProcessesByName("LocationGlue"))
                if (p.Id != selfPid) n++;
            return n;
        };

        // Step 1: Try graceful shutdown via named event.
        // This lets the running instance call watcher.Stop() + Dispose(),
        // which cleanly releases the location session so Windows updates
        // the tray icon immediately — no reboot required.
        if (otherCount() > 0)
        {
            try
            {
                EventWaitHandle shutdownEvent;
                if (EventWaitHandle.TryOpenExisting(@"LocationGlue_Shutdown", out shutdownEvent))
                {
                    shutdownEvent.Set();
                    shutdownEvent.Dispose();

                    // Wait up to 5 seconds for graceful exit
                    for (int i = 0; i < 50; i++)
                    {
                        Thread.Sleep(100);
                        if (otherCount() == 0) break;
                    }
                }
            }
            catch { /* best effort — fall through to hard kill */ }
        }

        // Step 2: Hard kill any stragglers (excluding self!)
        foreach (var p in Process.GetProcessesByName("LocationGlue"))
        {
            if (p.Id == selfPid) continue;
            try { p.Kill(); } catch { }
        }
        Console.WriteLine("[OK]  Stopped running instances");

        // Remove shortcut
        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "LocationGlue.lnk");
        if (System.IO.File.Exists(shortcutPath))
        {
            System.IO.File.Delete(shortcutPath);
            Console.WriteLine("[OK]  Startup shortcut removed");
        }

        // Step 3: Restart explorer to force taskbar to re-read location icon state
        try
        {
            Console.WriteLine("[LG] Restarting taskbar to refresh icon...");
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                p.Kill();
            }
            // Windows auto-restarts explorer; also launch explicitly for safety
            Process.Start("explorer.exe");
            Console.WriteLine("[OK]  Taskbar refreshed");
        }
        catch { /* best effort */ }

        Console.WriteLine("[DONE] LocationGlue uninstalled.");
    }

    static void Status()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  LocationGlue v1.1.1");
        Console.WriteLine("  Keep Windows 11 location icon steady");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Install status
        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "LocationGlue.lnk");
        bool installed = System.IO.File.Exists(shortcutPath);
        Console.WriteLine("  Auto-start:  [{0}]  {1}",
            installed ? "INSTALLED" : " NOT INSTALLED",
            installed ? shortcutPath : "");

        // Running instances (excluding self)
        int selfPid = Process.GetCurrentProcess().Id;
        var running = Process.GetProcessesByName("LocationGlue");
        int otherCount = 0;
        foreach (var p in running)
        {
            if (p.Id == selfPid) continue;
            otherCount++;
            long memMB = p.WorkingSet64 / 1024 / 1024;
            TimeSpan uptime = DateTime.Now - p.StartTime;
            string uptimeStr = uptime.TotalDays >= 1
                ? string.Format("{0}d {1}h {2}m", (int)uptime.TotalDays, uptime.Hours, uptime.Minutes)
                : string.Format("{0}h {1}m {2}s", uptime.Hours, uptime.Minutes, uptime.Seconds);
            Console.WriteLine();
            Console.WriteLine("  Running instance #{0}:", otherCount);
            Console.WriteLine("    PID:       {0}", p.Id);
            Console.WriteLine("    Started:   {0:yyyy-MM-dd HH:mm:ss}", p.StartTime);
            Console.WriteLine("    Uptime:    {0}", uptimeStr);
            Console.WriteLine("    Memory:    {0} MB (private)", memMB);
        }

        if (otherCount == 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Status:      NOT RUNNING");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("  Total running: {0} instance(s)", otherCount);
        }

        // Self info
        Console.WriteLine();
        Console.WriteLine("  Exe:        {0}", Process.GetCurrentProcess().MainModule.FileName);
        Console.WriteLine("========================================");
    }

    static void PrintUsage()
    {
        Console.WriteLine("LocationGlue v1.1.1 — Keep location icon always visible (.NET Framework 4.8)");
        Console.WriteLine();
        Console.WriteLine("  LocationGlue.exe            Run foreground (Ctrl+C to stop)");
        Console.WriteLine("  LocationGlue.exe install    Install to startup folder");
        Console.WriteLine("  LocationGlue.exe uninstall  Remove from startup");
        Console.WriteLine("  LocationGlue.exe status     Show install & running status");
    }
}
