@echo off
setlocal enabledelayedexpansion
title LocationGlue Setup

:: ==============================================
:: LocationGlue — Setup (Smart Entry Point)
:: Auto-detects state: installed → offer uninstall
::                    not installed → offer install
:: ==============================================

:: Locate LocationSteady.exe
set "EXE=%~dp0src\LocationSteady.exe"
if not exist "!EXE!" set "EXE=%~dp0LocationSteady.exe"

:: Check install state: look for startup shortcut
set "SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\LocationSteady.lnk"
set "INSTALLED=0"
if exist "!SHORTCUT!" set "INSTALLED=1"

:: Also check if process is running
tasklist /fi "imagename eq LocationSteady.exe" 2>nul | find /i "LocationSteady" >nul
set "RUNNING=!ERRORLEVEL!"

cls
echo.
echo  ============================================
echo    LocationGlue v1.0.0 — Setup
echo    Keep Windows 11 location icon always visible
echo  ============================================
echo.

if "!INSTALLED!"=="1" (
    echo  Status: [INSTALLED]
    if "!RUNNING!"=="0" (
        echo          LocationSteady is currently running.
    ) else (
        echo          LocationSteady is NOT running (will start at next logon).
    )
    echo.
    echo  ------------------------------------------
    echo   [U] Uninstall  — Remove from system
    echo   [S] Status     — Show detailed information
    echo   [R] Reinstall  — Repair installation
    echo   [Q] Quit
    echo  ------------------------------------------
    echo.
    choice /c USRQ /n /m "Your choice: "
    if errorlevel 4 goto :eof
    if errorlevel 3 goto :reinstall
    if errorlevel 2 goto :status
    if errorlevel 1 goto :uninstall
) else (
    echo  Status: [NOT INSTALLED]
    echo.
    echo  LocationGlue is not currently set up on this system.
    echo  It will keep the location tray icon steadily visible
    echo  so it stops blinking when apps use your location.
    echo.
    echo  ------------------------------------------
    echo   [I] Install    — Set up LocationGlue
    echo   [Q] Quit
    echo  ------------------------------------------
    echo.
    choice /c IQ /n /m "Your choice: "
    if errorlevel 2 goto :eof
    if errorlevel 1 goto :install
)

goto :eof

:: ==============================================

:install
    echo.
    if not exist "!EXE!" (
        echo  [ERROR] LocationSteady.exe not found.
        echo  [INFO]  Please extract all files from the zip first.
        echo  [INFO]  Expected: %~dp0src\LocationSteady.exe
        echo.
        pause
        goto :eof
    )
    echo  Installing...
    "!EXE!" install
    if %ERRORLEVEL% NEQ 0 (
        echo  [ERROR] Could not create startup shortcut.
        echo.
        pause
        goto :eof
    )
    echo  [OK]   Startup shortcut created.
    echo  [OK]   LocationSteady will auto-start at every logon.
    echo.
    echo  Starting now...
    start "" /MIN "!EXE!" run
    echo  [OK]   LocationSteady is now running.
    echo.
    echo  The location icon in your system tray should now stay
    echo  steadily visible. You can close this window.
    echo.
    pause
    goto :eof

:uninstall
    echo.
    echo  Uninstalling...
    taskkill /f /im "LocationSteady.exe" >nul 2>&1
    if exist "!EXE!" (
        "!EXE!" uninstall
    ) else (
        if exist "!SHORTCUT!" del /q "!SHORTCUT!" 2>nul
    )
    echo.
    echo  [DONE] LocationGlue has been removed from your system.
    echo  The location icon will now behave normally.
    echo.
    echo  Tip: You can safely delete this folder to remove
    echo       all remaining program files.
    echo.
    pause
    goto :eof

:status
    echo.
    if exist "!EXE!" (
        "!EXE!" status
    ) else (
        echo  [WARN] LocationSteady.exe not found — cannot show detailed status.
        echo.
        if exist "!SHORTCUT!" (
            echo  Startup shortcut: [INSTALLED]
        ) else (
            echo  Startup shortcut: [NOT INSTALLED]
        )
    )
    echo.
    pause
    goto :eof

:reinstall
    echo.
    echo  Reinstalling (remove + install)...
    taskkill /f /im "LocationSteady.exe" >nul 2>&1
    if exist "!EXE!" "!EXE!" uninstall >nul 2>&1
    timeout /t 2 /nobreak >nul
    "!EXE!" install
    start "" /MIN "!EXE!" run
    echo  [DONE] LocationGlue reinstalled and running.
    echo.
    pause
    goto :eof
