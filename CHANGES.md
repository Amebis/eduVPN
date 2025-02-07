﻿# Changelog

## [Unreleased](https://github.com/Amebis/eduVPN/compare/4.2.1...HEAD)

- OpenVPN updated to 2.5.11-20241129
    - openvpn 2.5.11 98e70e7351af37506df47d879342ee48c2f601c4
- eduvpn-common updated to 2.1.0 ef06ec24d3ee3af5d4c210db49744e635f34d1db
- Support for v3.x OAuth token migration removed


## [4.2.1](https://github.com/Amebis/eduVPN/compare/4.2...4.2.1) (2024-10-25)

- WireGuard updated to e70799b1440690e7d4140bffc7c73baf903c7b54
- eduvpn-common updated to 2.1.0 bca773c49f0c2e66b5c26a59b8bb772520afb9bd
- Use proxy environment settings in Self-update requests
- Support for Windows 7 discontinued


## [4.2](https://github.com/Amebis/eduVPN/compare/4.1.7...4.2) (2024-09-25)

- Translations updated
- User-Agent Windows version in Self-update requests simplified


## [4.1.7](https://github.com/Amebis/eduVPN/compare/4.1.6...4.1.7) (2024-09-10)

- False-positive foreign user session detection fixed
- Auto-reconnect for preconfigured Institute Access/Own servers fixed
- Translations updated


## [4.1.6](https://github.com/Amebis/eduVPN/compare/4.1.5...4.1.6) (2024-09-09)

- Additional session setup logging


## [4.1.5](https://github.com/Amebis/eduVPN/compare/4.1.4...4.1.5) (2024-09-03)

- Allow user to skip WireGuard tunnel connectivity test
- Fixes: #249


## [4.1.4](https://github.com/Amebis/eduVPN/compare/4.1.3...4.1.4) (2024-08-20)

- OpenVPN updated to 2.5.11-20240814
    - openvpn 2.5.11 6fb2a2560845e15da8918c114fe020b4a06b3cef
- eduvpn-common updated to 2.1.0 572ce6109006b1285baaa06f4bf0e7deb5ad7528
- Let's Connect! and govVPN client startup fixed


## [4.1.3](https://github.com/Amebis/eduVPN/compare/4.1.2...4.1.3) (2024-08-09)

- eduvpn-common updated to 2.1.0 f3c04d56ca67c2ff0b08bf75cc51ea8be14c84b9
- Self-update upgraded to deliver User-Agent with client and Windows versions
- Slightly delay WireGuard tunnel connectivity test and repeat until unsuccessful
- GUI refinements
- Fixes: #247


## [4.1.2](https://github.com/Amebis/eduVPN/compare/4.1.1...4.1.2) (2024-07-17)

- eduvpn-common updated to discovery-improvements ab95a24dcf2404899543638149b0a44a5d2720a3


## [4.1.1](https://github.com/Amebis/eduVPN/compare/4.1...4.1.1) (2024-07-01)

- eduvpn-common updated to 3fb8bc94fdbc362f49bf211a3090358c70c26e8b
- GUI refinements


## [4.1](https://github.com/Amebis/eduVPN/compare/4.0...4.1) (2024-06-26)

- eduvpn-common updated to 2.0.2 4962d0e2d37e93af20c8afdae6f59d4656f272f3
- GUI refinements
- Fixes: #246


## [4.0](https://github.com/Amebis/eduVPN/compare/3.255.23...4.0) (2024-06-06)

- eduvpn-common updated to 2.0.1 e12a9820895b48de6d1b8408364fec45958bc6c5
- Version bumped


## [3.255.23](https://github.com/Amebis/eduVPN/compare/3.255.22...3.255.23) (2024-06-06)

- GUI refinements
- Switch to release self-update channel


## [3.255.22](https://github.com/Amebis/eduVPN/compare/3.255.21...3.255.22) (2024-06-05)

- eduvpn-common updated to 2.0 3d47c63005ad962951080370a647914ab7c34e94
- Graceful disconnect on user sign-out or system restart/shutdown
- Allow user to control WireGuard kill-switch unless enforced by device admin


