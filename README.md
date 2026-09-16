# Windows 98 AI Harness

This project is exploring a small command-line AI harness that can run natively
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

- `src/` — modular source for the version 1 interactive chat program
- `BUILD.BAT` and `RUN.BAT` — Windows 98 build and launch scripts
- `V1README.TXT` — target-machine instructions and command reference
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
correctly recalled that response from the submitted message history. Physical
Windows 98 validation of the complete version 1 program is the remaining step.

Streaming output, saved conversations, command execution, and file editing are
intentionally deferred to later milestones.
