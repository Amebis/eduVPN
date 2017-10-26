The `nmake Setup` prepares distributable binaries in this folder.

# EXE Installers

*Naming convention: `eduVPN-Client-Win_<version>_<locale>.exe`*

The EXE installers are provided for end-users. They install the eduVPN client for Windows and all required pre-requisites.


# MSI Packages

*Naming convention: `eduVPN-Client-Win_<version>_<platform>.msi`*

eduVPN MSI packages install eduVPN client only. eduVPN MSI packages are provided for IT professionals looking to mass-deploy eduVPN in their organisation using Group Policy or similar.

Note: All eduVPN MSI packages have no GUI by design.


## Pre-requisites

The eduVPN client requires the following pre-requisites (not installed by MSI):

1. .NET Framework 4.5
2. Tap-windows drivers 9.21+


## Platforms

Microsoft Installer packages are single-platform. Therefore, eduVPN provides separate MSI packages for 32 and 64-bit platforms:

- _x64_ = 64-bit Windows
- _x86_ = 32-bit Windows

Although, 32-bit eduVPN client should run on 64-bit Windows fine, native 64-bit version should be used when possible.


## Localisations

MSI Installer selects the locale according to the current user's Regional Settings set in Control Panel. Setup locale can be forced using the following command line:

```
msiexec /i eduVPN-Client-Win_<version>_<platform>.msi TRANSFORMS=:1060
```

The `1060` represents Microsoft's LCID for Slovenian (Slovenia) locale. The list of LCIDs can be found [here](https://msdn.microsoft.com/en-us/library/cc767443.aspx).

The language used at run-time is selected automatically by Windows. Whenever possible, Windows tries to match the language of application with the language of operating system itself providing seamless experience of OS and applications. When matching localisation is not available, U.S. English is used as default.