## [3.255.21](https://github.com/Amebis/eduVPN/compare/3.255.20...3.255.21) (2024-05-30)

- GUI refinements


## [3.255.20](https://github.com/Amebis/eduVPN/compare/3.255.19...3.255.20) (2024-05-29)

- Switch to eduvpn-common provided server and organization searching
- Authorization time migration from 3.x client settings fixed
- eduvpn-common updated to 0cac5b215621ec835ac182682caa56e18e5a12c7 with 7b6c4cce34fa34bba262f794c6702011f4edc0e3 merged
- Translations updated
- Fixes: #241, #244


## [3.255.19](https://github.com/Amebis/eduVPN/compare/3.255.18...3.255.19) (2024-05-03)

- JSON parser replaced
- Search for Institute Access servers and Secure Internet organizations:
    - Keyword matching from OR to AND
    - Limit number of results to first 20 by alphabet
- eduvpn-common updated to 1.99.2 32b81ae9ec90d8beccc101cf456841615216b793


## [3.255.18](https://github.com/Amebis/eduVPN/compare/3.255.17...3.255.18) (2024-04-12)

- Sys-tray menu upgraded to show and manipulate connections
- Self-update simplified to avoid an A/V false positive chance
- govVPN switched to release self-update channel
- eduvpn-common updated to b0cfacfd6ebc5f6d2bafd974047b9b21d45d1108
- Fixes: #111, #240


## [3.255.17](https://github.com/Amebis/eduVPN/compare/3.255.16...3.255.17) (2024-04-05)

- Proxyguard updated to 1.0.1


## [3.255.16](https://github.com/Amebis/eduVPN/compare/3.255.15...3.255.16) (2024-04-03)

- Registry value to allow overriding WireGuard kill-switch
- GUI refinements
- Fixes


## [3.255.15](https://github.com/Amebis/eduVPN/compare/3.255.14...3.255.15) (2024-03-29)

- OpenVPN updated to 2.5.10-20240328
    - openvpn 2.5.10 fccae1fa71140bd66f4a57597ca3c7307ba05b30
- Fixes


## [3.255.14](https://github.com/Amebis/eduVPN/compare/3.255.13...3.255.14) (2024-03-27)

- Failover from UDP to TCP upgraded to detect no traffic condition better
- Fixes


## [3.255.13](https://github.com/Amebis/eduVPN/compare/3.255.12...3.255.13) (2024-03-22)

- Failover from UDP to TCP after failure to confirm initial VPN connectivity
- eduvpn-common updated to 3e5437ba36e8e5ffad926806960e4f76d5799e37 (1.99.1)
- OpenVPN updated to 2.5.9-20240321
    - openvpn 2.5.9 d29496cce2d91a74706e3d5e4c48773715b10812
- Fixes: #232, #233, #234, #235


## [3.255.12](https://github.com/Amebis/eduVPN/compare/3.255.11...3.255.12) (2024-03-08)

- Fixes


## [3.255.11](https://github.com/Amebis/eduVPN/compare/3.255.10...3.255.11) (2024-03-08)

- eduvpn-common updated to 9bc421138a2a4ecf5ee7dc46d9a4faa1af12c80e
- Redirect self-update check failures to About page


## [3.255.10](https://github.com/Amebis/eduVPN/compare/3.255.9...3.255.10) (2024-03-08)

- Support for WireGuard-over-TCP connections using Proxyguard
- eduvpn-common updated to 987f2e3041e0b5d6e06cdc29657be99de9c1cd60
- Preconfigurable start on sign-on


## [3.255.9](https://github.com/Amebis/eduVPN/compare/3.255.8...3.255.9) (2023-11-07)

- eduvpn-common updated to cad29dcc046163a944167bbaf2292b3e591e01c6
- Translations updated
- Italian translation


## [3.255.8](https://github.com/Amebis/eduVPN/compare/3.255.7...3.255.8) (2023-10-17)

