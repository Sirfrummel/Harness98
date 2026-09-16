WINDOWS 98 AI HARNESS - VERSION 2 DLL BENCHMARK
================================================

PURPOSE
-------

Version 2 keeps the version 1 interface and conversation behavior but replaces
the separate CURL.EXE process with the supplied LibCurl.NET DLL stack. The
program must print this line at startup:

Transport: LibCurl.NET DLL

There is no automatic CURL.EXE fallback in this benchmark.

STARTING THE PROGRAM
--------------------

Keep ORCHAT2.EXE, CACERT.PEM, and all five DLL files together in one folder.
Double-click RUN2.BAT to start. If ORCHAT2.EXE is missing, RUN2.BAT builds it
from the files in the SRC folder.

The native curl and OpenSSL files also require MSVCR80.DLL from the Microsoft
Visual C++ 2005 runtime. The program checks both its own folder and the Windows
system folder and reports a clear error if that runtime is missing.

OPENROUT.KEY stores the OpenRouter key locally in plain text. Do not share that
file outside a trusted system.

The model-selection and chat commands are unchanged from version 1:

/help   Show the command list
/clear  Clear conversation history
/model  Choose another model while retaining the conversation
/key    Replace the saved OpenRouter key
/exit   Exit the program
