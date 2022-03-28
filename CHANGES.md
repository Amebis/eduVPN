# Changelog

## [Unreleased](https://github.com/Amebis/eduVPN/compare/2.255.1...HEAD)

- WireGuard support
- Discover organizations as required
- OpenVPN updated to 2.5.5-20220304
    - openvpn 2.5.5 3e0c506e5d9135ef4b08547db8679cc5bd2a7582
    - openssl 3.0.1
- libsodium updated to 1.0.18-20220304
- Fixes


## [2.255.1](https://github.com/Amebis/eduVPN/compare/2.255.0...2.255.1) (2022-01-31)

- Kill running client on repair/upgrade/uninstall
- Fix cleanup when user signs out or shuts down computer
- Make OpenVPN management interface communication more secure
- Publish PDB files
- Switch to pre-release self-update channel


## [2.255.0](https://github.com/Amebis/eduVPN/compare/2.1.3...2.255.0) (2022-01-24)

- Switch to eduVPN Server APIv3
- OpenVPN updated to 2.5.5-20220124
    - openvpn 2.5.5 a184e790df801bd074619f2c90992866d40d8c3b
- Fixes and cleanups


## [2.1.3](https://github.com/Amebis/eduVPN/compare/2.1.2...2.1.3) (2022-01-12)

- Stable network adapter GUIDs for reusable NLA Public/Private profile assignments
- OpenVPN updated to 2.5.5-20220112
    - openvpn 2.5.5 813d1ee3c8b6a914599e4705eee3b8835d606e4b
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - MSVC building
    - Wintun 0.14+ support
    - net_gateway_ipv6 support
    - static build
- libsodium updated to 1.0.18-stable
- Windows 10 and 11 on ARM64 support
- Session renewal available 30 min after authentication and 24 hours before expiration
- Auto-connect on server add
- Faster startup
- Fixes and cleanups
- Binaries published on GitHub will be Minisigned with [minisign.pub](bin/Setup/minisign.pub) key
- Minisign signature verification may be configured to accept specific algorithm only


## [2.1.2](https://github.com/Amebis/eduVPN/compare/2.1.1...2.1.2) (2021-12-03)

- x64 dependency DLLs fixed


## [2.1.1](https://github.com/Amebis/eduVPN/compare/2.1...2.1.1) (2021-12-03)

- OpenVPN updated to 2.5.4-20211202
    - openvpn 2.5.4 36b3129d47a6dbfcd43ff4773c69618a28eb48bc
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - MSVC building
    - Wintun 0.14+ support
    - net_gateway_ipv6 support
- Wintun updated to 0.14.1
- Uninstall previous version before installing the new one


## [2.1](https://github.com/Amebis/eduVPN/compare/2.0.7...2.1) (2021-09-21)

- Version bumped


## [2.0.7](https://github.com/Amebis/eduVPN/compare/2.0.6...2.0.7) (2021-09-09)

- Fixes


## [2.0.6](https://github.com/Amebis/eduVPN/compare/2.0.5...2.0.6) (2021-09-03)

- OpenVPN updated to 2.5.3-20210903
    - openvpn 2.5.3 477781335cbca1aec69a372cbc18bf086155eea1
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.13+ support
- Fixes


## [2.0.5](https://github.com/Amebis/eduVPN/compare/2.0.4...2.0.5) (2021-07-30)

- Fixes


## [2.0.4](https://github.com/Amebis/eduVPN/compare/2.0.3...2.0.4) (2021-07-29)

- Fixes


## [2.0.3](https://github.com/Amebis/eduVPN/compare/2.0.2...2.0.3) (2021-07-27)

- OpenVPN updated to 2.5.3-20210727
    - openvpn 2.5.3 6204dc7cb8e1731fc0fdf6c2fcd016f9c049ac69
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support revised


## [2.0.2](https://github.com/Amebis/eduVPN/compare/2.0.1...2.0.2) (2021-07-26)

- Launching client on sign-on is an opt-in now
- Skip client re-authorization if expired on sign-on
- .NET Framework 4.8 setup issue fixed