- eduvpn-common updated to 3718828fb0b75e95a250ea8d1df23ce25b3e9442
- OpenVPN updated to 2.5.9-20230922
    - openvpn 2.5.9 3d9b4ce394f9d1a66842a5391aa744f7310a48a6
- Arabic translations updated
- All-native client on Windows 11 ARM64, Windows 10 ARM64 support discontinued


## [3.255.7](https://github.com/Amebis/eduVPN/compare/3.255.6...3.255.7) (2023-09-07)

- eduvpn-common updated to 676c989b585dd5590f3a3f0c8051ed8af4c12e5c
- Translations updated
- GUI refinements


## [3.255.6](https://github.com/Amebis/eduVPN/compare/3.255.5...3.255.6) (2023-09-06)

- Fixes


## [3.255.5](https://github.com/Amebis/eduVPN/compare/3.255.4...3.255.5) (2023-09-06)

- Testing and fixes
- govVPN introduced


## [3.255.4](https://github.com/Amebis/eduVPN/compare/3.255.3...3.255.4) (2023-07-11)

- Fixes


## [3.255.3](https://github.com/Amebis/eduVPN/compare/3.255.2...3.255.3) (2023-07-05)

- Fixes


## [3.255.2](https://github.com/Amebis/eduVPN/compare/3.255.1...3.255.2) (2023-06-05)

- Prevent VPN connection when other users are signed in
- eduLibsodium and eduOAuth removed
- OpenVPN updated to 2.5.9-20230605
    - openvpn 2.5.9 4a89a55b8a9d6193957711bef74228796a185179
- Fixes: #132, #222


## [3.255.1](https://github.com/Amebis/eduVPN/compare/3.255...3.255.1) (2023-05-17)

- GUI refinements


## [3.255](https://github.com/Amebis/eduVPN/compare/3.4...3.255) (2023-05-15)

- eduvpn-common integration
- Self-update migrated to Go
- Warn users if Windows not updated for longer than two months
- VPN connectivity detection
- Support for AcceptProfileTypes tweak discontinued
- Switch to pre-release self-update channel
- OpenVPN updated to 2.5.9-20230427
    - openvpn 2.5.9 ea4ce681d9008f277706f4d90f2648ae043cbb2e
- libsodium updated to 1.0.18-20230427
    - libsodium 1.0.18 adef28f318564a757da6c848f2b6a38fad2cd1fa
- Fixes: #128, #202
- Breaks: #222


## [3.4](https://github.com/Amebis/eduVPN/compare/3.3.8...3.4) (2023-04-25)

- Version bumped


## [3.3.8](https://github.com/Amebis/eduVPN/compare/3.3.7...3.3.8) (2023-04-12)

- Fixes: #222


## [3.3.7](https://github.com/Amebis/eduVPN/compare/3.3.6...3.3.7) (2023-02-14)

- Detection of foreign default gateway VPN
- Fixes: #214, #217, #218
- GUI refinements
- "Renew Session" button and session expiration notification timings revised
- OpenVPN updated to 2.5.8-20230214
    - openvpn 2.5.8 1d81df042eae416a4e83e6a433ae2b937c5a10a4
- libsodium updated to 1.0.18-20230214
    - libsodium 1.0.18 39b4300cf2b80161034a21e69f8ad66335e22864


## [3.3.6](https://github.com/Amebis/eduVPN/compare/3.3.5...3.3.6) (2022-11-23)

- Enforce `--script-security 1` on OpenVPN connections


## [3.3.5](https://github.com/Amebis/eduVPN/compare/3.3.4...3.3.5) (2022-11-22)

- OpenVPN updated to 2.5.8-20221122
    - openvpn 2.5.8 b43a9b9f3324ccd7dffde3048c616aa5becc2b13
- Support for AcceptProfileTypes tweak
- Fixes: #213


## [3.3.4](https://github.com/Amebis/eduVPN/compare/3.3.3...3.3.4) (2022-11-02)

- OpenVPN updated to 2.5.8-20221102
    - openvpn 2.5.8 0357ceb877687faa2f3c671fcb8bc88b5a69b449
    - OpenSSL 3.0.7
