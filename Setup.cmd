@echo off
title LocationGlue Setup

:: ==============================================
:: LocationGlue - Setup (Single Entry Point)
:: Double-click me. I handle everything.
:: ==============================================

set "EXE=%~dp0src\LocationSteady.exe"
if not exist "%EXE%" set "EXE=%~dp0LocationSteady.exe"
set "SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\LocationSteady.lnk"

:: ---- Detect state and jump to the right menu ----
if exist "%SHORTCUT%" goto menu_installed
goto menu_not_installed


:: ===== NOT INSTALLED =====
:menu_not_installed
cls
echo.
echo  ============================================
echo    LocationGlue v1.1.0 - Setup
echo    Keep Windows 11 location icon always visible
echo  ============================================
echo.
echo  Status: [NOT INSTALLED]
echo.
echo  [I] Install and start
echo  [Q] Quit
echo.
choice /c IQ /n /m "Your choice: "
if errorlevel 2 goto quit
if errorlevel 1 goto do_install
goto quit


:: ===== INSTALLED =====
:menu_installed
cls
echo.
echo  ============================================
echo    LocationGlue v1.1.0 - Setup
echo    Keep Windows 11 location icon always visible
echo  ============================================
echo.
echo  Status: [INSTALLED]
echo.
echo  [U] Uninstall
echo  [S] Status
echo  [R] Reinstall
echo  [Q] Quit
echo.
choice /c USRQ /n /m "Your choice: "
if errorlevel 4 goto quit
if errorlevel 3 goto do_reinstall
if errorlevel 2 goto do_status
if errorlevel 1 goto do_uninstall
goto quit


:: ===== ACTIONS =====
:do_install
    echo.
    echo Installing...
    if not exist "%EXE%" (
        echo [ERROR] LocationSteady.exe not found.
        echo Expected: %~dp0src\LocationSteady.exe
        echo Make sure you extracted all files from the zip.
        pause
        exit /b 1
    )
    "%EXE%" install
    if errorlevel 1 (
        echo [ERROR] Could not create startup shortcut.
        pause
        exit /b 1
    )
    echo [OK] Startup shortcut created.
    echo.
    echo Starting now...
    start "" /MIN "%EXE%" run
    echo [OK] LocationSteady is running.
    echo.
    echo The location icon in your system tray should now
    echo stay steadily visible. No more blinking!
    echo.
    echo You can close this window. To uninstall later,
    echo just double-click Setup.cmd again.
    pause
    exit /b 0

:do_uninstall
    echo.
    echo Uninstalling...
    taskkill /f /im "LocationSteady.exe" >nul 2>&1
    if exist "%EXE%" "%EXE%" uninstall
    if exist "%SHORTCUT%" del /q "%SHORTCUT%" 2>nul
    echo [DONE] LocationGlue has been removed.
    echo.
    echo The location icon will now behave normally.
    echo You can safely delete this folder.
    pause
    exit /b 0

:do_status
    echo.
    if exist "%EXE%" (
        "%EXE%" status
    ) else (
        echo [WARN] LocationSteady.exe not found.
        if exist "%SHORTCUT%" (
            echo Startup shortcut: [INSTALLED]
        ) else (
            echo Startup shortcut: [NOT INSTALLED]
        )
    )
    echo.
    pause
    goto menu_installed

:do_reinstall
    echo.
    echo Reinstalling...
    taskkill /f /im "LocationSteady.exe" >nul 2>&1
    if exist "%EXE%" "%EXE%" uninstall >nul 2>&1
    timeout /t 2 /nobreak >nul
    "%EXE%" install
    start "" /MIN "%EXE%" run
    echo [DONE] LocationGlue reinstalled and running.
    pause
    exit /b 0

:quit
    echo.
    pause
    exit /b 0