## [2.0.1](https://github.com/Amebis/eduVPN/compare/2.0...2.0.1) (2021-07-13)

- Launch client on sign-on and install
- OpenVPN updated to 2.5.3-20210709
    - openvpn 2.5.3 12146fbee792455e551b7b422da8a8ba7c9054ff
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support
- GUI refinements


## [2.0](https://github.com/Amebis/eduVPN/compare/1.255.9...2.0) (2021-06-16)

- Minor bug fixed


## [1.255.11](https://github.com/Amebis/eduVPN/compare/1.255.9...1.255.11) (2021-06-15)

None. Testing self-update.


## [1.255.10](https://github.com/Amebis/eduVPN/compare/1.255.9...1.255.10) (2021-06-15)

- Switch back to official self-update channel
- OpenVPN updated to 2.5.2-20210615
    - openvpn 2.5.2 1601f79bc2c771976a68a708abd11fd024adc4dc
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support
- Wintun updated to 0.11


## [1.255.9](https://github.com/Amebis/eduVPN/compare/1.255.8...1.255.9) (2021-04-22)

- OpenVPN updated to 2.5.2-20210422
    - openvpn 2.5.2 acf52dda9f4cb117e9d020dd06fccd7ecb90d303
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support
- GUI refinements


## [1.255.8](https://github.com/Amebis/eduVPN/compare/1.255.7...1.255.8) (2021-04-08)

- Upgrade to .NET Framework 4.8
- OpenVPN updated to 2.5.1-20210408
    - openvpn 2.5.1 acf52dda9f4cb117e9d020dd06fccd7ecb90d303
    - dead code removed, MSVC warnings resolved
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support
- Spanish (Latin America) translations
- Translations updated
- Auto-reconnect on client restart
- Customizable hardware acceleration
- GUI refinements


## [1.255.7](https://github.com/Amebis/eduVPN/compare/1.255.6...1.255.7) (2021-03-24)

- OpenVPN updated to 2.5.1-20210322
    - openvpn 2.5.1 475d17a53eba85591f270008f8b583383a5b9afa
    - openvpnserv fix event log error reporting
    - openvpnserv fix unexpected termination
    - Wintun 0.9+ support
- Network adapter switched to Wintun
- Individual MSI packages merged into one (per platform)
- .wxl setup localization files moved to Install\<language ID>
- Phantom leftover packages in Setup -> Apps list and incomplete package uninstalls fixed


## [1.255.6](https://github.com/Amebis/eduVPN/compare/1.255.5...1.255.6) (2021-03-03)

- OpenVPN updated to 2.5.1-20210303
    - openvpn 2.5.1
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
    - openvpnserv fix event log error reporting
    - openvpnserv fix unexpected termination
- Translations updated


## [1.255.5](https://github.com/Amebis/eduVPN/compare/1.255.4...1.255.5) (2021-03-03)

- GUI refinements


## [1.255.4](https://github.com/Amebis/eduVPN/compare/1.255.3...1.255.4) (2021-03-01)

- GUI refinements
- OpenVPN updated to 2.5.1-20210225
    - openvpn 2.5.1
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
    - openvpnserv fix event log error reporting
    - openvpnserv fix unexpected termination
- Customizing with .config.local files migrated to registry


## [1.255.3](https://github.com/Amebis/eduVPN/compare/1.255.2...1.255.3) (2021-02-18)

- Support session renewal before it expires
- GUI refinements
- OpenVPN updated to 2.5.0-20210217
    - openvpn 2.5.0 1f61f3f755a84ed9765da744c7b61a35f36c4d4b
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
    - openvpnserv fix event log error reporting
    - openvpnserv fix unexpected termination
- Prism updated to 8.0.0.1909
- Turkish translation
- Fixes


## [1.255.2](https://github.com/Amebis/eduVPN/compare/1.255.1...1.255.2) (2020-12-24)

- Switch to pre-release self-update channel
- Fix incomplete product updates on x86


## [1.255.1](https://github.com/Amebis/eduVPN/compare/1.255.0...1.255.1) (2020-12-22)

