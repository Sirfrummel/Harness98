@ECHO OFF
ECHO OpenRouter Windows 98 connectivity test
ECHO.

SET CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe

IF EXIST "%CSC%" GOTO COMPILE

ECHO ERROR: The .NET Framework 2.0 C# compiler was not found at:
ECHO %CSC%
ECHO.
ECHO Confirm that the v2.0.50727 framework directory is installed.
GOTO END

:COMPILE
ECHO Compiling ORPROBE.CS with .NET Framework 2.0...
"%CSC%" /nologo /target:exe /out:ORPROBE.EXE ORPROBE.CS
IF ERRORLEVEL 1 GOTO BUILDFAIL

ECHO.
ECHO Compilation succeeded. Running ORPROBE.EXE...
ECHO.
ORPROBE.EXE
IF ERRORLEVEL 1 GOTO RUNFAIL

ECHO.
ECHO The probe completed successfully.
GOTO END

:BUILDFAIL
ECHO.
ECHO BUILD FAILED. Please copy or photograph all output above.
GOTO END

:RUNFAIL
ECHO.
ECHO THE TEST REPORTED A FAILURE. Please copy or photograph all output above.

:END
ECHO.
PAUSE
