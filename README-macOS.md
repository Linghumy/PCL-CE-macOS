# PCL-CE-macOS

PCL-CE-macOS is a self-contained macOS distribution based on
[PCL Community Edition](https://github.com/PCL-community/PCL-CE). It keeps the
original launcher UI and Minecraft workflows, including Microsoft account
login, Minecraft installation, mod and modpack installation, CurseForge and
Modrinth resources, `.mrpack` import, Java management, instance isolation, and
the existing launcher tools.

## Distribution identity

- App bundle: `PCL-CE-macOS.app`
- Bundle identifier: `cn.org.reasonable.pcl-ce-macos`
- Version: `2.15.1` (PCL-CE-macOS release 1)
- Architecture: Apple Silicon by default; the packaging script also accepts
  `osx-x64`

The app contains its own .NET runtime and does not require a separately
installed runtime.

## Centralized storage

The packaged app keeps launcher-owned data under one user-selected root:

```text
~/PCL-CE/
├── .minecraft/   Minecraft data and instances
├── Launcher/     configuration, profiles, logs, and launcher state
├── Java/         Java runtimes downloaded by the launcher
├── Cache/        disposable caches
└── Temp/         temporary files
```

Set `PCL_CE_ROOT` before launching to move the whole tree. Individual paths
can also be overridden with `PCL_DATA_DIR`, `PCL_MINECRAFT_DIR`,
`PCL_JAVA_DIR`, `PCL_CACHE_DIR`, and `PCL_TEMP_DIR`. The Minecraft folder
remains selectable and movable in the launcher's existing folder manager.

The settings page includes a data management section that can clear launcher
settings and login state, caches/logs/temporary files, managed Java runtimes,
and optionally Minecraft data. Categories can be selected independently.

## Build

All build dependencies stay inside the project:

```bash
.toolchain/dotnet-install.sh \
  --channel 11.0 \
  --install-dir "$PWD/.toolchain/dotnet" \
  --no-path

scripts/build-macos-app.sh osx-arm64 Release
```

The build output is exactly:

```text
dist/PCL-CE-macOS.app
```

The build script removes older `.app` bundles from `dist/` after a successful
new build. NuGet packages, temporary files, and the local SDK live in
`.cache/` and `.toolchain/`.

Microsoft login and CurseForge access can be supplied by build-time
`PCL_MS_CLIENT_ID` and `PCL_CURSEFORGE_API_KEY` environment variables with
`PCL_WRITE_SECRET=1`. They are intentionally not committed to the repository.