- libsodium updated to 1.0.18-20221102
    - libsodium 1.0.18 stable fd5cbe9e696c1b886e45f3111dd099d51b12de6e
- Server bookkeeping as discovery changes


## [3.3.3](https://github.com/Amebis/eduVPN/compare/3.3.2...3.3.3) (2022-10-25)

- Server and organization discovery now pre-cached in eduVPN client


## [3.3.2](https://github.com/Amebis/eduVPN/compare/3.3.1...3.3.2) (2022-10-19)

- Preconfigurable Institute Access is no longer enforcing. Users may add additional servers.
- OpenVPN updated to 2.5.7-20221010
    - openvpn 2.5.7 af546d798213587285b225cd0031944a81e8e26c
- Translations updated
- Resilient web requests
- Fixes: #212


## [3.3.1](https://github.com/Amebis/eduVPN/compare/3.3...3.3.1) (2022-09-05)

- Increase minimum window height
- Show update button on the About page when available
- vcpkg updated to 2022.08.15 927006b24c3a28dfd8aa0ec5f8ce43098480a7f1
- OpenVPN updated to 2.5.7-20220905
    - openvpn 2.5.7 64cac790b9d64b3c07fa5222bf46754a04ea1659


## [3.3.0.1](https://github.com/Amebis/eduVPN/compare/3.3...3.3.0.1) (2022-11-02)

- OpenVPN updated to 2.5.7-20221102
    - openvpn 2.5.7 e3c397b0edd86158b8c417f6d396920a7e2eae68
    - OpenSSL 3.0.7


## [3.3](https://github.com/Amebis/eduVPN/compare/3.2.2...3.3) (2022-08-26)

- Fix AppVeyor vcpkg integration
- OpenVPN updated to 2.5.7-20220809
    - openvpn 2.5.7 e3c397b0edd86158b8c417f6d396920a7e2eae68
    - Fix AppVeyor vcpkg integration
- Version bumped


## [3.2.2](https://github.com/Amebis/eduVPN/compare/3.2.1...3.2.2) (2022-08-08)

- Fixes: #205, #207
- OpenVPN updated to 2.5.7-20220808
    - openvpn 2.5.7 e3c397b0edd86158b8c417f6d396920a7e2eae68


## [3.2.1](https://github.com/Amebis/eduVPN/compare/3.2...3.2.1) (2022-07-19)

- Allow SYSTEM user to delete configuration and log files on update or remove


## [3.2](https://github.com/Amebis/eduVPN/compare/3.1.8...3.2) (2022-07-13)

- Version bumped


## [3.1.8](https://github.com/Amebis/eduVPN/compare/3.1.7...3.1.8) (2022-07-12)

- Desktop shortcut made opt-out


## [3.1.7](https://github.com/Amebis/eduVPN/compare/3.1.6...3.1.7) (2022-07-12)

- Preconfigurable Institute Access
- Preconfigurable Secure Internet
- libsodium updated to 1.0.18-20220711
    - libsodium 1.0.18 stable d69a2342bccb98a3c28c0b7d5e4e6f3b8c789621
- OpenVPN configuration file extension changed from .conf to .ovpn
- OpenVPN configuration delivery over pipe and stdin
- OpenVPN updated to 2.5.7-20220712
    - openvpn 2.5.7 ce24bec7e2518d4ea7aa931021454d1191f4906b
    - DPAPI protection of .ovpn files
    - Interactive Service --config stdin support


## [3.1.6](https://github.com/Amebis/eduVPN/compare/3.1.5...3.1.6) (2022-07-07)

- OpenSSL updated to 3.0.5


## [3.1.5](https://github.com/Amebis/eduVPN/compare/3.1.4...3.1.5) (2022-07-04)

- OpenVPN updated to 2.5.7-20220704
    - openvpn 2.5.7 ce24bec7e2518d4ea7aa931021454d1191f4906b
- OpenSSL updated to 3.0.4


