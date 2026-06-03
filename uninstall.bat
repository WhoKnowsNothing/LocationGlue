@echo off
setlocal enabledelayedexpansion
title LocationGlue — One-Click Uninstall

:: ==============================================
:: LocationGlue — Uninstall Script
:: Removes everything: process, shortcut, files.
:: ==============================================

echo.
echo  ============================================
echo    LocationGlue — One-Click Uninstall
echo  ============================================
echo.

:: Locate LocationSteady.exe
set "EXE=%~dp0src\LocationSteady.exe"
if not exist "!EXE!" set "EXE=%~dp0LocationSteady.exe"

echo  [STEP 1/3] Stopping running instances...

:: Kill by process name first (reliable, no exe needed)
taskkill /f /im "LocationSteady.exe" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo  [OK]   Stopped running LocationSteady processes.
) else (
    echo  [INFO] No running instances found.
)

:: Also use the built-in uninstaller if exe exists
if exist "!EXE!" (
    echo.
    echo  [STEP 2/3] Removing startup shortcut...
    "!EXE!" uninstall
) else (
    echo.
    echo  [STEP 2/3] Removing startup shortcut manually...
    :: Remove shortcut directly
    set "SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\LocationSteady.lnk"
    if exist "!SHORTCUT!" del /q "!SHORTCUT!" 2>nul && echo  [OK]   Startup shortcut removed. || echo  [INFO] Could not remove shortcut.
)

echo.
echo  [STEP 3/3] Cleaning up...
echo.
echo  LocationGlue has been uninstalled. The location icon will
echo  now behave normally (disappear/reappear as apps use location).
echo.
echo  You can safely delete this folder to remove all program files.
echo.
set /p DELETE="  Delete all program files now? [y/N]: "
if /i "!DELETE!"=="y" (
    echo.
    echo  Deleting LocationGlue files...
    cd /d "%~dp0.."
    rmdir /s /q "%~dp0" 2>nul
    if exist "%~dp0" (
        echo  [INFO] Some files may still be in use. You can delete the folder manually.
    ) else (
        echo  [OK]   All files removed.
    )
)

echo.
echo  ============================================
echo    Uninstall Complete
echo  ============================================
echo.
pause
