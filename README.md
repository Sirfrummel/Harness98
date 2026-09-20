# Harness98

Harness98 is an experimental AI chat and agent harness built to run natively
on Windows 98. It connects to models through OpenRouter, preserves
conversations locally, and can let tool-capable models run commands and work
with text files on the computer.

The project includes both a Windows Forms interface and a command-line
interface over the same application core.

> [!WARNING]
> Harness98 is experimental software for a vintage operating system. Agent
> commands and file operations run without an approval prompt or sandbox. A
> model can change or delete any data available to the Windows account. Use it
> only on a machine and files you are prepared to restore.

## Status

The current version is **3.7.1**. It has been tested on a physical Windows 98
Second Edition computer with the .NET Framework 2.0 CLR.

Working features include:

- GUI and CLI frontends backed by a shared core
- OpenRouter model discovery, filtering, and selection
- Multi-turn conversations saved locally as JSON
- Resume, new-conversation, and model-switching controls
- Optional automatic conversation titles
- Session token and cost tracking, with a configurable cost warning
- Live tool-call progress in the GUI
- `run_command`, `read_file`, `write_file`, and `edit_file` agent tools
- Bounded command output and file reads
- Basic fenced-code formatting in the GUI transcript
- LAN-based update checking and restart-to-apply updates

Image-capability filters reflect OpenRouter model metadata. Harness98 does not
yet send or receive image attachments.

## Requirements

### To run Harness98

- Windows 98 Second Edition
- .NET Framework 2.0 (`v2.0.50727`)
- Microsoft Visual C++ 2005 runtime, providing `MSVCR80.dll`
- Working IPv4 networking and DNS
- An OpenRouter API key
- The complete Harness98 release folder, including:
  - `HARNESS98.EXE`, `H98GUI.EXE`, and `H98CLI.EXE`
  - `LibCurlNet.dll`, `LibCurlShim.dll`, and `libcurl.dll`
  - `libeay32.dll` and `ssleay32.dll`
  - `CACERT.PEM`

The native curl/OpenSSL dependency set is necessary because the .NET 2.0 HTTP
stack on Windows 98 cannot negotiate the modern TLS required by OpenRouter.
Keep the executables, DLLs, and certificate bundle together in one folder.

### To build on Windows 98

- The .NET Framework 2.0 C# compiler at
  `%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe`
- The dependency files under `vendor/libcurl-win98/`
- `MSVCR80.dll` installed in the Windows system directory or placed beside the
  Harness98 executables

The Linux release script additionally expects Bash, Wine with the .NET 2.0
compiler configured, and standard Unix tools such as `sha256sum`.

## Installation

1. Copy the **entire** release folder to the Windows 98 computer. A short local
   path such as `C:\HARNESS98` is convenient.
2. Confirm that .NET Framework 2.0 and the Visual C++ 2005 runtime are
   installed.
3. Run `HARNESS98.EXE` for the graphical interface.
4. Enter an OpenRouter API key when prompted, then choose a model.

`HARNESS98.EXE` is the permanent launcher. It applies any previously staged
update before opening `H98GUI.EXE`, so a desktop shortcut should point to the
launcher. Run `H98CLI.EXE` or `RUNCLI.BAT` to use the command-line interface.

## API-key storage

Harness98 stores the OpenRouter key in `HARNESS98.KEY` beside the program. The
file is plain text because Windows 98 does not provide a suitable modern
credential store.

- Do not share or commit `HARNESS98.KEY`.
- Do not put the installation folder on a share accessible to untrusted users.
- Use the Settings window or the CLI `/key` command to replace the saved key.
- Delete `HARNESS98.KEY` to make Harness98 request a key on the next launch.

`HARNESS98.KEY`, the older `OPENROUT.KEY` filename, and general `*.key` files
are excluded by `.gitignore`. Release packaging also aborts if it finds a key
file in the assembled package.

## Interfaces

### Graphical interface

The GUI provides a scrollable rich-text transcript, a toggleable conversation
sidebar, a searchable model picker, a three-line composer, settings, update
checks, and an optional session-cost toolbar. Press **Enter** to send and
**Shift+Enter** to insert a new line.

### Command-line interface

The CLI supports these commands:

| Command | Action |
| --- | --- |
| `/new` | Start a new conversation |
| `/continue` | Resume the most recently updated conversation |
| `/resume` | Choose from saved conversations |
| `/resume ID` | Resume a conversation by ID |
| `/clear` | Preserve the current chat and start an empty one |
| `/model` | Choose a different model |
| `/key` | Replace the saved OpenRouter key |
| `/update` | Check for and stage an update |
| `/help` | Show the command list |
| `/exit` | Exit Harness98 |

