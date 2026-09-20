HARNESS98 - WINDOWS 98 QUICK START
=================================

REQUIREMENTS
------------

- Windows 98 Second Edition
- Microsoft .NET Framework 2.0
- Microsoft Visual C++ 2005 runtime (MSVCR80.DLL)
- Working IPv4 networking and DNS
- An OpenRouter API key

Keep the entire release package together.  Harness98 requires its managed and
native LibCurl/OpenSSL DLLs plus CACERT.PEM beside the executables.

STARTING HARNESS98
------------------

Run HARNESS98.EXE for the graphical interface.  This is the permanent launcher
and the correct target for a desktop shortcut.  It applies a staged update, if
one exists, and then starts H98GUI.EXE.

Run RUNCLI.BAT or H98CLI.EXE for the command-line interface.

On first use, Harness98 asks for an OpenRouter API key and saves it as plain
text in HARNESS98.KEY beside the program.  Do not share that file.  Settings,
credentials, and saved conversations are preserved during updates.

GUI NOTES
---------

- Enter sends a message; Shift+Enter inserts a new line.
- While a response is active, Send changes to Queue.  Queued messages run in
  order after the current response finishes.
- A command running for ten seconds shows Stop command beneath the Queue
  button.  Stopping terminates COMMAND.COM and child processes Harness98 can
  discover, then asks the model for a final response without more tools.
- The hard command timeout is 30 seconds.
- View > Cost Toolbar shows session cost and token totals.
- Tools > Settings controls the key, update path, titles, tool limit, and cost
  warning.

AGENT TOOLS
-----------

Tool-capable models may receive run_command, read_file, write_file, and
edit_file.  These tools run without a confirmation prompt or sandbox.  A model
can change or delete any data available to the current Windows account.

Use Harness98 only on a computer and files you are prepared to restore.

UPDATES
-------

Choose Tools > Check for updates.  If a release is found and staged, choose
Restart now.  HARNESS98.EXE applies the update before reopening the GUI.

BUILDING FROM SOURCE ON WINDOWS 98
----------------------------------

The .NET Framework 2.0 C# compiler must exist at:

  %WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe

Run BUILD.BAT from the repository or source-package root.  Compiler response
files are stored under BUILD\WINDOWS98.
