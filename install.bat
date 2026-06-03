@echo off
setlocal enabledelayedexpansion
title LocationGlue — One-Click Install

:: ==============================================
:: LocationGlue — Install Script
:: Double-click to install. No admin needed.
:: ==============================================

echo.
echo  ============================================
echo    LocationGlue — One-Click Install
echo  ============================================
echo.

:: Locate LocationSteady.exe (try multiple paths)
set "EXE=%~dp0src\LocationSteady.exe"
if not exist "!EXE!" set "EXE=%~dp0LocationSteady.exe"

if not exist "!EXE!" (
    echo  [ERROR] LocationSteady.exe not found.
    echo  [INFO]  Please make sure you extracted all files from the zip.
    echo  [INFO]  Expected location: %~dp0src\LocationSteady.exe
    echo.
    pause
    exit /b 1
)

echo  [INFO] Found: !EXE!
echo.

:: Install via the built-in installer
echo  [STEP 1/2] Creating startup shortcut...
"!EXE!" install

if %ERRORLEVEL% NEQ 0 (
    echo  [ERROR] Installation failed. Please try running as Administrator.
    echo.
    pause
    exit /b 1
)

:: Start immediately
echo.
echo  [STEP 2/2] Starting LocationSteady now...
start "" /MIN "!EXE!" run

if %ERRORLEVEL% EQU 0 (
    echo  [OK]   LocationSteady is now running in the background.
) else (
    echo  [WARN] Started but may need a moment. Check status after a few seconds.
)

echo.
echo  ============================================
echo    Install Complete!
echo.
echo    The location icon in your system tray
echo    should now stay steadily visible.
echo.
echo    To uninstall later, double-click:
echo       Setup.cmd  or  uninstall.bat
echo  ============================================

echo.
pause
