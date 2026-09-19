HARNESS98 3.1.0 - GUI AND CLI
============================

RUN3.BAT starts the graphical Harness98 application.  RUNCLI.BAT starts the
command-line interface.  Both programs use the same API key, settings,
conversation files, model catalog, networking code, and updater.

GUI FEATURES
------------

- Scrollable and selectable conversation transcript
- Multiline prompt editor; Ctrl+Enter sends
- Conversation selector and New Conversation command
- Reusable model picker with text, vision-input, and image-output filters
- Settings for API key, update server, and automatic conversation titles
- Background requests with a replaceable status message
- Update checks under the Tools menu

The capability filters describe models advertised by OpenRouter.  Harness98
3.1.0 still sends text messages only; image attachment and image response
handling are future harness features.

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
