HARNESS98 - GUI, CLI, AND LAUNCHER
=================================

HARNESS98.EXE is now a permanent launcher.  It automatically applies a staged
update before starting H98GUI.EXE.  A desktop shortcut can safely point to
HARNESS98.EXE.  RUNCLI.BAT starts the command-line interface.  Both frontends
use the same API key, settings, conversation files, networking, and updater.

GUI FEATURES
------------

- Scrollable and selectable conversation transcript
- Enter sends; Shift+Enter inserts a new line
- Optional session cost and token toolbar under the View menu
- Live command and output progress while the agent is working
- Toggleable conversation sidebar and New Conversation command
- Reusable model picker with text, vision-input, and image-output filters
- Tool-capable model filter and COMMAND.COM command execution
- OpenRouter's original model ordering is preserved
- Settings for API key, update server, and automatic conversation titles
- Background requests with a replaceable status message
- Compact three-line prompt editor and visually distinct chat turns
- Update checks with Restart now and Later choices

The capability filters describe models advertised by OpenRouter.  Harness98
currently sends text messages only; image attachment and response
handling are future harness features.

COMMAND TOOL
------------

Models advertised by OpenRouter as supporting tools receive a run_command
tool.  Commands run through the COMSPEC command interpreter (COMMAND.COM on
Windows 98) with a 30-second timeout.  Harness98 returns the exit code and up
to 16 KB each of standard output and error output to the model.  Tool requests
and results are retained in saved conversations.

AUTOMATIC TITLES
----------------

Automatic titles are disabled by default.  When enabled in Settings, Harness98
makes one additional model request after the first successful reply.  You can
choose a dedicated title model or use the active conversation model.

UPGRADING FROM 3.0.0
--------------------

Run /update in the 3.0.0 CLI and restart with RUN3.BAT.  The updater preserves
HARNESS98.KEY, HARNESS98.CFG, and the CONVERSATIONS directory.  After the
update, RUN3.BAT launches the GUI and RUNCLI.BAT remains available for the CLI.

Upgrading from 3.1.1 requires one final launch through RUN3.BAT so the existing
GUI executable can be replaced by the launcher.  After that transition, launch
HARNESS98.EXE directly; it handles staged updates automatically.
