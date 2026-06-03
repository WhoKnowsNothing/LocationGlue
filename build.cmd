@echo off
:: LocationGlue — Build from Source
:: Uses the C# compiler included with Windows (.NET Framework 4.x)

echo.
echo ========================================
echo   LocationGlue — Build
echo ========================================
echo.

set CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# compiler not found at: %CSC%
    echo [INFO]  Install .NET Framework 4.8 Developer Pack:
    echo         https://dotnet.microsoft.com/download/dotnet-framework/net48
    pause
    exit /b 1
)

echo [INFO] Compiler: %CSC%
echo [INFO] Source:   src\ProgramFx.cs
echo [INFO] Output:   src\LocationSteady.exe
echo.

"%CSC%" -out:src\LocationSteady.exe -target:winexe -platform:x64 -optimize -reference:System.Device.dll -reference:Microsoft.CSharp.dll src\ProgramFx.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] Build successful! LocationSteady.exe is ready.
) else (
    echo.
    echo [FAIL] Build failed. Check the errors above.
)

echo.
echo Press any key to close...
pause >nul
