@ECHO OFF
ECHO Building Harness98 3.1.0 GUI compatibility probe...
SET CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe
IF EXIST "%CSC%" GOTO COMPILE
ECHO ERROR: The .NET Framework 2.0 compiler was not found.
ECHO Expected: %CSC%
GOTO FAILED
:COMPILE
"%CSC%" @GUIBUILD.RSP
IF ERRORLEVEL 1 GOTO FAILED
ECHO Build succeeded: H98GUI.EXE
GOTO END
:FAILED
ECHO Build failed.
:END
PAUSE
