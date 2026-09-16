# Third-party files

## `curl-probe/curl.exe`

- Project: `OmegaAOL/curl-windows98`
- Project URL: https://github.com/OmegaAOL/curl-windows98
- Release asset: `curl-7.42.1-BINARY-ZLIB-LDAP.rar`
- Asset URL: https://github.com/OmegaAOL/curl-windows98/releases/download/ldap_zlib_release/curl-7.42.1-BINARY-ZLIB-LDAP.rar
- Asset SHA-256: `46ba91402b2b81ddfc90dea5dffa0491e0913d45d1eeac2899151a9041033370`
- Extracted executable SHA-256: `88f6e1abef0bac25d758f644ba79b0a18b2966487c552fb6d689840a09d82d04`
- Executable size: 1,650,688 bytes

The executable reports:

```text
curl 7.42.1 (i386-pc-win32) libcurl/7.42.1 OpenSSL/1.0.2u
Protocols: dict file ftp ftps gopher http https imap imaps ldap pop3 pop3s rtsp smb smbs smtp smtps telnet tftp
Features: AsynchDNS Largefile NTLM SSL
```

It is a PE32 Intel 80386 console executable with a Windows 4.0 subsystem target.
It was tested against `https://openrouter.ai/api/v1/models` before staging and
again on the target Windows 98 machine.

The upstream repository does not declare a repository-level license. Review the
licenses and redistribution terms of curl, OpenSSL, zlib, and any other bundled
components before redistributing this binary beyond this restoration project.

## `curl-probe/cacert.pem`

- Source: https://curl.se/ca/cacert.pem
- SHA-256: `f66dff1bdf8f96060b8177976f8b7d9254bc89bc4db933d769f7384d28480bc9`
- Retrieved: 2026-09-15
- Bundle metadata: Mozilla CA certificate store as of 2026-08-13

This CA bundle is runtime data used by curl to authenticate HTTPS servers. It
will need periodic replacement if the target sites move to certificate chains
that the bundled roots cannot validate.

## LibCurl.NET Windows 98 DLL set

Version 2 uses the five matched DLLs preserved under `vendor/libcurl-win98/`.
Their source, individual hashes, dependency purpose, and redistribution caveat
are recorded in that directory's `README.md`.
