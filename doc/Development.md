# Development of eduVPN Clients for Windows


## Folders

- _bin_: Automation scripts
   - _Debug_: Debug binaries
   - _Release_: Release binaries
   - _OpenVPN_: OpenVPN upstream binaries
   - _Setup_: MSI packages and EXE installer
- _eduMSICA_: MSI custom actions
- _eduOAuth_: OAuth 2.0 library (for importing legacy OAuth tokens)
- _eduOpenVPN_: OpenVPN IPC for C#/.NET
- _eduVPN_: Client logic (Model and View Model)
- _eduVPN.Client_: eduVPN client application (Shell)
- _eduVPN.Resources_: Native resources for Windows MUI - localizable names and descriptions for Start Menu shortcuts
- _eduVPN.Views_: Client UI (View)
- _eduvpn-common_: eduvpn-common source code
- _eduWGSvcHost_: WireGuard manager and tunnel services
- _eduWireGuard_: WireGuard Tunnel Manager Service IPC for C#/.NET
- _govVPN.Client_: govVPN client application (Shell)
- _Install_: WiX installer additional files
- _LetsConnect.Client_: Let's Connect! client application (Shell)
- _openvpn_: OpenVPN source code
- _WinStd_: Win32 Standard C++ helpers


## Pre-requisites

1. Install [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/). A minimum set of required features is:
   - Workloads
      - .NET desktop development
      - Desktop development with C++
   - Individual components
      - Code tools
         - Git for Windows (if not installed by other means)
      - Compilers, build tools, and runtimes
         - MSVC v143 ARM64, ARM64EC and x64/x86 build tools (Latest)
         - MSVC v143 ARM64, ARM64EC and x64/x86 Spectre-mitigated libs (Latest)
         - Python 3 (required by openvpn submodule)
      - SDKs, libraries, and frameworks
         - Windows 11 SDK