- GUI refinements
- Fixes


## [1.255.0](https://github.com/Amebis/eduVPN/compare/1.0.36...1.255.0) (2020-12-19)

- New workflow
- New styling
- Cleanups


## [1.0.36](https://github.com/Amebis/eduVPN/compare/1.0.35...1.0.36) (2020-12-18)

- OpenVPN updated to 2.5.0-20201217
    - openvpn 2.5.0 2f2df474158b6c24325a47334fc8b5eb77a69b85
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
    - openvpnserv fix event log error reporting
    - openvpnserv fix unexpected termination


## [1.0.35](https://github.com/Amebis/eduVPN/compare/1.0.34...1.0.35) (2020-11-23)

- OpenVPN updated to 2.5.0-20201120
    - openvpn 2.5.0
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters


## [1.0.34](https://github.com/Amebis/eduVPN/compare/1.0.33...1.0.34) (2020-10-05)

- OpenVPN updated to 2.5-20201005
    - openvpn 2.5_rc2 7b4f53095c761bde8c6b39cf645cade4c1c0c5d4
    - openvpnmsica silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
- TAP-Windows updated to 9.24.5


## [1.0.33](https://github.com/Amebis/eduVPN/compare/1.0.32...1.0.33) (2020-09-24)

- OpenVPN updated to 2.5-20200924
    - openvpn 2.5_rc1
    - netsh calls by interface index rather than name
    - netsh DNSv6 and WINS cleanup on disconnect
    - openvpnmsica code simplifications, silence adapter creation
    - openvpnmsica ignore legacy TAP-Windows6 adapters
- Arab translations included in the setup


## [1.0.32](https://github.com/Amebis/eduVPN/compare/1.0.31...1.0.32) (2020-09-21)

- Arab translations updated
- Fixes


## [1.0.31](https://github.com/Amebis/eduVPN/compare/1.0.30...1.0.31) (2020-09-21)

- Right-to-left support
- OpenVPN updated to 2.5-20200918
    - openvpn 2.5_beta4 a5964e34057bd3a2c0cb232f1abc6feeefdf146e
    - openvpnmsica code simplifications, silence adapter creation
    - netsh calls by interface index rather than name
    - netsh DNSv6 and WINS cleanup on disconnect
    - openvpnmsica ignore legacy TAP-Windows6 adapters
- TAP-Windows updated to 9.24.4


## [1.0.30](https://github.com/Amebis/eduVPN/compare/1.0.29...1.0.30) (2020-09-11)

- OpenVPN updated to 2.5-20200904
    - openvpn 2.5_beta3 d8c037eaff4e87106e8ca798ee4abff522f0ce34
    - openvpnmsica code simplifications, silence adapter creation
    - netsh calls by interface index rather than name
    - netsh DNSv6 and WINS cleanup on disconnect
- TAP-Windows setup updated to MSI
- TLS 1.3 support
- Switch to OpenVPN/OpenSSL certificate management
- Arab translation


## [1.0.29](https://github.com/Amebis/eduVPN/compare/1.0.28...1.0.29) (2020-06-22)

- Partial Norwegian Bokmål translation
- French translation revised
- A one-month worth of past OpenVPN logs kept for troubleshooting
- Fixes


## [1.0.28](https://github.com/Amebis/eduVPN/compare/1.0.27...1.0.28) (2020-05-25)

- Auto-reconnect on client restart
- User may opt-in to allow local network access while connected
- 2FA discontinued
- JSON sequence checking discontinued
- OpenVPN updated to 2.4.9
- TAP-Windows updated to 9.24.2
- French translation
- German translation
- GUI refinements
- Fixes


## [1.0.27](https://github.com/Amebis/eduVPN/compare/1.0.26...1.0.27) (2019-08-13)

- "Add other address" resurrected in eduVPN Client
- Setup UI refinements
- Add Ukrainian and Complete Dutch translation
- Update to Visual Studio 2019


## [1.0.26](https://github.com/Amebis/eduVPN/compare/1.0.25...1.0.26) (2019-04-15)

