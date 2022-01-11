# Customization of eduVPN and Let's Connect! Clients for Windows


This document describes customization of the eduVPN and Let's Connect! Clients for Windows only. For eduVPN server setup see [eduVPN Documentation](https://github.com/eduvpn/documentation).

eduVPN and Let's Connect! clients are the same client targeted for a different audience. While their UI is different, the customization is identical. Therefore, the remainder of this document will reference "eduVPN" only for readability. For Let's Connect! use case, the "eduVPN" in paths and filenames translates to "LetsConnect".


## Default values

Some properties of the client are customizable. The default values of customizable properties are stored in the .config files:

- [eduVPN.Client.exe.config](../eduVPN.Client/app.config)
- [LetsConnect.Client.exe.config](../LetsConnect.Client/app.config)

As the .config files evolve over time and are overwritten on client updates, they should not be used for client customization.


## Registry overrides

Computer-wide client settings may be overriden using the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN` registry key.

To override specific setting, add a value described in this chapter to the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN` registry key and set its content as required.


### `ServersDiscovery` (`REG_SZ`)

`ServersDiscovery` specifies server list discovery URI. The client downloads a [list of available servers](https://github.com/eduvpn/documentation/blob/v2/SERVER_DISCOVERY.md) from this URI. The server discovery is turned off by setting this value to a blank string (e.g. in Let's Connect! client).


### `ServersDiscoveryPublicKeys` (`REG_MULTI_SZ`)

`ServersDiscoveryPublicKeys` is a list of acceptable Minisign public keys. The `ServersDiscovery` URI content is expected to be signed with one of those. Add one `<Base64 encoded public key>[|<algorithm mask>]` per string/line. Should the sign check not be required (strongly discouraged), set `ServersDiscoveryPublicKeys` to an empty list.

Ignored when server discovery is off.


### `OrganizationsDiscovery` (`REG_SZ`)

`OrganizationsDiscovery` specifies organization list discovery URI. The client downloads a [list of available organizations](https://github.com/eduvpn/documentation/blob/v2/SERVER_DISCOVERY.md) from this URI. The organization discovery is turned off by setting this value to a blank string (e.g. in Let's Connect! client).


### `OrganizationsDiscoveryPublicKeys` (`REG_MULTI_SZ`)

`OrganizationsDiscoveryPublicKeys` is a list of acceptable Minisign public keys. The `OrganizationsDiscovery` URI content is expected to be signed with one of those. Add one `<Base64 encoded public key>[|<algorithm mask>]` per string/line. Should the sign check not be required (strongly discouraged), set `OrganizationsDiscoveryPublicKeys` to an empty list.

Ignored when server discovery is off.


### `SelfUpdateDiscovery` (`REG_SZ`)

`SelfUpdateDiscovery` specifies self-update discovery URI. The client checks this URI for the latest release. The self-updating is turned off by setting this value to a blank string.


### `SelfUpdateDiscoveryPublicKeys` (`REG_MULTI_SZ`)

`SelfUpdateDiscoveryPublicKeys` is a list of acceptable Minisign public keys. The `SelfUpdateDiscovery` URI content is expected to be signed with one of those. Add one `<Base64 encoded public key>[|<algorithm mask>]` per string/line. Should the sign check not be required (strongly discouraged), set `SelfUpdateDiscoveryPublicKeys` to an empty list.

Ignored when self-updating is off.


### `OpenVPNInteractiveServiceInstance` (`REG_SZ`)

`OpenVPNInteractiveServiceInstance` specifies the OpenVPN instance ID when more than one instance of the OpenVPN is installed. Defaults to "$eduVPN" or "$LetsConnect". Set to blank string to use default OpenVPN installed instance.


### `OpenVPNRemoveOptions` (`REG_MULTI_SZ`)

Options to be removed from provisioned OpenVPN profile configuration at run-time. List OpenVPN option names only - i.e. without parameters, one per string/line. The client will parse the OpenVPN profile configuration at run-time and remove all instances of options listed here.

Use in combination with `OpenVPNAddOptions` setting to override the OpenVPN options provisioned by the VPN provider.


### `OpenVPNAddOptions` (`REG_MULTI_SZ`)

Custom options to be added to provisioned OpenVPN profile configuration at run-time. Those options are appended to the OpenVPN profile configuration file (OVPN) at run-time and should conform to the OVPN file syntax. It can contain multiple strings/lines to specify multiple OpenVPN options.

Use in combination with `OpenVPNRemoveOptions` setting to override the OpenVPN options provisioned by the VPN provider.


### Minisign algorithm masks

Value | Algorithm
------|----------
1     | legacy
2     | prehashed
3     | any
