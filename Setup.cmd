@echo off
title LocationGlue Setup
set "EXE=%~dp0src\LocationGlue.exe"
if not exist "%EXE%" set "EXE=%~dp0LocationGlue.exe"
if not exist "%EXE%" (
    echo [ERROR] LocationGlue.exe not found.
    echo Expected: %~dp0src\LocationGlue.exe
    pause
    exit /b 1
)
"%EXE%" setup
pause
