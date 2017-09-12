# eduVPN
Windows eduVPN Client

# Folders
- _bin_: Automation scripts
   - _Debug_: Debug binaries
   - _Release_: Release binaries
   - _Setup_: MSI packages
- _eduEd25519_: libsodium wrapper providing Ed25519 signing and verifying support to C#/.NET
- _eduJSON_: Lightweight JSON parser
- _eduOAuth_: OAuth 2.0 library
- _eduOpenVPN_: OpenVPN IPC for C#/.NET
- _eduVPN_: eduVPN client business logic (Model and View Model)
- _eduVPN.Client_: eduVPN client UI (View)

# Building
## Pre-requisites
- libsodium: Download libsodium [pre-compiled MSVC binaries](https://download.libsodium.org/libsodium/releases/) and extract them to `C:\SDK\libsodium`. (Or change `LIBSODIUM_SDK` in `eduEd25519\eduEd25519.props` to the folder of your choice.) `sodium.h` include file should be at `C:\SDK\libsodium\include\sodium.h`.
- Visual Studio 2017 Community Edition: The required VS2017 feature set to be announced later...

## Code Signing
In order to have the build process digitally sign the output files, one should provide the following:

1. A signing certificate installed in the building user’s certificate store.
2. The following variables in the environment:
  - `ManifestCertificateThumbprint` - set the value to certificate’s SHA1 thumbprint (hexadecimal, without spaces, i.e. `bc0d8da45f9eeefcbe4e334e1fc262804df88d7e`).
  - `ManifestTimestampRFC3161Url` - set the value to URL used to perform timestamp signature (i.e. `http://sha256timestamp.ws.symantec.com/sha256/timestamp`). In order to perform the timestamp signing successfully, the computer running the build should be online and able to access this URL.

## Initial Registration
1. Start the _Developer Command Prompt for VS2017_ elevated (_Start_ » _Run as Administrator_).
2. `cd` to the project folder. The one where `eduVPN.sln` and `Makefile` files are located.
3. Start the initial build and registration using `nmake Register` command. This command will:
   a) Build the Debug version for your architecture as defined by the `PROCESSOR_ARCHITECTURE` environment variable.
   b) Register the `org.eduvpn.app` custom URI scheme.
   c) Create a Start menu eduVPN Client shortcut.
4. The client can now be started using the Start menu shortcut.

## Debugging eduVPN Client
1. Perform the [initial registration](#initial-registration).
2. Open the `eduVPN.sln` file (with VS2017).
3. _Build_ » _Configuration Manager..._: Select desired active solution and platform. Please note _AnyCPU_ is not supported (yet).
4. In the _Solution Explorer_ pane, locate _eduVPN.Client_ project, right click on it, and _Set as StartUp Project_.
5. _Build_ » _Build Solution_.
6. _Debug_ » _Start Debugging_.

## Building MSI Packages

### Pre-requisites
- [WiX Toolset 3.11](http://wixtoolset.org/releases/v3.11/stable)

## Building
1. Start the _Developer Command Prompt for VS2017_. The elevation is not required.
2. `cd` to the project folder. The one where `eduVPN.sln` and `Makefile` files are located.
3. Start building using `nmake Setup` command.
4. The MSI packages will be saved to `bin\Setup` folder.
 