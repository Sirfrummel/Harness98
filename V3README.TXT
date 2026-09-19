HARNESS98 V3 - WINDOWS 98 TEST BUILD
===================================

Copy this entire folder to a local hard-drive directory on Windows 98.
Do not run it directly from the network share.

Start Harness98 by double-clicking RUN3.BAT.  The batch file applies any
previously staged update before it launches HARNESS98.EXE.

Conversations are saved automatically in the CONVERSATIONS directory.
Useful commands include:

  /new         Start a new conversation
  /continue    Resume the most recently updated conversation
  /resume      Show the saved conversation list
  /resume ID   Resume a specific ID, for example C000001
  /update      Check the configured update share
  /help        Show every command

HARNESS98.CFG contains the update-server path.  HARNESS98.KEY contains the
OpenRouter key and is never replaced by the updater.  Conversations, the key,
and the configuration file are also excluded from update packages.

When /update finds a newer version, it validates every file and says to
restart.  Exit Harness98 and launch RUN3.BAT again to apply it.  The files
that were replaced are retained in UPDATE-BACKUP.
