@echo off
SET index=1
:: Change to the directory that this batch file is in
:: NB: it must be invoked with a full path!
for /f %%i in ("%0") do set curpath=%%~dpi
cd /d %curpath%

:: Fetch input parameters
set "build.config=%~1"

:: If no config is specified, run a debug build
if "%build.config%"=="" set build.config=release
SET ThisScriptsDirectory=%~dp0
SET PowerShellScriptPath=%ThisScriptsDirectory%build.ps1

SETLOCAL ENABLEDELAYEDEXPANSION
FOR %%f IN (*.cake) DO (
   SET file!index!=%%f
   ECHO !index! - %%f
   SET /A index=!index!+1
)
SETLOCAL DISABLEDELAYEDEXPANSION

CALL :SHOWMENU

:SHOWMENU
IF NOT %index% == 2 (
	SET /P selection="select which build to run or q to exit:"
	IF "%selection%" == "q" (EXIT)

	SET file%selection% >nul 2>&1

	IF ERRORLEVEL 1 (
		ECHO invalid number selected   
		EXIT /B 1
	)

	CALL :RESOLVE %%file%selection%%%
) ELSE (
	cls
	CALL :RESOLVE %file1%
)

:: Execute the build script
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%PowerShellScriptPath%' -Experimental -Script '%file_name%' -Target 'Build-Artifact' -Verbosity 'diagnostic' -configuration 'webprod'";
pause
IF NOT %index% == 2 (
	CALL :SHOWMENU
)

:RESOLVE
SET file_name=%1
GOTO:EOF