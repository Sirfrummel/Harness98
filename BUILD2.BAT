@ECHO OFF
ECHO Building Windows 98 AI Harness v2 DLL benchmark...
SET CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe
IF EXIST "%CSC%" GOTO COMPILE
ECHO ERROR: The .NET Framework 2.0 compiler was not found.
ECHO Expected: %CSC%
GOTO FAILED
:COMPILE
"%CSC%" /nologo /target:exe /platform:x86 /optimize+ /define:LIBCURL_DLL /reference:LibCurlNet.dll /out:ORCHAT2.EXE SRC\*.CS
IF ERRORLEVEL 1 GOTO FAILED
ECHO Build succeeded: ORCHAT2.EXE
GOTO END
:FAILED
ECHO Build failed.
:END