## [3.1.4](https://github.com/Amebis/eduVPN/compare/3.1.3...3.1.4) (2022-06-28)

- Fixes: #199


## [3.1.3](https://github.com/Amebis/eduVPN/compare/3.1.2...3.1.3) (2022-06-28)

- OpenVPN updated to 2.5.7-20220628
    - openvpn 2.5.7 70897fd139e84a64d6344bf6af28fe0b0b8087d3
    - openvpnserv client&pipe management revised.


## [3.1.2](https://github.com/Amebis/eduVPN/compare/3.1.1...3.1.2) (2022-06-27)

- Optionally install desktop shortcut


## [3.1.1](https://github.com/Amebis/eduVPN/compare/3.1...3.1.1) (2022-06-25)

- Fixes: #199


## [3.1](https://github.com/Amebis/eduVPN/compare/3.0.5...3.1) (2022-06-23)

- Switch to release self-update channel


## [3.0.5](https://github.com/Amebis/eduVPN/compare/3.0.4...3.0.5) (2022-06-22)

- WireGuard session abort-on-activation fixed


## [3.0.4](https://github.com/Amebis/eduVPN/compare/3.0.3...3.0.4) (2022-06-22)

- HTTP redirections on self-update installer download allowed


## [3.0.3](https://github.com/Amebis/eduVPN/compare/3.0.2...3.0.3) (2022-06-22)

- OpenVPN updated to 2.5.7-20220622
    - openvpn 2.5.7 70897fd139e84a64d6344bf6af28fe0b0b8087d3
    - Discontinue "openvpnserv fix unexpected termination", as it could bring openvpnserv into a dormant state not accepting new clients.
- Cleanups


## [3.0.2](https://github.com/Amebis/eduVPN/compare/3.0.1...3.0.2) (2022-06-18)

- Fixes: #197
- Switch to pre-release self-update channel
- VirusTotal file submissions automated
- OpenVPN updated to 2.5.7-20220618
    - openvpn 2.5.7 cf5864f5922e4f40357d9f75a35cd448e671dddf


## [3.0.1](https://github.com/Amebis/eduVPN/compare/3.0...3.0.1) (2022-06-09)

- Fixes: #194


## [3.0](https://github.com/Amebis/eduVPN/compare/2.255.6...3.0) (2022-05-12)

- Translations updated
- Switch to release self-update channel
- OpenVPN updated to 2.5.6-20220509
    - openvpn 2.5.6 55cfc0b9541ff25fac31059ffcf7eea06fd6c0ec
- libsodium updated to 1.0.18-20220509


## [2.255.6](https://github.com/Amebis/eduVPN/compare/2.255.5...2.255.6) (2022-05-05)

- OpenVPN updated to 2.5.6-20220505
    - openvpn 2.5.6 7b1b100557608db8a311d06f7578ceb7c4d33aa6
- Fixes


## [2.255.5](https://github.com/Amebis/eduVPN/compare/2.255.4...2.255.5) (2022-04-25)

- OpenVPN updated to 2.5.6-20220425
    - openvpn 2.5.6 f89b07831e8a6d0819b32d2fd6b15f430941ebcb
- GUI refinements


## [2.255.4](https://github.com/Amebis/eduVPN/compare/2.255.3...2.255.4) (2022-04-06)

- OpenVPN updated to 2.5.6-20220406
    - openvpn 2.5.6 aa6f15dd2a1df68409384d6f955f68692595b77b
    - support for DOMAIN-SEARCH
- Fixes


## [2.255.3](https://github.com/Amebis/eduVPN/compare/2.255.2...2.255.3) (2022-04-01)

- Fixes


## [2.255.2](https://github.com/Amebis/eduVPN/compare/2.255.1...2.255.2) (2022-03-30)

- WireGuard support
- Discover organizations as required
- OpenVPN updated to 2.5.6-20220330
    - openvpn 2.5.6 aa6f15dd2a1df68409384d6f955f68692595b77b
- libsodium updated to 1.0.18-20220307
- Stop using auto-generated fourth version fields in .NET assemblies
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
