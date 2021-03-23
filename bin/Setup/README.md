The `nmake Setup` prepares distributable binaries in this folder.


# EXE Installers

*Naming convention: `<client brand>Client_<version>.exe`*

The EXE installers are provided for end-users. They install the VPN client for Windows and all required pre-requisites.


# MSI Packages

VPN client MSI packages are provided for IT professionals looking to mass-deploy VPN client in their organisation using Group Policy or similar.

Note: All VPN client MSI packages have no GUI by design.


## Platforms

Microsoft Installer packages are single-platform. Therefore, separate MSI packages are provided for 32 and 64-bit platforms:

- _x64_ = 64-bit Windows
- _x86_ = 32-bit Windows

Although, 32-bit VPN client should run on 64-bit Windows fine, native 64-bit version should be used when possible.


## Localization

MSI Installer selects the locale according to the current user's Regional Settings set in Control Panel. Setup locale can be forced using the following command line:

```
msiexec /i <client brand>Core_<version>_<platform>.msi TRANSFORMS=:1060
```

The `1060` represents Microsoft's LCID for Slovenian (Slovenia) locale. The list of LCIDs can be found [here](https://msdn.microsoft.com/en-us/library/cc767443.aspx).

The language used at run-time is selected automatically by Windows. Whenever possible, Windows tries to match the language of application with the language of operating system itself providing seamless experience of OS and applications. When matching localisation is not available, U.S. English is used as default.


## VPN Client OpenVPN Components

*Naming convention: `<client brand>OpenVPN_<version>_<platform>.msi`*


## VPN Client Core

*Naming convention: `<client brand>Core_<version>_<platform>.msi`*

Pre-requisites:
- .NET Framework 4.5
- VPN Client OpenVPN Components
