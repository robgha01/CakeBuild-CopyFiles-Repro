@echo off
if not "%1" == "max" start /MAX cmd /c %0 max & exit/b
SET DIR=%~dp0%
@PowerShell -NoProfile -ExecutionPolicy unrestricted -Command "& '%DIR%setup.ps1' %*"
pause