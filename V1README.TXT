WINDOWS 98 AI HARNESS - VERSION 1
=================================

REQUIREMENTS
------------

- Windows 98 Second Edition
- Microsoft .NET Framework 2.0
- Working network connection
- An OpenRouter API key

STARTING THE PROGRAM
--------------------

Keep ORCHAT.EXE, CURL.EXE, and CACERT.PEM in the same folder. Double-click
RUN.BAT to start the program. If ORCHAT.EXE is missing, RUN.BAT builds it from
the files in the SRC folder first.

On first run, enter your OpenRouter API key. The program hides the key while
you type it and saves it in OPENROUT.KEY beside the program. Windows 98 cannot
protect this file with modern credential storage, so it is plain text. Do not
share that file.

MODEL SELECTION
---------------

The first screen lists models in pages. Type part of a model name or ID and
press Enter to filter the list. Enter a displayed number to select a model.
Use N and P to move between pages, or Q to quit.

CHAT COMMANDS
-------------

/help   Show the command list
/clear  Clear conversation history
/model  Choose another model while retaining the conversation
/key    Replace the saved OpenRouter key
/exit   Exit the program

Version 1 sends one request at a time and keeps the conversation history in
memory until it is cleared or the program exits. It does not yet stream output,
save conversations, run commands, or edit files.
