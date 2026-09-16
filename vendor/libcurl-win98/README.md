# LibCurl.NET Windows 98 dependency set

These files are the matched developer-use dependency set from:

https://github.com/OmegaAOL/curl-windows98/tree/main/LibCurl.NET%20%2B%20libraries%20%28for%20developer%20use%29

Retrieved 2026-09-15. They are committed to preserve a reproducible Windows 98
build. The version 2 distribution places all files beside `ORCHAT2.EXE`, as
required by the Windows DLL loader.

`libcurl.dll`, `libeay32.dll`, and `ssleay32.dll` import `MSVCR80.dll`. That
Microsoft Visual C++ 2005 runtime file is not present in the upstream folder and
is therefore not vendored here. Version 2 checks for it beside the program and
in the Windows system directory before initializing curl.

| File | SHA-256 |
| --- | --- |
| `LibCurlNet.dll` | `ae15237f53c5460f7f58c3c646cfe328afcf800c21e0e673667783810698bf77` |
| `LibCurlShim.dll` | `5096b176b53c95957f2bce9e7ee835e5b3e9af7197ce0b76708da48000f7382a` |
| `libcurl.dll` | `da7f86acb234e529f701e8c58e4621a293200dbc5d7060a188fe4257898de991` |
| `libeay32.dll` | `c2a0a89463ac9cff2bd9ed0df00467c591c5eed72426811cc925c763ba53779a` |
| `ssleay32.dll` | `cf5634d4745b96afce8822c3e97ddfc4b6fec23062cb75ae5794af02b765fdde` |

The upstream repository does not declare a repository-level license. Review
the redistribution terms of LibCurl.NET, curl, OpenSSL, and the shim before
distributing these binaries outside this restoration project.
