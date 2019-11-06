# Changelog

## [Unreleased](https://github.com/Amebis/eduVPN/compare/1.0.27...HEAD)

- User may opt-in to allow local network access while connected
- OpenVPN updated to 2.4.8
- TAP-Windows updated to 9.24.2
- French translation
- GUI refinements


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
- New OAuth client ID
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