2. Install [vcpkg](https://vcpkg.io/).
   - `git clone https://github.com/Microsoft/vcpkg.git C:\ProgramData\SDK\vcpkg`
      The path used is only an example. If you decide to install to `C:\Program Files` or any other path that requires elevation to make folder modifications, you must grant user account, used to build eduVPN, read and write permissions to vcpkg folder. Otherwise, all build targets will require elevation too. vcpkg builds inside this folder and then copies .h/.lib/.dll files to your eduVPN working tree in the vcpkg_installed folders. To avoid vcpkg access-denied issues on building, it is easiest to install vcpkg somewhere in your user home folder.
   - Bootstrap with `C:\ProgramData\SDK\vcpkg\bootstrap-vcpkg -disableMetrics`
   - Integrate into MSBuild/Visual Studio with `C:\ProgramData\SDK\vcpkg\vcpkg integrate install`
   - You must keep vcpkg up-to-date manually:
     ```cmd
     cd C:\ProgramData\SDK\vcpkg
     git clean -fdx
     git pull
     bootstrap-vcpkg -disableMetrics
     ```
3. Install [Go](https://go.dev/) 1.18 or later. The last Go version that produces Windows 7-compatible binaries is 1.20.x.
4. Clone the eduVPN project source code _including_ sub-modules from the [eduVPN GitHub repository](https://github.com/Amebis/eduVPN) using `git clone --recurse-submodules https://github.com/Amebis/eduVPN.git eduVPN` command.
5. Install .NET Framework 3.5.x: can be installed from _Control Panel_ » _Programs and Features_ » _Turn Windows features on or off_ (required by WiX Toolset).
6. Install [WiX Toolset 3.14.0.5722 or compatible (See _Development Builds_ section)](https://wixtoolset.org/docs/wix3/#development-builds) (required for MSI and EXE installer packaging). Currently, this project does not support WiX Toolset 4 yet, as v4 is not a snap-in replacement for v3.14.


### Code Signing and Minisign

In order to have the build process digitally sign the release output files, one should setup either:

- Local signing:
   1. A signing certificate/hardware key
   2. The following variables in the environment:
      - `ManifestCertificateThumbprint` - set the value to certificate’s SHA1 thumbprint (hexadecimal, without spaces, e.g. `bc0d8da45f9eeefcbe4e334e1fc262804df88d7e`).
      - `ManifestTimestampRFC3161Url` - set the value to URL used to perform timestamp signature (e.g. `http://sha256timestamp.ws.symantec.com/sha256/timestamp`, `http://timestamp.digicert.com` etc.). In order to perform the timestamp signing successfully, the computer running the build should be online and able to access this URL.

- Microsoft Trusted Signing:
   1. Install [Trusted Signing dlib package](https://www.nuget.org/packages/Microsoft.Trusted.Signing.Client):
      ```cmd
      nuget install Microsoft.Trusted.Signing.Client -Version 1.0.53 -x`
      ```
   2. Provide a [`manifest.json`](https://learn.microsoft.com/en-us/azure/trusted-signing/how-to-signing-integrations#create-a-json-file) file and place it at `%APPDATA%\Microsoft.Trusted.Signing.Client.json`:
      ```cmd
      notepad "%APPDATA%\Microsoft.Trusted.Signing.Client.json"
      ```

In order to have the build process produce `.minisig` files for publishing, one should provide the following:

1. Have `minisign.exe` in path.
2. Have Minisign private key in `%USERPROFILE%\.minisign\minisign.key`. Run `minisign -G` to create a new keypair.
3. Either set `MinisignKeyPassword` environment variable, or call `nmake <flags> <target> MINISIGNKEYPASSWORD=<password>` when building.


### VirusTotal Submissions

In order to have the build process submit all release binaries to the VirusTotal for analysis, one should provide own VirusTotal API key in the `VirusTotalAPIKey` environment variable.


## Building and Packaging


### General Building Guidelines

All `nmake` commands should be invoked from the _x64 Native Tools Command Prompt for VS 2022_ (_Start_ » _All Programs_ » _Visual Studio 2022_ » _Visual Studio Tools_ » _VC_ » _x64 Native Tools Command Prompt for VS 2022_). x86 build environment can be used when the 32-bit version is preferred.
This command prompt sets all Visual Studio 2022 environment variables required by the command line building.

As dependencies take long time to build, separate `nmake` commands are available: `nmake builddeps` and `nmake cleandeps`.

`nmake register` and `nmake unregister` require elevation. Start the _x64 Native Tools Command Prompt for VS 2022_ elevated (_Start_ » _All Programs_ » _Visual Studio 2022_ » _Visual Studio Tools_ » _VC_ » Right click _x64 Native Tools Command Prompt for VS 2022_ » _Run as Administrator_). Other `nmake` commands can be run from a non-elevated _x64 Native Tools Command Prompt for VS 2022_.

Before pulling a new version of the eduVPN source code from the GitHub a `nmake unregister` and `nmake clean cleandeps` is strongly recommended. You can run both commands combined as `nmake unregister clean cleandeps` in a single elevated _x64 Native Tools Command Prompt for VS 2022_.

The govVPN client is demonstrational and is not built by default. Building can be enabled by invoking `set BUILD_GOVVPN=1` in the command prompt where `nmake` commands are invoked.


### Testing and Debugging

#### Initial Registration

The registration prepares the working environment for the eduVPN clients for testing on the build/development computer.

1. Start the _x64 Native Tools Command Prompt for VS 2022_ elevated (_Start_ » _All Programs_ » _Visual Studio 2022_ » _Visual Studio Tools_ » _VC_ » Right click _x64 Native Tools Command Prompt for VS 2022_ » _Run as Administrator_).
2. `cd` to the project folder - the one where `eduVPN.sln` and `Makefile` files are located.
3. If you haven't done so yet, build dependencies with `nmake builddeps`.
4. Start the initial build and registration using `nmake register` command. This command will:
   - Build all prerequisites.
   - Build the Debug version.
   - Install OpenVPN Interactive Service: one instance per client.
   - Install WireGuard Tunnel Manager Service: one instance per client.
   - Create a Start menu eduVPN client shortcuts.
5. The clients can now be started using the Start menu shortcut.


#### Debugging Clients

1. Perform the [initial registration](#initial-registration) described above.
2. Open the `eduVPN.sln` file (with VS2022).
3. _Build_ » _Configuration Manager..._: Select desired active solution and platform. Please note _AnyCPU_ is not supported (yet).
4. In the _Solution Explorer_ pane, locate _eduVPN.Client_, _LetsConnect.Client_ or _govVPN.Client_ project, right click on it, and _Set as StartUp Project_.
5. _Build_ » _Build Solution_.
6. _Debug_ » _Start Debugging_.


### Building MSI Packages and EXE Installer

1. Start the _x64 Native Tools Command Prompt for VS 2022_ (_Start_ » _All Programs_ » _Visual Studio 2022_ » _Visual Studio Tools_ » _VC_ » _x64 Native Tools Command Prompt for VS 2022_). x86 build environment can be used if required too. Both versions build all MSI packages and EXE installers
2. `cd` to the project folder - the one where `eduVPN.sln` and `Makefile` files are located.
3. If you haven't done so yet, build dependencies with `nmake builddeps`.
4. Start the MSI build using `nmake setup` command.
5. The MSI packages and EXE installers will be saved to the `bin\Setup` folder.