- OpenVPN updated to 2.4.7
- Fixes


## [1.0.25](https://github.com/Amebis/eduVPN/compare/1.0.24...1.0.25) (2018-11-19)

- Complete Dutch translation
- Self-updating revised to use original binary installer filename
- Copyright notice update


## [1.0.24](https://github.com/Amebis/eduVPN/compare/1.0.23...1.0.24) (2018-11-08)

- Improved profile management in Let's Connect! client
- GUI refinements
- Fixes


## [1.0.23](https://github.com/Amebis/eduVPN/compare/1.0.22...1.0.23) (2018-07-20)

- Issue with non-Admin on non-English Windows fixed
- Additional settings to allow OpenVPN profile configuration run-time changes by client
- MSI updating switched from "Uninstall&Install" to "Update&Cleanup" strategy


## [1.0.22](https://github.com/Amebis/eduVPN/compare/1.0.21...1.0.22) (2018-05-03)

- OpenVPN updated to 2.4.6
- Delay when reconnecting to a rebuilt VPN server fixed
- "Add other address" depretiated from eduVPN Client in favour of Let's Connect Client
- GUI refinements
- Fixes and cleanups


## [1.0.21](https://github.com/Amebis/eduVPN/compare/1.0.20...1.0.21) (2018-03-06)

- OpenVPN updated to 2.4.5
- Fixes and cleanups


## [1.0.20](https://github.com/Amebis/eduVPN/compare/1.0.19...1.0.20) (2018-02-05)

- GUI refinements
- Let's Connect! client will self-update from this version on


## [1.0.19](https://github.com/Amebis/eduVPN/compare/1.0.18...1.0.19) (2018-02-02)

- Let's Connect! client
- GUI redesign
- Accessibility and screen reader support
- Fixes and cleanups


## [1.0.18](https://github.com/Amebis/eduVPN/compare/1.0.17...1.0.18) (2018-01-18)

- Fixes


## [1.0.17](https://github.com/Amebis/eduVPN/compare/1.0.16...1.0.17) (2018-01-18)

- Updates to comply with security audit report on all accounts possible
- Prism library updated to 7.0
- Full debug info is now included in all builds
- Fixes, performance improvements and cleanups


## [1.0.16](https://github.com/Amebis/eduVPN/compare/audit/2017-12...1.0.16) (2018-01-10)

- TOTP enrollment UI enhanced
- Self-updating enhancements
- OpenVPN performance improvements on Windows 7
- libsodium updated to 1.0.16
- Fixes and cleanups


## [audit/2017-12](https://github.com/Amebis/eduVPN/compare/1.0.15...audit/2017-12) (2017-12-17)

This version has been submitted for code review.


## [1.0.15](https://github.com/Amebis/eduVPN/compare/1.0.14...1.0.15) (2017-12-15)

- TOTP secret increased to 160bit
- Basic Dutch translation
- GUI refinements
- Bug fixes and clean-ups

## [1.0.14](https://github.com/Amebis/eduVPN/compare/1.0.13...1.0.14) (2017-12-13)

- Client-based 2FA enrollment
- New OAuth client identifier
- Bug fixes and clean-ups


## [1.0.13](https://github.com/Amebis/eduVPN/compare/1.0.12...1.0.13) (2017-12-08)

- OAuth redesigned to workaround browser confirmation to launch external application and to provide a "finished" page to the browser after OAuth is finished.
- eduVPN client window is now brought in front after OAuth is complete correctly
- Support for 2FA enrollment added (web-based).
- Default client mode changed to 3.
- GUI updated to 3.2 guidelines.
- Forgetting provider now removes orphaned authentication token and certificate
- Certificate management improvements
- TAP driver install prompt has been silenced
- Client settings are now saved on user logout/computer shutdown
- Various issues fixed, internal clean-ups and reorganizations


## [1.0.12](https://github.com/Amebis/eduVPN/compare/1.0.11...1.0.12) (2017-11-28)

- User-Agent added to HTTP requests
- Various issues fixed


## [1.0.11](https://github.com/Amebis/eduVPN/compare/1.0-alpha8...1.0.11) (2017-11-27)

