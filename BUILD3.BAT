@ECHO OFF
ECHO Building Harness98 v3...
SET CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe
IF EXIST "%CSC%" GOTO COMPILE
ECHO ERROR: The .NET Framework 2.0 compiler was not found.
ECHO Expected: %CSC%
GOTO FAILED
:COMPILE
"%CSC%" /nologo /target:exe /platform:x86 /optimize+ /out:HARNESS98-UPDATER.EXE UPDATER\UPDATER.CS SRC\UPDATEMANIFEST.CS
IF ERRORLEVEL 1 GOTO FAILED
"%CSC%" /nologo /target:exe /platform:x86 /optimize+ /define:LIBCURL_DLL;HARNESS98_V3 /reference:LibCurlNet.dll /out:HARNESS98.EXE SRC\*.CS
IF ERRORLEVEL 1 GOTO FAILED
ECHO Build succeeded: HARNESS98.EXE
GOTO END
:FAILED
ECHO Build failed.
:END
