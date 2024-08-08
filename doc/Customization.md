# Customization of eduVPN Clients for Windows


This document describes customization of the eduVPN, Let's Connect! and govVPN Clients for Windows only. For eduVPN server setup see [eduVPN Documentation](https://github.com/eduvpn/documentation).

eduVPN, Let's Connect! and govVPN clients are the same client targeted for a different audience. While their UI is different, the customization is mostly identical. Therefore, the remainder of this document will reference "eduVPN" only for readability. For Let's Connect! and govVPN use case, the "eduVPN" in paths and filenames translates to "LetsConnect" and "govVPN" respectively.


## Default values

Some properties of the client are customizable. The default values of customizable properties are stored in the .config files:

- [eduVPN.Client.exe.config](../eduVPN.Client/app.config)
- [LetsConnect.Client.exe.config](../LetsConnect.Client/app.config)
- [govVPN.Client.exe.config](../govVPN.Client/app.config)

As the .config files evolve over time and are overwritten on client updates, they should not be used for client customization.


## Registry overrides

Computer-wide client settings may be overriden using the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN` registry key.

To override specific setting, add a value described in this chapter to the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN` registry key and set its content as required.


### `InstituteAccessServers` (`REG_MULTI_SZ`)

`InstituteAccessServers` allows preconfiguring the client with Institute Access and/or Other server(s). May contain any number of eduVPN server base URLs (e.g. `https://demo.eduvpn.nl/`). The servers on this list cannot be removed by user. However, user may add additional servers.

Servers listed in the eduVPN discovery infrastructure will be shown under Institute Access section, others under Other Servers in the client UI.

This registry value can also be used to preconfigure servers in Let's Connect! and govVPN clients which have no discovery. The name of the registry value remains for backward compatibility.


### `SecureInternetOrganization` (`REG_SZ`)

`SecureInternetOrganization` allows preconfiguring the client with specific Secure Internet organization. It has three possible states:

State            | Meaning
-----------------|--------
absent           | No Secure Internet organization is preconfigured. User must search and add organization on his own. (default)
blank            | Secure Internet is completely disabled.
organization ID  | Secure Internet organization is preset. User cannot modify the Secure Internet organization on his own. But, is allowed to change Secure Internet server to connect to.


### `SelfUpdateDiscovery` (`REG_SZ`)

`SelfUpdateDiscovery` specifies self-update discovery URI. The client checks this URI for the latest release. The self-updating is turned off by setting this value to a blank string.

Example: [How to switch client to pre-release update channel?](FAQ.md#how-to-switch-client-to-pre-release-update-channel)


### `SelfUpdateDiscoveryPublicKeys` (`REG_MULTI_SZ`)

`SelfUpdateDiscoveryPublicKeys` is a list of acceptable Minisign public keys. The `SelfUpdateDiscovery` URI content is expected to be signed with one of those. Add one `<Base64 encoded public key>[|<algorithm mask>]` per string/line. Should the sign check not be required (strongly discouraged), set `SelfUpdateDiscoveryPublicKeys` to an empty list.

Ignored when self-updating is off.

Example: [How to switch client to pre-release update channel?](FAQ.md#how-to-switch-client-to-pre-release-update-channel)


### `OpenVPNInteractiveServiceInstance` (`REG_SZ`)

`OpenVPNInteractiveServiceInstance` specifies the OpenVPN instance ID when more than one instance of the OpenVPN is installed. Defaults to "$eduVPN", "$LetsConnect" or "$govVPN". Set to blank string to use default OpenVPN installed instance.


### `OpenVPNRemoveOptions` (`REG_MULTI_SZ`)

Options to be removed from provisioned OpenVPN profile configuration at run-time. List OpenVPN option names only - i.e. without parameters, one per string/line. The client will parse the OpenVPN profile configuration at run-time and remove all instances of options listed here.

Use in combination with `OpenVPNAddOptions` setting to override the OpenVPN options provisioned by the VPN provider.


### `OpenVPNAddOptions` (`REG_MULTI_SZ`)

Custom options to be added to provisioned OpenVPN profile configuration at run-time. Those options are appended to the OpenVPN profile configuration file (OVPN) at run-time and should conform to the OVPN file syntax. It can contain multiple strings/lines to specify multiple OpenVPN options.

Use in combination with `OpenVPNRemoveOptions` setting to override the OpenVPN options provisioned by the VPN provider.


### `WireGuardKillSwitch` (`REG_DWORD`)

When used as the default gateway, WireGuard has optional firewall rules to block non-tunnel traffic (aka "kill-switch"). This registry setting allows user control, enforcing or removing the kill-switch at run-time. When registry value is absent, user may control the setting.

Value  | Behavior
-------|----------
0      | Allow user to control the setting (Default)
1      | Enforce kill-switch on any default gateway profile
2      | Remove kill-switch from any default gateway profile

Note: Use with extreme caution as this might disable remote access, LAN resources, and other VPNs on user computer when enforced. Or, allow data leakage, unintended gateway to organization network, or other VPNs when WireGuard kill-switch is removed. This option allows administrators to balance between convenience and security.

### Minisign algorithm masks

Value | Algorithm
------|----------
1     | legacy
2     | prehashed
3     | any


## User settings registry overrides

User client settings may be overriden using the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN\Views` registry key.

To override specific setting, add a value described in this chapter to the `HKEY_LOCAL_MACHINE\SOFTWARE\SURF\eduVPN\Views` registry key and set its content as required.


### `StartOnSignon` (`REG_DWORD`)

`StartOnSignon` enforces or disables client startup on each user sign-on. Set to non-zero to enforce startup. Set to zero to disable startup. When this registry value is not present, users are allowed to configure startup in the client UI.
