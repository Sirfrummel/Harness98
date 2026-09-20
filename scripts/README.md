# Release automation

Run `scripts/release.sh` from anywhere to build and test all Harness98
executables, recreate the versioned fallback package, generate the stable update
manifest, and verify every SHA-256 hash.

## Linux requirements

- Bash
- Wine configured to run the .NET Framework 2.0 C# compiler
- `sha256sum`, `sed`, `find`, `grep`, `cut`, and standard file utilities

The compiler defaults to
`C:\windows\Microsoft.NET\Framework\v2.0.50727\csc.exe` inside the active Wine
prefix. Set `WINE_CSC` when it is installed elsewhere.

The version comes from `src/VersionInfo.cs`. The release script never packages
or publishes `HARNESS98.KEY`, `HARNESS98.CFG` from an installed machine,
conversation data, staged updates, or backups. The repository's default
`HARNESS98.CFG` template is included only in the fresh-install package and is
protected from updater manifests.

The normal local command does not touch the network:

```sh
scripts/release.sh
```

After reviewing the generated package and manifest, publish explicitly:

```sh
scripts/release.sh --publish
```

Publishing cleanly replaces the exact generated stable-channel and versioned
package directories so stale files from an earlier build cannot survive.

Set `HARNESS98_PUBLISH_URL` to override the default private LAN destination, or
`WINE_CSC` to override the .NET 2.0 compiler path used through Wine.
`kioclient5` is required only for `--publish` to the default SMB destination.