## Agent tools and limits

Harness98 offers tools to models that OpenRouter reports as supporting tool
calls:

| Tool | Behavior |
| --- | --- |
| `run_command` | Runs a command through `%COMSPEC%`—normally `COMMAND.COM`—with a 30-second timeout |
| `read_file` | Reads text files in bounded ranges; binary-looking files are refused |
| `write_file` | Creates or replaces a text file, up to 65,536 characters |
| `edit_file` | Replaces one exact, unique text match in a file |

Command output returned to the model is capped at 8 KiB of standard output and
2 KiB of standard error. A file read returns at most 1,000 lines and 32,768
characters. The default tool-round limit is 10 and can be changed under
**Tools > Settings > Limits**.

Relative paths and command working directories start from the Harness98
installation directory. The harness also tells the model about a scratch
directory named `HARNESS98` beneath the Windows temporary directory.

Windows 98 `COMMAND.COM` does not support every feature found in modern shells.
For multi-step work, an agent may need to write and invoke a temporary batch
file instead of using modern command-chaining syntax.

## Local data and configuration

Runtime data stays beside the application unless noted otherwise:

| Path | Purpose |
| --- | --- |
| `HARNESS98.KEY` | Plain-text OpenRouter API key |
| `HARNESS98.CFG` | Update server, automatic-title, tool-limit, and cost-warning settings |
| `CONVERSATIONS\` | Saved conversation JSON files |
| `UPDATE-STAGE\` | Downloaded update waiting for restart |
| `%TEMP%\HARNESS98\` | Suggested scratch directory for agent-created temporary files |

Empty conversations are not saved. The updater protects credentials,
configuration, and conversation data from replacement.

The repository's default `UPDATE_SERVER` points to the private development LAN
share and will not work elsewhere. Set your own path in the GUI settings or in
`HARNESS98.CFG`, or simply leave update checks unused.

## Building

On Windows 98, run:

```bat
BUILD31.BAT
```

This builds the launcher, GUI, CLI, updater, and restart helper with the .NET
2.0 compiler. The response files keep compiler options compatible with the
target machine.

For a reproducible package and update manifest from the development system,
run:

```sh
scripts/release.sh
```

The script builds all executables, runs the compatibility tests, creates a
credential-free package, generates SHA-256 hashes, and stops before publishing.
Publishing is an explicit separate action:

```sh
scripts/release.sh --publish
```

See [`scripts/README.md`](scripts/README.md) for release-script configuration.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/` | Shared application core, OpenRouter client, storage, updates, and agent tools |
| `gui/` | Windows Forms frontend |
| `launcher/` | Stable GUI launcher and update handoff |
| `updater/` | Restart-time update installer |
| `restarter/` | GUI restart helper |
| `tests/` | .NET 2.0 compatibility tests |
| `vendor/libcurl-win98/` | Matched LibCurl.NET and native TLS dependencies |
| `probe/`, `curl-probe/`, `gui-probe/` | Early compatibility probes retained for reference |
| `scripts/` | Release and publishing automation |

Third-party binary provenance and hashes are documented in
[`THIRD_PARTY.md`](THIRD_PARTY.md).

## Compatibility notes

The original compatibility investigation established that:

- The application compiles and runs with the .NET Framework 2.0 toolchain on
  the target Windows 98 machine.
- .NET's native HTTPS stack is too old for OpenRouter's current TLS endpoint.
- The bundled curl/OpenSSL stack negotiates TLS 1.2 and successfully reaches
  the OpenRouter API on the physical target.
- Some older routers may mishandle cold-cache DNS resolution for the `.ai`
  top-level domain when queried by Windows 98. A different DNS resolver or a
  cache-warming lookup from a modern computer can help diagnose that specific
  failure.

## Versioning

Harness98 uses `major.minor.patch` version numbers. Feature groups normally
advance the minor version during rapid development, compatible fixes advance
the patch version, and major versions are reserved for fundamental changes to
the program or its compatibility contract.

The application version has one source of truth:
[`src/VersionInfo.cs`](src/VersionInfo.cs).

## Security and redistribution

- Treat saved conversations as private data; they can contain prompts, model
  responses, tool calls, command output, and file contents.
- OpenRouter requests leave the local machine and are subject to the selected
  provider and model's policies and pricing.
- The bundled native libraries and certificate bundle are retained for
  reproducibility. Review [`THIRD_PARTY.md`](THIRD_PARTY.md) and
  [`vendor/libcurl-win98/README.md`](vendor/libcurl-win98/README.md) before
  redistributing binaries.
- This repository does not currently include a project-level license. Public
  source availability alone does not grant redistribution or modification
  rights; add a license before accepting outside contributions or releases.
