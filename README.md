# Harness98

Harness98 is a small command-line AI harness that can run natively
on Windows 98, call OpenRouter, and eventually operate on local files and run
approved commands.

The project is intentionally being developed in small, testable stages on the
target computer. The immediate goal is to prove each compatibility layer before
choosing the larger harness architecture.

## Confirmed environment

- Windows 98 Second Edition on the target computer
- .NET Framework 2.0 compiler and CLR `2.0.50727.42`
- Working IPv4 network access and SMB file transfer
- The public OpenRouter model endpoint is reachable using TLS 1.2

## Findings so far

1. `probe/` compiles and runs directly on the Windows 98 machine with the .NET
   Framework 2.0 C# compiler.
2. The native .NET HTTP stack offers SSL 3.0 and TLS 1.0, so its direct HTTPS
   request is rejected by the modern OpenRouter endpoint.
3. OpenRouter's HTTP endpoint redirects to HTTPS and does not avoid the TLS
   requirement.
4. The Windows 98 curl build in `curl-probe/` includes OpenSSL 1.0.2u and can
   negotiate TLS 1.2. It successfully downloaded OpenRouter's public model list
   and received HTTP status 200 on the target computer.
5. The LAN router at `192.168.50.1` exhibited a cold-cache DNS issue for the
   `.ai` TLD. Initial replies were marked truncated and required a DNS-over-TCP
   retry that Windows 98 did not complete. A modern client's retry warmed the
   router's `.ai` delegation cache, after which new `.ai` names resolved on the
   Windows 98 machine. A public DNS resolver may be the eventual workaround.

## Repository layout

- `src/` — modular source for the Harness98 interactive chat program
- `BUILD.BAT` and `RUN.BAT` — Windows 98 build and launch scripts
- `V1README.TXT` — target-machine instructions and command reference
- `BUILD2.BAT` and `RUN2.BAT` — LibCurl.NET transport benchmark scripts
- `V2README.TXT` — version 2 dependency and test instructions
- `BUILD3.BAT`, `RUN3.BAT`, and `V3README.TXT` — Harness98 v3 build,
  update-aware launcher, and target-machine instructions
- `updater/` — the small restart-time update installer
- `gui/` — Windows Forms frontend for Harness98 3.1
- `gui-probe/` — standalone Windows Forms compatibility probe for the 3.1 line
- `scripts/release.sh` — build, test, package, manifest, verification, and
  explicit publishing automation
- `vendor/libcurl-win98/` — matched managed/native DLL dependency set
- `probe/` — source and batch file for the original .NET connectivity probe
- `curl-probe/` — known-working curl/OpenSSL TLS 1.2 connectivity probe
- `THIRD_PARTY.md` — provenance and hashes for bundled third-party files

Both Windows 98 batch files use DOS-compatible CRLF line endings.

## Running the successful probe

Copy the complete `curl-probe` directory to a short local path such as
`C:\ORCURL` on Windows 98, then run `TEST_CURL.BAT`.

No OpenRouter key is required for this public-endpoint test. A successful run
creates `MODELS.JSN` and `HEADERS.TXT`; both are generated files and are ignored
by Git.

## Security

Never commit an OpenRouter API key or another credential. Runtime credentials
will be kept outside version control. Third-party executables are recorded with
their source URLs and SHA-256 hashes in `THIRD_PARTY.md`.

## Version 1 scope

Version 1 uses a modular .NET 2.0 console application with the verified curl
binary as its HTTPS transport. It provides local API-key storage, model-list
filtering and selection, a multi-turn in-memory conversation, and commands to
clear history, switch models, or replace the key.

An authenticated end-to-end test using OpenRouter's free-model router completed
successfully: the first response matched the requested text, and a second turn
correctly recalled that response from the submitted message history. The
complete version 1 program was also validated on the physical Windows 98 target.

Streaming output, saved conversations, command execution, and file editing are
intentionally deferred to later milestones.

## Version 2 transport benchmark

Version 2 holds the user interface, OpenRouter client, JSON handling, and
conversation behavior constant while replacing the `CURL.EXE` child process
with an in-process LibCurl.NET transport. It has no automatic process-transport
fallback and prints its active transport at startup, making physical Windows 98
results unambiguous.

The local x86 compatibility test completed the same parity sequence as version
1: model retrieval, an authenticated first response, and a second response that
retained the first turn. Unlike version 1, the DLL transport created no request
or response files. The DLL transport, model selection, and multi-turn chat were
validated on the physical Windows 98 computer.

## Harness98 version 3

Version 3 adopts the Harness98 name and retains the proven LibCurl.NET
transport. Conversations are stored as individual JSON files with stable IDs;
`/continue`, `/resume`, `/resume ID`, and `/new` manage them.

The `/update` command reads `HARNESS98.CFG`, checks a manifest on the configured
LAN share, copies and SHA-256-verifies a newer release into `UPDATE-STAGE`, and
asks the user to restart. `RUN3.BAT` invokes the separate updater before launch,
allowing loaded program files to be replaced safely. Replaced files are backed
up, while credentials, configuration, and conversations are protected from the
update manifest.

## Versioning

Harness98 uses `major.minor.patch` version numbers. During rapid development,
substantial experiments and feature groups normally advance the minor version;
compatible fixes advance the patch version. Major versions are reserved for a
fundamental change to the program or its compatibility contract. The 3.0.0
release established the persistent CLI and updater. Version 3.1.0 adds a
Windows Forms frontend while retaining the CLI over a shared application core.

## Harness98 version 3.1

`HARNESS98.EXE` is the graphical frontend and `H98CLI.EXE` is the command-line
frontend. They share the OpenRouter client, model metadata, conversation store,
configuration, update service, and send/save behavior through `HarnessCore`.
The GUI adds conversation management, a reusable model picker with multimodal
capability filters, background requests, and settings for credentials, updates,
and optional model-generated conversation titles.

Version 3.1.1 refines the chat layout with a toggleable conversation sidebar,
a more compact composer, and distinct `>` user and `:` assistant transcript
styling.

Version 3.1.2 makes `HARNESS98.EXE` a stable launcher for `H98GUI.EXE`, allowing
the same desktop shortcut to apply staged updates before opening the GUI. Empty
conversations remain transient and are not added to saved history.
Model lists preserve the ordering returned by OpenRouter rather than sorting
alphabetically.

Version 3.4.2 adds role-aware transcript colors for user messages, assistant
responses, command requests, successful command output, and command failures.

## Release process

The application version has one source of truth in `src/VersionInfo.cs`.
`scripts/release.sh` builds every executable with the .NET 2.0 compiler, runs
the compatibility suite, assembles a clean credential-free package, generates
and verifies the update manifest, and stops before publishing. Passing
`--publish` explicitly uploads the stable channel and fallback package.
