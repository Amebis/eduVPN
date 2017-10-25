The `nmake Setup` prepares distributable binaries in this folder.

# EXE Installers

*Naming convention: `eduVPN-Client-Win_<version>_<locale>.exe`*

The EXE installers are provided for end-users. They install the eduVPN client for Windows and all required pre-requisites.


# MSI Packages

eduVPN MSI packages install eduVPN client. eduVPN MSI packages are provided for IT professionals looking to mass-deploy eduVPN in their organisation using Group Policy or similar. Deploying eduVPN client is deploying:

1. .NET Framework 4.5
2. Tap-windows drivers 9.21+
3. eduVPN MSI

Note: All eduVPN MSI packages have no GUI by design.

eduVPN client MSI is published in various flavours. This document describes them.


## Platforms

Microsoft Installer packages are single-platform. Therefore, eduVPN provides separate MSI packages for 32 and 64-bit platforms:

- _x64_ = 64-bit Windows
- _x86_ = 32-bit Windows

Although, 32-bit eduVPN client should run on 64-bit Windows fine, native 64-bit version should be used when possible.


## Localisations

eduVPN client is published in English. At the time of writing this document (1.0-alpha6), it also includes resources in Slovenian language. Other language resources should follow once the GUI is settled.

Resources of all available eduVPN client localisations are always installed regardless of MSI flavour being installed.

The language used at run-time is selected by Windows. Whenever possible, Windows tries to match the language of application with the language of operating system itself providing seamless experience of OS and applications. When matching localisation is not available, U.S. English is used as default.

However, there are multiple flavours of MSI packages for eduVPN client available based on localisation.


### Localised

*Naming convention: `eduVPN-Client-Win_<version>_<platform>_<locale>.msi`*

The MSI packages of this type have properties, annotations and other internals localised. This means that "Add/Remove Programs" or "Programs and Features" list of installed programs will show eduVPN client in the `<locale>` language.

However, they will install all resources, so the Start Menu shortcut and eduVPN client itself language will nevertheless be matched to Windows language when possible.

This flavour of MSI packages might get obsoleted in the following versions.


### Multilingual

*Naming convention: `eduVPN-Client-Win_<version>_<platform>.msi`*

Multilingual MSI packages contain en-US MSI localised package serving as the base with language dependent MSI transforms. The language is dynamically selected at setup by MSI Installer.

Unfortunately, MSI Installer's locale selection algorithm deviates from standard Windows practice. Instead of trying to match application locale to OS, MSI Installer selects the locale according to the current user's Regional Settings set in Control Panel.

Example: Installing multilingual MSI package on U.S. English Windows with user's Regional Settings set to Slovenian will do the setup in Slovenian. However, the eduVPN client will use U.S. English locale at run-time.

Setup language can be forced using the following command line:

```
msiexec /i eduVPN-Client-Win_<version>_<platform>.msi TRANSFORMS=:1060
```

The `1060` represents Microsoft's LCID for Slovenian (Slovenia) locale. The list of LCIDs can be found [here](https://msdn.microsoft.com/en-us/library/cc767443.aspx).
