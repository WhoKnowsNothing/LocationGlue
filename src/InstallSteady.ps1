<#
.SYNOPSIS
    [ALTERNATIVE METHOD] Install LocationSteady via PowerShell.
    For most users, double-clicking Setup.cmd is recommended instead.
    This script is for advanced users who prefer the command line.
#>

param(
    [ValidateSet("install", "uninstall", "status")]
    [string]$Command = "install"
)

$ExeName = "LocationSteady.exe"
$TaskName = "LocationSteady"

# Paths: exe lives next to this script
$ScriptDir = Split-Path -Parent $PSCommandPath
$ExePath = Join-Path $ScriptDir $ExeName
$StartupDir = [Environment]::GetFolderPath("Startup")
$ShortcutPath = Join-Path $StartupDir "LocationSteady.lnk"

function Install-Steady {
    Write-Host "=== LocationSteady Install ===" -ForegroundColor Cyan

    if (-not (Test-Path $ExePath)) {
        Write-Host "[ERR] $ExeName not found at: $ExePath" -ForegroundColor Red
        return
    }

    # Create a shortcut in the Startup folder that runs minimized
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $ExePath
    $Shortcut.Arguments = "run"
    $Shortcut.WindowStyle = 7  # Normal window (WinExe has no window anyway)
    $Shortcut.WorkingDirectory = $ScriptDir
    $Shortcut.Save()

    Write-Host "[OK]  Startup shortcut created: $ShortcutPath" -ForegroundColor Green
    Write-Host "[INFO] LocationSteady will start minimized at every logon."
    Write-Host "[TIP]  Log out and back in, or run the exe directly:"
    Write-Host "       $ExePath"

    # Also run it now
    Write-Host ""
    Write-Host "[INFO] Starting LocationSteady now..."
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $ExePath
    $psi.Arguments = "run"
    $psi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized
    [System.Diagnostics.Process]::Start($psi) | Out-Null
    Write-Host "[OK]  LocationSteady started. Location icon should now be always visible." -ForegroundColor Green
}

function Uninstall-Steady {
    Write-Host "=== LocationSteady Uninstall ===" -ForegroundColor Cyan

    # Kill any running instances
    Get-Process -Name "LocationSteady" -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "[OK]  Stopped running instances"

    # Remove startup shortcut
    if (Test-Path $ShortcutPath) {
        Remove-Item $ShortcutPath -Force
        Write-Host "[OK]  Startup shortcut removed"
    } else {
        Write-Host "[INFO] No startup shortcut found"
    }

    Write-Host "[DONE] LocationSteady uninstalled." -ForegroundColor Green
}

function Show-Status {
    Write-Host "=== LocationSteady Status ===" -ForegroundColor Cyan
    Write-Host ""

    # Startup shortcut
    if (Test-Path $ShortcutPath) {
        Write-Host "  Startup shortcut: [INSTALLED]"
    } else {
        Write-Host "  Startup shortcut: [not installed]"
    }

    # Running instances
    $running = Get-Process -Name "LocationSteady" -ErrorAction SilentlyContinue
    Write-Host "  Running instances: $($running.Count)"
    if ($running) {
        foreach ($p in $running) {
            Write-Host "    PID $($p.Id) — Started: $($p.StartTime) — Mem: $([math]::Round($p.WorkingSet64/1MB, 1)) MB"
        }
    }

    # Exe location
    if (Test-Path $ExePath) {
        Write-Host "  Executable: $ExePath"
    } else {
        Write-Host "  Executable: [MISSING — $ExePath]"
    }
}

switch ($Command) {
    "install"   { Install-Steady }
    "uninstall" { Uninstall-Steady }
    "status"    { Show-Status }
}