- Self-updating support
- `.config` files now annotated and `eduVPN.Client.exe.config` extended to include `eduVPN.dll.config` entries
- Separate upgrade GUIDs for 32/64-bit MSI packages
- Allow blank public keys to disable signature checking
- Switch to all-numeric version designation
- EXE bundle and Core MSI version split


## [1.0-alpha8](https://github.com/Amebis/eduVPN/compare/1.0-alpha6...1.0-alpha8) (2017-11-17)

- Internal client reorganization to support customizable work-flows now
- OpenVPN 2.4.4 update
- OpenVPN components moved to a separate MSI; Client and OpenVPN components install now into separate folders
- Previous installation folder detected and reused; installation folder is customizable via command line now
- Bundle uninstall fixed
- Settings and About pages backward navigation revised
- Errors reported by OpenVPN Interactive Service are now annotated appropriately


## [1.0-alpha6](https://github.com/Amebis/eduVPN/compare/1.0-alpha5...1.0-alpha6) (2017-10-27)

- OpenVPN 2.4.4 is now bundled inside MSI packages. TAP-Windows driver remains a pre-requisite.
- EXE installer to install .NET Framework 4.5 and TAP-Windows driver on demand, and eduVPN client MSI of course.
- Localizable Start Menu shortcut
- Non-localized installation folder
- Access token handling redesigned to avoid racing condition, and to support access token reuse for clones of the same instance
- After the last profile is removed from history the connection wizard is redirected back to initial screen
- AppVeyor support
- Minor fixes and clean-ups


## [1.0-alpha5](https://github.com/Amebis/eduVPN/compare/1.0-alpha4a...1.0-alpha5) (2017-10-04)

- 2-Factor authentication support
- Recent configurations can now be cleared using context menu
- Client configuration display name extended to include computer name (See _VPN User Portal_ � _Configurations_)
- libsodium updated to 1.0.14
- TLS renegotiation forced every 5 minutes for Debug versions of the client
- Client reapplies for certificate when `tls-error` related reconnect occurs
- Visual enhancements
- Bug fixes
- Internal clean-up and reorganizations


## [1.0-alpha4a](https://github.com/Amebis/eduVPN/compare/1.0-alpha3...1.0-alpha4a) (2017-09-15)

- Settings page introduced
- Support for _Force VPN_ setting added
- Visual enhancements
- Bug fixes
- Internal clean-up and reorganizations


## [1.0-alpha3](https://github.com/Amebis/eduVPN/compare/1.0-alpha2a...1.0-alpha3) (2017-09-13)

- MSI setup packages introduced
- About page introduced
- Custom instances entered by hostname instead of base URI
- Closed client window can be reopened via system tray menu
- Instance source selection page allows navigating back (when not the starting page)
- Error messages (stack trace actually) can be copied to the clipboard
- Visual enhancements
- Bug fixes
- Internal clean-up and reorganizations


## [1.0-alpha2a](https://github.com/Amebis/eduVPN/compare/1.0-alpha1...1.0-alpha2a) (2017-09-06)

- List of recent configurations introduced
- VPN connection stays connected when navigating the Wizard
- Generic fallback instance icon introduced
- Bug fixes
- Internal clean-up and reorganizations


## [1.0-alpha1](https://github.com/Amebis/eduVPN/compare/1.0-alpha...1.0-alpha1) (2017-08-28)

- New styling applied, including logic changes implied; VPN status icon updated to official version; Window icon and taskbar icon overlay introduced
- Minimize to system tray implemented
- Authorization request no longer processed as a wizard page but as an on-demand pop-up
- eduVPN client certificates are now stored in a separate certificate store
- Instance sources discovery updated
- Sequence is now mandatory; Sequence checking (re)introduced; Cache no longer reset on loading issues, to prevent roll-back attack
- Custom source reverted to custom instance
- ETag/If-None-Match support added
- Settings migrate across versions now
- Bug fixes
- Internal clean-up and reorganizations
- Meta-data update


## 1.0-alpha (2017-08-21)

Initial release
