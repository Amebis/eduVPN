# Development of eduVPN Client for Windows


## Folders
- _bin_: Automation scripts
   - _Debug_: Debug binaries
   - _Release_: Release binaries
   - _OpenVPN_: OpenVPN upstream binaries
   - _Setup_: MSI packages and EXE installer
- _eduEd25519_: libsodium wrapper providing Ed25519 signing and verifying support to C#/.NET
- _eduJSON_: Lightweight JSON parser
- _eduOAuth_: OAuth 2.0 library
- _eduOpenVPN_: OpenVPN IPC for C#/.NET
- _eduVPN_: eduVPN client business logic (Model and View Model)
- _eduVPN.Client_: eduVPN client UI (View)
- _eduVPN.Install_: Theme files for WiX Standard Bootstrapper Application
- _eduVPN.Resources_: Native resources for Windows MUI - localizable names and descriptions for Start Menu shortcuts etc.
- _OpenVPN.Resources_: Native resources for Windows MUI - localizable names and descriptions for system services names etc.

## Building

### Pre-requisites
- Visual Studio 2017 Community Edition: The required VS2017 feature set to be announced later...

### Code Signing
In order to have the build process digitally sign the release output files, one should provide the following:

1. A signing certificate installed in the building user’s certificate store.
2. The following variables in the environment:
  - `ManifestCertificateThumbprint` - set the value to certificate’s SHA1 thumbprint (hexadecimal, without spaces, i.e. `bc0d8da45f9eeefcbe4e334e1fc262804df88d7e`).
  - `ManifestTimestampRFC3161Url` - set the value to URL used to perform timestamp signature (i.e. `http://sha256timestamp.ws.symantec.com/sha256/timestamp`). In order to perform the timestamp signing successfully, the computer running the build should be online and able to access this URL.

### Initial Registration
The registration prepares the working environment for the eduVPN client, much like the setup does. It performs the following:
- Prepares local OpenVPN binaries.
- Builds, installs and starts OpenVPN Interactive Service.
- Builds the Debug version of the eduVPN Client.
- Registers URI handler for OAuth redirection.
- Creates Start Menu shortcut.

1. Start the _x64 Native Tools Command Prompt for VS 2017_ elevated (_Start_ » _All Programs_ » _Visual Studio 2017_ » _Visual Studio Tools_ » _VC_ » Right click _x64 Native Tools Command Prompt for VS 2017_ » _Run as Administrator_). x86 build environment can be used if required too.
2. `cd` to the project folder - the one where `eduVPN.sln` and `Makefile` files are located.
3. Start the initial build and registration using `nmake Register` command. This command will:
   - Build the Debug version for your architecture as defined by the `PROCESSOR_ARCHITECTURE` environment variable.
   - Create a Start menu eduVPN Client shortcut.
   - Install eduVPN Interactive Service.
4. The client can now be started using the Start menu shortcut.

### Debugging eduVPN Client
1. Perform the [initial registration](#initial-registration) described above.
2. Open the `eduVPN.sln` file (with VS2017).
3. _Build_ » _Configuration Manager..._: Select desired active solution and platform. Please note _AnyCPU_ is not supported (yet).
4. In the _Solution Explorer_ pane, locate _eduVPN.Client_ project, right click on it, and _Set as StartUp Project_.
5. _Build_ » _Build Solution_.
6. _Debug_ » _Start Debugging_.

### Building MSI Packages and EXE Installer

#### Pre-requisites
- [WiX Toolset 3.11](http://wixtoolset.org/releases/v3.11/stable)

#### Building
1. Start the _x64 Native Tools Command Prompt for VS 2017_ (_Start_ » _All Programs_ » _Visual Studio 2017_ » _Visual Studio Tools_ » _VC_ » _x64 Native Tools Command Prompt for VS 2017_). x86 build environment can be used if required too.
2. `cd` to the project folder - the one where `eduVPN.sln` and `Makefile` files are located.
3. Start the MSI build using `nmake Setup` command.
4. The MSI packages and EXE installer will be saved to the `bin\Setup` folder.
