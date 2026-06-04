<#
.SYNOPSIS
    [ALTERNATIVE METHOD] Install LocationGlue via PowerShell.
    For most users, double-clicking Setup.cmd is recommended instead.
    This script is for advanced users who prefer the command line.
#>

param(
    [ValidateSet("install", "uninstall", "status")]
    [string]$Command = "install"
)

$ExeName = "LocationGlue.exe"
$TaskName = "LocationGlue"

# Paths: exe lives next to this script
$ScriptDir = Split-Path -Parent $PSCommandPath
$ExePath = Join-Path $ScriptDir $ExeName
$LegacyShortcut = Join-Path ([Environment]::GetFolderPath("Startup")) "LocationGlue.lnk"

function Install-Steady {
    Write-Host "=== LocationGlue Install ===" -ForegroundColor Cyan

    if (-not (Test-Path $ExePath)) {
        Write-Host "[ERR] $ExeName not found at: $ExePath" -ForegroundColor Red
        return
    }

    # Remove legacy Startup shortcut if present
    if (Test-Path $LegacyShortcut) {
        Remove-Item $LegacyShortcut -Force
        Write-Host "[INFO] Removed legacy Startup shortcut"
    }

    # Create scheduled task for auto-start at logon
    $action = New-ScheduledTaskAction -Execute $ExePath -Argument "run"
    $trigger = New-ScheduledTaskTrigger -AtLogOn
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -DisallowStartIfOnBatteries:$false -StopIfGoingOnBatteries:$false -ExecutionTimeLimit 0 -Hidden

    try {
        Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
        Write-Host "[OK]  Scheduled task '$TaskName' created." -ForegroundColor Green
        Write-Host "[INFO] LocationGlue will start automatically at every logon."
    }
    catch {
        Write-Host "[ERR] Failed to create scheduled task: $_" -ForegroundColor Red
        return
    }

    # Also run it now
    Write-Host ""
    Write-Host "[INFO] Starting LocationGlue now..."
    Start-Process -FilePath $ExePath -ArgumentList "run" -WindowStyle Hidden
    Write-Host "[OK]  LocationGlue started. Location icon should now be always visible." -ForegroundColor Green
}

function Uninstall-Steady {
    Write-Host "=== LocationGlue Uninstall ===" -ForegroundColor Cyan

    # Kill any running instances
    Get-Process -Name "LocationGlue" -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "[OK]  Stopped running instances"

    # Remove scheduled task
    try {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
        Write-Host "[OK]  Scheduled task removed"
    }
    catch {
        Write-Host "[INFO] No scheduled task to remove"
    }

    # Remove legacy shortcut
    if (Test-Path $LegacyShortcut) {
        Remove-Item $LegacyShortcut -Force
        Write-Host "[OK]  Legacy Startup shortcut removed"
    }

    Write-Host "[DONE] LocationGlue uninstalled." -ForegroundColor Green
}

function Show-Status {
    Write-Host "=== LocationGlue Status ===" -ForegroundColor Cyan
    Write-Host ""

    # Scheduled task
    $task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if ($task) {
        Write-Host "  Auto-start:     [INSTALLED]  (Task Scheduler: $TaskName)"
    }
    else {
        Write-Host "  Auto-start:     [not installed]"
    }

    # Legacy shortcut
    if (Test-Path $LegacyShortcut) {
        Write-Host "  Legacy shortcut: [EXISTS — run install to migrate]"
    }

    # Running instances
    $running = Get-Process -Name "LocationGlue" -ErrorAction SilentlyContinue
    Write-Host "  Running instances: $($running.Count)"
    if ($running) {
        foreach ($p in $running) {
            Write-Host "    PID $($p.Id) — Started: $($p.StartTime) — Mem: $([math]::Round($p.WorkingSet64/1MB, 1)) MB"
        }
    }

    # Exe location
    if (Test-Path $ExePath) {
        Write-Host "  Executable: $ExePath"
    }
    else {
        Write-Host "  Executable: [MISSING — $ExePath]"
    }
}

switch ($Command) {
    "install"   { Install-Steady }
    "uninstall" { Uninstall-Steady }
    "status"    { Show-Status }
}
