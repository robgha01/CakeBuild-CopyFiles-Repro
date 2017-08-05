@echo off
set "build.config=%~1"
:: If no config is specified, run a debug build
if "%build.config%"=="" set build.config=release
SET ThisScriptsDirectory=%~dp0
SET PowerShellScriptPath=%ThisScriptsDirectory%build.ps1
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%PowerShellScriptPath%' -Experimental -Script 'build.cake'";