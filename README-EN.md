[简体中文](README.md) | **English** | [繁體中文](README-ZH_TW.md)

# PCL-CE-macOS

## **This project has no affiliation, authorization, partnership, or endorsement relationship with the PCL-CE team**

**This is not an official PCL-CE macOS release, and it is not published, approved, or maintained by the PCL-CE team.**

The original PCL-CE project is also working on macOS support. If you prefer an official implementation, you may wait for the official macOS release. This project is an unofficial technical port created by community members for their own use. It does not intend to compete with the official project's release schedule or represent its views. If the PCL-CE team later releases an official macOS build, please prefer that version.

The original PCL-CE and PCL teams are not affiliated with this project and provide no guarantee for its use. Please submit issues and suggestions to [this repository](https://github.com/Linghumy/PCL-CE-macOS/issues), not to the original developers.

[Archived upstream README](docs/archive/upstream/README.en.md) · [PCL-CE project](https://github.com/PCL-Community/PCL-CE) · [PCL upstream](https://github.com/Meloong-Git/PCL)

---

## Overview

PCL-CE-macOS is an **unofficial, self-contained macOS distribution** built from the source code of [PCL Community Edition](https://github.com/PCL-Community/PCL-CE). It keeps the original launcher UI and major workflows, including:

- Microsoft and third-party account login
- Vanilla Minecraft, Forge, NeoForge, Fabric, Quilt, and other installation flows
- Mods, modpacks, resource packs, shaders, datapacks, and world management
- CurseForge and Modrinth browsing and downloads
- `.mrpack`, CurseForge modpack, and launcher modpack imports
- Automatic Java selection, Java management, and per-instance isolation
- Launcher update checks, logs, and crash analysis
- Centralized storage with independently selectable cleanup categories

The `.app` bundles its required .NET runtime. No separately installed SDK or runtime is needed.

## Distribution identity

| Item | Value |
|---|---|
| App bundle | `PCL-CE-macOS.app` |
| Bundle identifier | `cn.org.reasonable.pcl-ce-macos` |
| Default architecture | Apple Silicon (`osx-arm64`) |
| Optional architecture | Intel (`osx-x64`) |
| Minimum macOS version | macOS 12 |
| Final distribution artifact | `dist/PCL-CE-macOS.dmg` |
| Signing and notarization | Apple Developer ID signing, Apple notarization, and stapled ticket |

## Apple signing and notarization

The public DMG and the enclosed `PCL-CE-macOS.app` are signed with an **Apple Developer ID Application** certificate, submitted to and approved by Apple's notarization service, and have the notarization ticket stapled. Gatekeeper can verify the signature and ticket automatically, so normal installation and first launch do not require enabling unknown-source apps in System Settings.

Signing and notarization are performed through the packaging script's `CODESIGN_IDENTITY`, `APPLE_ID`, `APPLE_TEAM_ID`, and `APPLE_APP_SPECIFIC_PASSWORD` flow. Certificates and credentials are never committed to the repository.

Development builds compiled directly from source without distribution credentials may use ad-hoc signing and are not the notarized release distributed by this project. Download and use the notarized build from this project's official Release.

## Centralized storage

By default, all app-owned data is kept under:

```text
~/PCL-CE/
├── Minecraft/   Minecraft folders and instances
├── Launcher/    settings, login state, logs, and launcher state
├── Java/        Java runtimes managed by the launcher
├── Cache/       disposable caches
└── Temp/        temporary files
```

Set `PCL_CE_ROOT` to move the entire tree. Individual paths can also be overridden with `PCL_DATA_DIR`, `PCL_MINECRAFT_DIR`, `PCL_JAVA_DIR`, `PCL_CACHE_DIR`, and `PCL_TEMP_DIR`.

The Minecraft folder remains available to the original folder manager and can be viewed, switched, moved, or customized. On macOS the default is the visible `~/PCL-CE/Minecraft`; the old centralized `.minecraft` directory is migrated safely on first launch.

The launcher data section supports independently selecting:

- Launcher settings and login state
- Caches, logs, and temporary files
- Managed Java runtimes
- Minecraft folders and instances

## Network and telemetry

- Update checks use only [this project's GitHub Releases](https://github.com/Linghumy/PCL-CE-macOS/releases).
- Update mirrors and CDNs from the original project, including MirrorChyan, are not used.
- Sentry telemetry, launcher telemetry settings, and lobby identity reporting are removed.
- The original PCL-CE announcement endpoint is a public read-only service. If available, announcements are retained and labelled as originating from the original PCL-CE.

## Build

All build dependencies stay inside the repository:

```bash
.toolchain/dotnet-install.sh \
  --channel 11.0 \
  --install-dir "$PWD/.toolchain/dotnet" \
  --no-path

scripts/build-macos-app.sh osx-arm64 Release
scripts/create-macos-dmg.sh
```

The DMG contains the app, an installation arrow, and an `/Applications` shortcut. Only the final distribution artifact is retained after packaging:

```text
dist/PCL-CE-macOS.dmg
```

## License

- `PCL.Launcher/` and `PCL.Launcher.Core/` retain the [PCL-CE custom license](https://github.com/PCL-Community/PCL-CE/blob/dev/PCL.Launcher.Core/LICENCE).
- All other directories retain the [Apache License 2.0](https://github.com/PCL-Community/PCL-CE/blob/dev/LICENSE).

Responsibility for modifications, packaging, and this unofficial distribution rests with this project's maintainers.
