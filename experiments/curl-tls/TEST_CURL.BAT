@ECHO OFF
ECHO Windows 98 OpenRouter TLS 1.2 test
ECHO.
IF EXIST CURL.EXE GOTO HAVE_CURL
ECHO ERROR: CURL.EXE is missing.
GOTO END
:HAVE_CURL
IF EXIST CACERT.PEM GOTO RUN_TEST
ECHO ERROR: CACERT.PEM is missing.
GOTO END
:RUN_TEST
ECHO Curl version information:
CURL.EXE --version
ECHO.
ECHO Fetching OpenRouter's public model list...
CURL.EXE --fail --show-error --cacert CACERT.PEM --tlsv1.2 --connect-timeout 20 --max-time 60 -D HEADERS.TXT -o MODELS.JSN -w "HTTP status: %%{http_code}\n" https://openrouter.ai/api/v1/models
IF ERRORLEVEL 1 GOTO FAILED
ECHO.
ECHO SUCCESS: OpenRouter answered over HTTPS.
ECHO The response is saved as MODELS.JSN.
GOTO END
:FAILED
ECHO.
ECHO FAILED: curl returned an error. Please take a photo of this screen.
:END
ECHO.
PAUSE
