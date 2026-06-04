/*
 * LocationGlue.exe — Keep Windows 11 "Location In Use" icon always visible.
 * .NET Framework 4.8 version — uses system runtime (~5-10MB, shared across all .NET apps).
 *
 * Uses System.Device.Location.GeoCoordinateWatcher (no WinRT needed).
 *
 * v1.2.0 — Task Scheduler for auto-start (more reliable than Startup folder),
 *          reverted to winexe (no console window).
 */

using System;
using System.Device.Location;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

class LocationGlue
{
    const string TaskName   = "LocationGlue";

    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    const int ATTACH_PARENT_PROCESS = -1;
    const string TaskXmlFmt = @"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <Triggers>
    <LogonTrigger/>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{0}</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>LeastPrivilege</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Hidden>true</Hidden>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>""{1}""</Command>
      <Arguments>run</Arguments>
    </Exec>
  </Actions>
</Task>";

    static void Main(string[] args)
    {
        string cmd = args.Length > 0 ? args[0].ToLower() : "run";

        // For non-run commands, attach to parent console so output is visible.
        // The "run" command deliberately skips this → no window at logon.
        if (cmd != "run" && !AttachConsole(ATTACH_PARENT_PROCESS))
            AllocConsole();

        switch (cmd)
        {
            case "run":       RunForeground(); break;
            case "install":   Install(); break;
            case "uninstall": Uninstall(); break;
            case "status":    Status(); break;
            case "setup":     SetupInteractive(); break;
            default:          PrintUsage(); break;
        }
    }

    static void RunForeground()
    {
        bool running = true;

        // Ctrl+C handler — only fires when run from terminal
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
                // Quiet — winexe has no console to write to
            };
            watcher.PositionChanged += (s, e) =>
            {
                // Discard — just keep the session alive
            };

            watcher.Start();

            // Keep alive until shutdown signal or Ctrl+C
            while (running)
            {
                int signaled = WaitHandle.WaitAny(new WaitHandle[] { shutdownEvent }, 1000);
                if (signaled == 0) break;

                // Periodically check if watcher is still healthy
                if (watcher.Status == GeoPositionStatus.Disabled || watcher.Status == GeoPositionStatus.NoData)
                {
                    watcher.Stop();
                    Thread.Sleep(2000);
                    if (shutdownEvent.WaitOne(0)) break;
                    watcher.Start();
                }
            }

