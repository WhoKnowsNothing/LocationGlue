/*
 * LocationSteady.exe — Keep Windows 11 "Location In Use" icon always visible.
 * .NET Framework 4.8 version — uses system runtime (~5-10MB, shared across all .NET apps).
 *
 * Uses System.Device.Location.GeoCoordinateWatcher (no WinRT needed).
 */

using System;
using System.Device.Location;
using System.Diagnostics;
using System.Threading;

class LocationSteady
{
    const string TaskName = "LocationSteady";

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
        Console.WriteLine("[LS] LocationSteady FX — keeping location icon always visible");
        Console.WriteLine("[LS] Press Ctrl+C to stop.");

        bool running = true;
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            running = false;
            Console.WriteLine("\n[LS] Shutting down...");
        };

        using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
        {
            watcher.MovementThreshold = 10000; // 10km — almost never fires
            watcher.StatusChanged += (s, e) =>
            {
                Console.WriteLine("[LS] Status: {0}", e.Status);
            };
            watcher.PositionChanged += (s, e) =>
            {
                // Discard — just keep the session alive
            };

            watcher.Start();
            Console.WriteLine("[LS] Location watcher started (status: {0})", watcher.Status);

            if (watcher.Status == GeoPositionStatus.NoData || watcher.Status == GeoPositionStatus.Ready)
            {
                Console.WriteLine("[LS] Location icon should now be visible.");
            }

            // Keep alive until Ctrl+C
            while (running)
            {
                Thread.Sleep(1000);
                // Periodically check if watcher is still healthy
                if (watcher.Status == GeoPositionStatus.Disabled || watcher.Status == GeoPositionStatus.NoData)
                {
                    // Try restarting
                    watcher.Stop();
                    Thread.Sleep(2000);
                    watcher.Start();
                    Console.WriteLine("[LS] Restarted watcher (status was: {0})", watcher.Status);
                }
            }

            watcher.Stop();
        }

        Console.WriteLine("[LS] LocationSteady stopped.");
    }

    static void Install()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = System.IO.Path.Combine(startupDir, "LocationSteady.lnk");

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
        Console.WriteLine("[OK]  LocationSteady installed. Log out and back in to start.");
    }

    static void Uninstall()
    {
        // Kill running instances
        foreach (var p in Process.GetProcessesByName("LocationSteady"))
        {
            p.Kill();
        }
        Console.WriteLine("[OK]  Stopped running instances");

        // Remove shortcut
        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "LocationSteady.lnk");
        if (System.IO.File.Exists(shortcutPath))
        {
            System.IO.File.Delete(shortcutPath);
            Console.WriteLine("[OK]  Startup shortcut removed");
        }

        Console.WriteLine("[DONE] LocationSteady uninstalled.");
    }

    static void Status()
    {
        Console.WriteLine("=== LocationSteady FX Status ===");
        Console.WriteLine();

        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "LocationSteady.lnk");
        Console.WriteLine("  Startup: [{0}]", System.IO.File.Exists(shortcutPath) ? "INSTALLED" : "not installed");

        var running = Process.GetProcessesByName("LocationSteady");
        Console.WriteLine("  Running:  {0} instance(s)", running.Length);
        foreach (var p in running)
        {
            Console.WriteLine("    PID {0} — Started: {1} — Mem: {2} MB", p.Id, p.StartTime, p.WorkingSet64 / 1024 / 1024);
        }

        Console.WriteLine("  Exe:      {0}", Process.GetCurrentProcess().MainModule.FileName);
    }

    static void PrintUsage()
    {
        Console.WriteLine("LocationSteady FX — Keep location icon always visible (.NET Framework 4.8)");
        Console.WriteLine();
        Console.WriteLine("  LocationSteady.exe            Run foreground (Ctrl+C to stop)");
        Console.WriteLine("  LocationSteady.exe install    Install to startup folder");
        Console.WriteLine("  LocationSteady.exe uninstall  Remove from startup");
        Console.WriteLine("  LocationSteady.exe status     Show status");
    }
}
