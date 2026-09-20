HARNESS98 3.1.0 GUI COMPATIBILITY PROBE
======================================

This is a standalone display and responsiveness test.  It does not connect to
OpenRouter and does not modify the installed Harness98 3.0.0 folder.

Run H98GUI.EXE.  If the executable is missing, run BUILD_GUI.BAT first.

Please test the following:

1. The window opens without an error.
2. The model box, transcript, prompt, Send, Clear, and status bar are visible.
3. The window can be resized without controls overlapping.
4. Text in the transcript can be selected and copied.
5. A long transcript gains a working vertical scrollbar.
6. Send a prompt.  The status bar should temporarily say Waiting, the window
   should remain responsive, and a simulated reply should appear after about
   1.5 seconds.  The status bar should then return to Ready.
7. Ctrl+Enter should send; an ordinary Enter should add a line to the prompt.

Nothing from this probe needs to be copied into the Harness98 3.0.0 folder.