            watcher.Stop();
        }
    }

    static string GetExePath()
    {
        return Process.GetCurrentProcess().MainModule.FileName;
    }

    static bool IsElevated()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    static void Install()
    {
        // If not admin, re-launch ourselves elevated
        if (!IsElevated())
        {
            Console.WriteLine("[INFO] Administrator privileges required. Elevating...");
            var selfPsi = new ProcessStartInfo
            {
                FileName = GetExePath(),
                Arguments = "install",
                UseShellExecute = true,
                Verb = "runas",  // Triggers UAC prompt
            };
            try
            {
                var selfP = Process.Start(selfPsi);
                selfP.WaitForExit();
                Environment.Exit(selfP.ExitCode);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User declined UAC prompt
                Console.Error.WriteLine("[ERR] Administrator privileges required to create scheduled task.");
                Environment.Exit(1);
            }
        }

        string exePath = GetExePath();
        string xmlFile = System.IO.Path.GetTempFileName();
        string userId  = Environment.UserDomainName + "\\" + Environment.UserName;

        try
        {
            // Write task XML (avoids schtasks argument-quoting issues with paths containing spaces)
            string xml = string.Format(TaskXmlFmt, userId, exePath);
            System.IO.File.WriteAllText(xmlFile, xml);

            var psi = new ProcessStartInfo
            {
                FileName  = "schtasks",
                Arguments = string.Format("/create /tn \"{0}\" /xml \"{1}\" /f", TaskName, xmlFile),
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle     = ProcessWindowStyle.Hidden,
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit(60000);
                if (p.ExitCode != 0)
                {
                    Console.Error.WriteLine("[ERR] schtasks exited with code {0}", p.ExitCode);
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("[OK]  Scheduled task '{0}' created.", TaskName);
            Console.WriteLine("[OK]  LocationGlue will start automatically at every logon.");
        }
        finally
        {
            try { System.IO.File.Delete(xmlFile); } catch { }
        }
    }

    static void Uninstall()
    {
        // If not admin, re-launch ourselves elevated
        if (!IsElevated())
        {
            Console.WriteLine("[INFO] Administrator privileges required. Elevating...");
            var selfPsi = new ProcessStartInfo
            {
                FileName = GetExePath(),
                Arguments = "uninstall",
                UseShellExecute = true,
                Verb = "runas",
            };
            try
            {
                var selfP = Process.Start(selfPsi);
                selfP.WaitForExit();
                Environment.Exit(selfP.ExitCode);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.Error.WriteLine("[ERR] Administrator privileges required to remove scheduled task.");
                Environment.Exit(1);
            }
        }

        int selfPid = Process.GetCurrentProcess().Id;

        // Helper: count OTHER LocationGlue instances (excluding this uninstaller)
        Func<int> otherCount = () =>
        {
            int n = 0;
            foreach (var p in Process.GetProcessesByName("LocationGlue"))
                if (p.Id != selfPid) n++;
            return n;
        };

        // Step 1: Graceful shutdown via named event
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
            catch { /* best effort */ }
        }

        // Step 2: Hard kill any stragglers
        foreach (var p in Process.GetProcessesByName("LocationGlue"))
        {
            if (p.Id == selfPid) continue;
            try { p.Kill(); } catch { }
        }
        Console.WriteLine("[OK]  Stopped running instances");

        // Step 3: Remove scheduled task
        var psiTask = new ProcessStartInfo
        {
            FileName  = "schtasks",
            Arguments = string.Format("/delete /tn \"{0}\" /f", TaskName),
            UseShellExecute = false,
            CreateNoWindow  = true,
            WindowStyle     = ProcessWindowStyle.Hidden,
        };
        using (var p = Process.Start(psiTask))
        {
            p.WaitForExit(30000);
            if (p.ExitCode == 0)
                Console.WriteLine("[OK]  Scheduled task removed");
            else
                Console.WriteLine("[INFO] No scheduled task to remove (code {0})", p.ExitCode);
        }

        // Step 4: Restart explorer to force taskbar refresh
        try
        {
            Console.WriteLine("[LG] Restarting taskbar to refresh icon...");
            foreach (var ep in Process.GetProcessesByName("explorer"))
                ep.Kill();
            Process.Start("explorer.exe");
            Console.WriteLine("[OK]  Taskbar refreshed");
        }
        catch { /* best effort */ }

        Console.WriteLine("[DONE] LocationGlue uninstalled.");
    }

    static void Status()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  LocationGlue v1.2.0");
        Console.WriteLine("  Keep Windows 11 location icon steady");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // ---- Scheduled task check ----
        var psiTask = new ProcessStartInfo
        {
            FileName  = "schtasks",
            Arguments = string.Format("/query /tn \"{0}\" /fo csv /nh", TaskName),
            UseShellExecute = false,
            CreateNoWindow  = true,
            WindowStyle     = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };

        bool taskExists = false;
        try
        {
            using (var p = Process.Start(psiTask))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(15000);
                taskExists = p.ExitCode == 0 && output.Contains(TaskName);
            }
        }
        catch { /* keep default false */ }

        string statusText = taskExists ? "INSTALLED" : "NOT INSTALLED";
        Console.WriteLine("  Auto-start:  [{0}]  (Task Scheduler: {1})", statusText, TaskName);

        // ---- Check Startup shortcut (legacy) ----
        string shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "LocationGlue.lnk");
        if (System.IO.File.Exists(shortcutPath))
        {
            Console.WriteLine("  [NOTE] Legacy Startup shortcut still exists.");
            Console.WriteLine("         Run 'install' again to migrate to Task Scheduler.");
        }

        Console.WriteLine();

        // ---- Running instances ----
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
            Console.WriteLine("  Running instance #{0}:", otherCount);
            Console.WriteLine("    PID:       {0}", p.Id);
            Console.WriteLine("    Started:   {0:yyyy-MM-dd HH:mm:ss}", p.StartTime);
            Console.WriteLine("    Uptime:    {0}", uptimeStr);
            Console.WriteLine("    Memory:    {0} MB (private)", memMB);
        }

        if (otherCount == 0)
        {
            Console.WriteLine("  Status:      NOT RUNNING");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("  Total running: {0} instance(s)", otherCount);
        }

        Console.WriteLine();
        Console.WriteLine("  Exe:        {0}", Process.GetCurrentProcess().MainModule.FileName);
        Console.WriteLine("========================================");
    }

    static bool IsTaskInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName  = "schtasks",
                Arguments = string.Format("/query /tn \"{0}\" /fo csv /nh", TaskName),
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle     = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(15000);
                return p.ExitCode == 0 && output.Contains(TaskName);
            }
        }
        catch { return false; }
    }

    static void SetupInteractive()
    {
        // Elevate the entire interactive session upfront
        if (!IsElevated())
        {
            Console.WriteLine("[INFO] Administrator privileges required. Elevating...");
            var selfPsi = new ProcessStartInfo
            {
                FileName = GetExePath(),
                Arguments = "setup",
                UseShellExecute = true,
                Verb = "runas",
            };
            try
            {
                var selfP = Process.Start(selfPsi);
                selfP.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.Error.WriteLine("[ERR] Administrator privileges required.");
            }
            return;
        }

        bool installed = IsTaskInstalled();

        Console.WriteLine();
        Console.WriteLine("  ============================================");
        Console.WriteLine("    LocationGlue v1.2.0 - Setup");
        Console.WriteLine("    Keep Windows 11 location icon always visible");
        Console.WriteLine("  ============================================");
        Console.WriteLine();
        Console.WriteLine(installed
            ? "  Status: [INSTALLED]  (Task Scheduler: {0})"
            : "  Status: [NOT INSTALLED]", TaskName);
        Console.WriteLine();

        if (installed)
        {
            Console.WriteLine("  [U] Uninstall");
            Console.WriteLine("  [S] Status");
            Console.WriteLine("  [R] Reinstall");
            Console.WriteLine("  [Q] Quit");
        }
        else
        {
            Console.WriteLine("  [I] Install and start");
            Console.WriteLine("  [Q] Quit");
        }
        Console.WriteLine();

        string line = (Console.ReadLine() ?? "").Trim().ToUpper();
        Console.WriteLine();

        if (line == "I" && !installed)
        {
            // Clean up any stale state first
            try { Uninstall(); } catch { }
            Install();
            Console.WriteLine();
            Console.WriteLine("Starting now...");
            StartRunProcess();
            Console.WriteLine("[OK] LocationGlue is running.");
        }
        else if (line == "U" && installed)
        {
            Uninstall();
        }
        else if (line == "S" && installed)
        {
            Status();
        }
        else if (line == "R" && installed)
        {
            Uninstall();
            foreach (var p in Process.GetProcessesByName("LocationGlue"))
            {
                try { p.Kill(); } catch { }
            }
            Thread.Sleep(2000);
            Install();
            Console.WriteLine();
            StartRunProcess();
            Console.WriteLine("[DONE] LocationGlue reinstalled and running.");
        }
        else if (line == "Q")
        {
            Console.WriteLine("Bye.");
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }

        Console.WriteLine();
        Console.Write("Press Enter to exit...");
        Console.ReadLine();
    }

    static void StartRunProcess()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetExePath(),
                Arguments = "run",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi);
        }
        catch { /* best effort */ }
    }

    static void PrintUsage()
    {
        Console.WriteLine("LocationGlue v1.2.0 — Keep location icon always visible (.NET Framework 4.8)");
        Console.WriteLine();
        Console.WriteLine("  LocationGlue.exe            Run foreground");
        Console.WriteLine("  LocationGlue.exe setup      Interactive setup menu");
        Console.WriteLine("  LocationGlue.exe install    Install scheduled task (auto-start at logon)");
        Console.WriteLine("  LocationGlue.exe uninstall  Remove scheduled task");
        Console.WriteLine("  LocationGlue.exe status     Show install & running status");
    }
}
