**简体中文** | [English](README-EN.md) | [繁體中文](README-ZH_TW.md)

# PCL-CE-macOS

## **本项目与 PCL-CE 团队没有任何隶属、授权、合作或背书关系**

**这不是 PCL-CE 官方 macOS 版本，也不是由 PCL-CE 团队发布、批准或维护的版本。**

PCL-CE 原项目也正在推进 macOS 适配。如果你更希望使用官方实现，可以等待原项目发布 macOS 版本。本项目只是社区成员为满足自身使用需求而制作的非官方技术移植，无意与 PCL-CE 官方抢发布，也不代表官方立场；如果官方后续发布了 macOS 版本，建议优先使用官方版本。

原版 PCL-CE、PCL 原版及其团队成员与本项目没有隶属关系，也没有为本项目的使用提供担保。问题、建议和反馈请提交到[本项目仓库](https://github.com/Linghumy/PCL-CE-macOS/issues)，不要发送给原开发者。

[原版 README 归档](docs/archive/upstream/README.zh-CN.md) · [PCL-CE 原项目](https://github.com/PCL-Community/PCL-CE) · [PCL 上游项目](https://github.com/Meloong-Git/PCL)

---

## 项目简介

PCL-CE-macOS 是基于 [PCL Community Edition](https://github.com/PCL-Community/PCL-CE) 源码制作的**非官方、自包含 macOS 发行版**。它保留原启动器 UI 和主要功能，包括：

- Microsoft 正版登录与第三方账户登录
- Minecraft 原版、Forge、NeoForge、Fabric、Quilt 等安装流程
- Mod、整合包、资源包、光影包、数据包及世界管理
- CurseForge、Modrinth 资源浏览与下载
- `.mrpack`、CurseForge 整合包及启动器整合包导入
- Java 自动选择、版本管理、游戏实例隔离
- 启动器更新检查、日志查看与错误分析
- 集中化数据目录和可选择分类的数据清理

`.app` 内置所需 .NET 运行时，用户无需预先安装 SDK 或运行时。

## 主要区别

- 启用了Minecraft游戏资源下载
- 将启动器的所有文件（包括配置、Java与Minecraft游戏文件夹）存放于 /Users/用户名/PCL-CE 中，便于管理

## Apple 签名与公证

本项目对外发布的 DMG 及其中的 `PCL-CE-macOS.app` 使用 **Apple Developer ID Application** 证书签名，并已通过 Apple 公证服务验证、完成公证及 stapled。签名和公证票据可由 Gatekeeper 自动验证，正常安装和首次启动时无需前往“系统设置”手动允许未知来源软件。

签名与公证由构建脚本中的 `CODESIGN_IDENTITY`、`APPLE_ID`、`APPLE_TEAM_ID` 和 `APPLE_APP_SPECIFIC_PASSWORD` 流程完成，相关证书和凭据不会提交到仓库。

直接从源码构建但不提供发行证书的开发构建可能使用 ad-hoc 签名，不属于本项目对外发布的 Apple 公证版本。请从本项目的正式 Release 下载并使用公证成品。


## 发行信息

| 项目 | 值 |
|---|---|
| App Bundle | `PCL-CE-macOS.app` |
| Bundle ID | `cn.org.reasonable.pcl-ce-macos` |
| 默认架构 | Apple Silicon (`osx-arm64`) |
| 可选架构 | Intel (`osx-x64`) |
| 最低系统版本 | macOS 12 |
| 最终分发成品 | `dist/PCL-CE-macOS.dmg` |
| 签名与公证 | Apple Developer ID 签名、Apple 公证并 stapled |

## 集中化存储

打包后的启动器默认将本 App 产生的数据集中放在：

```text
~/PCL-CE/
├── Minecraft/    Minecraft 游戏文件夹与实例
├── Launcher/     配置、登录状态、日志和启动器状态
├── Java/         由启动器下载并管理的 Java 运行环境
├── Cache/        可重新生成的缓存
└── Temp/         临时文件
```

可以设置 `PCL_CE_ROOT` 一次性迁移整套目录。也可以分别覆盖：

- `PCL_DATA_DIR`
- `PCL_MINECRAFT_DIR`
- `PCL_JAVA_DIR`
- `PCL_CACHE_DIR`
- `PCL_TEMP_DIR`

Minecraft 文件夹仍可在原版的文件夹管理器中查看、切换、移动和自定义。macOS 默认目录为可见的 `~/PCL-CE/Minecraft`；旧版集中的 `.minecraft` 会在首次启动时安全迁移。

设置中的“启动器数据”支持自由组合清理：

- 启动器配置与登录状态
- 缓存、日志与临时文件
- 托管 Java 运行环境
- Minecraft 游戏文件夹与实例

## 网络与遥测

- 启动器更新仅检查[本项目的 GitHub Releases](https://github.com/Linghumy/PCL-CE-macOS/releases)。
- 不再使用 Mirror 酱等原项目更新镜像或 CDN。
- 不包含 Sentry 遥测、启动器遥测开关或联机身份上报。
- 原 PCL-CE 启动器公告接口为公开只读服务；若接口可用则保留，并在公告标题中明确标注“来自原 PCL-CE 的公告”。

## 构建

所有 SDK、NuGet 包和临时文件均保存在项目目录内：

```bash
.toolchain/dotnet-install.sh \
  --channel 11.0 \
  --install-dir "$PWD/.toolchain/dotnet" \
  --no-path

scripts/build-macos-app.sh osx-arm64 Release
scripts/create-macos-dmg.sh
```

DMG 中包含 App、安装箭头和指向 `/Applications` 的快捷方式。打包成功后只保留最终分发成品：

```text
dist/PCL-CE-macOS.dmg
```

## 许可证

- `PCL.Launcher/` 与 `PCL.Launcher.Core/` 沿用 [PCL-CE 自定义许可证](https://github.com/PCL-Community/PCL-CE/blob/dev/PCL.Launcher.Core/LICENCE)。
- 其余目录沿用 [Apache License 2.0](https://github.com/PCL-Community/PCL-CE/blob/dev/LICENSE)。

本项目对 PCL-CE 源码的修改、构建说明和非官方发行责任由本项目维护者承担。
