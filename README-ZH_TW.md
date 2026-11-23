[简体中文](README.md) | [English](README-EN.md) | **繁體中文**

# PCL-CE-macOS

## **本專案與 PCL-CE 團隊沒有任何隸屬、授權、合作或背書關係**

**這不是 PCL-CE 官方 macOS 版本，也不是由 PCL-CE 團隊發佈、批准或維護的版本。**

PCL-CE 原專案也正在推進 macOS 適配。如果你較希望使用官方實作，可以等待原專案發布 macOS 版本。本專案只是社群成員為滿足自身使用需求而製作的非官方技術移植，無意與 PCL-CE 官方搶先發佈，也不代表官方立場；如果官方日後發布 macOS 版本，建議優先使用官方版本。

原版 PCL-CE、PCL 原版及其團隊成員與本專案沒有隸屬關係，也沒有為本專案的使用提供擔保。問題、建議與回饋請提交到[本專案儲存庫](https://github.com/Linghumy/PCL-CE-macOS/issues)，不要傳送給原開發者。

[原版 README 封存](docs/archive/upstream/README.zh-TW.md) · [PCL-CE 原專案](https://github.com/PCL-Community/PCL-CE) · [PCL 上游專案](https://github.com/Meloong-Git/PCL)

---

## 專案簡介

PCL-CE-macOS 是基於 [PCL Community Edition](https://github.com/PCL-Community/PCL-CE) 原始碼製作的**非官方、自包含 macOS 發行版**。它保留原啟動器 UI 與主要功能，包括：

- Microsoft 正版登入與第三方帳號登入
- Minecraft 原版、Forge、NeoForge、Fabric、Quilt 等安裝流程
- Mod、整合包、資源包、光影包、資料包及世界管理
- CurseForge、Modrinth 資源瀏覽與下載
- `.mrpack`、CurseForge 整合包及啟動器整合包匯入
- Java 自動選擇、版本管理與遊戲實例隔離
- 啟動器更新檢查、日誌檢視與錯誤分析
- 集中化資料目錄與可分類選擇的資料清理

`.app` 內建所需 .NET 執行環境，使用者無需預先安裝 SDK 或執行環境。

## 發行資訊

| 項目 | 值 |
|---|---|
| App Bundle | `PCL-CE-macOS.app` |
| Bundle ID | `cn.org.reasonable.pcl-ce-macos` |
| 預設架構 | Apple Silicon (`osx-arm64`) |
| 可選架構 | Intel (`osx-x64`) |
| 最低系統版本 | macOS 12 |
| 最終發行成品 | `dist/PCL-CE-macOS.dmg` |
| 簽章與公證 | Apple Developer ID 簽章、Apple 公證並 stapled |

## Apple 簽章與公證

本專案對外發布的 DMG 及其中 `PCL-CE-macOS.app` 使用 **Apple Developer ID Application** 憑證簽章，並已通過 Apple 公證服務驗證、完成公證及 stapled。簽章與公證票據可由 Gatekeeper 自動驗證，正常安裝與首次啟動時無需前往「系統設定」手動允許未知來源軟體。

簽章與公證由建置腳本中的 `CODESIGN_IDENTITY`、`APPLE_ID`、`APPLE_TEAM_ID` 與 `APPLE_APP_SPECIFIC_PASSWORD` 流程完成，相關憑證與金鑰不會提交到儲存庫。

直接從原始碼建置但未提供發行憑證的開發版本可能使用 ad-hoc 簽章，不屬於本專案對外發布的 Apple 公證版本。請從本專案的正式 Release 下載並使用公證成品。

## 集中化儲存

打包後的啟動器預設將本 App 產生的資料集中放在：

```text
~/PCL-CE/
├── Minecraft/   Minecraft 遊戲資料夾與實例
├── Launcher/    設定、登入狀態、日誌與啟動器狀態
├── Java/        由啟動器下載並管理的 Java 執行環境
├── Cache/       可重新產生的快取
└── Temp/        暫存檔案
```

可以設定 `PCL_CE_ROOT` 一次性遷移整組目錄。也可以分別覆寫 `PCL_DATA_DIR`、`PCL_MINECRAFT_DIR`、`PCL_JAVA_DIR`、`PCL_CACHE_DIR` 與 `PCL_TEMP_DIR`。

Minecraft 資料夾仍可在原版資料夾管理器中檢視、切換、移動與自訂。macOS 預設目錄為可見的 `~/PCL-CE/Minecraft`；舊版集中式 `.minecraft` 會在首次啟動時安全遷移。

設定中的「啟動器資料」支援自由組合清理：

- 啟動器設定與登入狀態
- 快取、日誌與暫存檔案
- 託管 Java 執行環境
- Minecraft 遊戲資料夾與實例

## 網路與遙測

- 啟動器更新僅檢查[本專案的 GitHub Releases](https://github.com/Linghumy/PCL-CE-macOS/releases)。
- 不再使用 Mirror 醬等原專案更新鏡像或 CDN。
- 不包含 Sentry 遙測、啟動器遙測開關或聯機身分上報。
- 原 PCL-CE 啟動器公告介面為公開唯讀服務；若介面可用則保留，並在公告標題中明確標示「來自原 PCL-CE 的公告」。

## 建置

所有 SDK、NuGet 套件與暫存檔案均保存在專案目錄內：

```bash
.toolchain/dotnet-install.sh \
  --channel 11.0 \
  --install-dir "$PWD/.toolchain/dotnet" \
  --no-path

scripts/build-macos-app.sh osx-arm64 Release
scripts/create-macos-dmg.sh
```

DMG 中包含 App、安裝箭頭與指向 `/Applications` 的捷徑。打包成功後只保留最終發行成品：

```text
dist/PCL-CE-macOS.dmg
```

## 授權條款

- `PCL.Launcher/` 與 `PCL.Launcher.Core/` 沿用 [PCL-CE 自訂授權條款](https://github.com/PCL-Community/PCL-CE/blob/dev/PCL.Launcher.Core/LICENCE)。
- 其餘目錄沿用 [Apache License 2.0](https://github.com/PCL-Community/PCL-CE/blob/dev/LICENSE)。

本專案對 PCL-CE 原始碼的修改、建置說明與非官方發行責任由本專案維護者承擔。
