@echo off
:: LocationGlue — One-Click Install
:: Right-click → Run as Administrator is NOT required.

echo.
echo ========================================
echo   LocationGlue — One-Click Install
echo ========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\InstallSteady.ps1" -Command install

echo.
echo Press any key to close...
pause >nul
