@echo off
:: LocationGlue — One-Click Uninstall

echo.
echo ========================================
echo   LocationGlue — One-Click Uninstall
echo ========================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\InstallSteady.ps1" -Command uninstall

echo.
echo Press any key to close...
pause >nul
