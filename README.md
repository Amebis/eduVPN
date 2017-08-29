# eduVPN
Windows eduVPN Client

# Building
## Pre-requisites
### libsodium
Download libsodium [pre-compiled MSVC binaries](https://download.libsodium.org/libsodium/releases/) and extract them to `C:\SDK\libsodium`. (Or change `LIBSODIUM_SDK` in `eduEd25519\eduEd25519.props` to the folder of your choice.) Include files should be in `C:\SDK\libsodium\include`.
### Visual Studio 2017 Community Edition
The required VS2017 feature set to be announced later...

## Code Signing
In order to have the build process digitally sign output files, one should provide the following:

1. A signing certificate installed in the building user’s certificate store.
2. The following variables in the environment:
  - `ManifestCertificateThumbprint` - set the value to certificate’s SHA1 thumbprint (hexadecimal, without spaces, i.e. `bc0d8da45f9eeefcbe4e334e1fc262804df88d7e`).
  - `ManifestTimestampRFC3161Url` - set the value to URL used to perform timestamp signature (i.e. `http://sha256timestamp.ws.symantec.com/sha256/timestamp`). In order to perform the timestamp signing successfully, the computer running the build should be online and able to access this URL.

## eduVPN Client
1. Open the `eduVPN.sln` file (with VS2017).
2. _Build_ » _Configuration Manager..._: Select desired active solution and platform. Please note _AnyCPU_ is not supported (yet).
3. In the _Solution Explorer_ pane locate _eduVPN.Client_ project, right click on it and _Set as StartUp Project_.
4. _Build_ » _Build Solution_: You shall find the output files in the `bin` folder relative to the `eduVPN.sln` file.
5. Register `org.eduvpn.app` custom URI scheme to launch your compiled `eduVPN.Client.exe` following the instructions published at [1.0-alpha release](https://github.com/Amebis/eduVPN/releases/tag/1.0-alpha).
6. _Debug_ » _Start Debugging_.

# Architecture
- eduEd25519: libsodium wrapper exposing Ed25519 signing and verifying support to C#/.NET
- eduJSON: Lightweight JSON parser
- eduOAuth: OAuth 2.0 library
- eduOpenVPN: OpenVPN IPC via C# API
- eduVPN: eduVPN client business logic (Model and View Model)
- eduVPN.Client: eduVPN client UI (View)
