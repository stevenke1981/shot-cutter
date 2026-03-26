# ShotCutter — 專案計劃書

> 影片截圖工具：WPF (.NET 8) 桌面應用程式，以 FFmpeg 為核心，支援 5 種擷取模式、批次處理、智慧場景分析，並輸出 HTML 瀏覽器相簿。

---

## 一、專案概覽

| 項目 | 說明 |
|------|------|
| 語言 | C# 12 / .NET 8 LTS |
| UI 框架 | WPF + WPF-UI (lepoco) v3.0.5 (Fluent Dark) |
| 視訊引擎 | FFmpeg / FFprobe |
| DI / Hosting | Microsoft.Extensions.Hosting 8.0 |
| MVVM | CommunityToolkit.Mvvm 8.4 |
| 測試 | xUnit 2.5 + Moq + coverlet |
| 解決方案 | `ShotCutter.sln` → 4 個專案 |

---

## 二、解決方案結構

```
ShotCutter.sln
├── src/
│   ├── ShotCutter.Core          ← 純業務邏輯 (net8.0)
│   ├── ShotCutter.SmartAnalysis ← 智慧場景分析 (net8.0)
│   └── ShotCutter.App           ← WPF UI 入口 (net8.0-windows)
└── tests/
    └── ShotCutter.Core.Tests    ← 單元測試 (net8.0)
```

---

## 三、擷取模式（5 種）

| 模式 | 說明 | 參數 |
|------|------|------|
| `Interval` | 等時間間隔截圖 | `IntervalSeconds` (0.5–60) |
| `TimePoint` | 指定時間點 | `TimePointsText`（HH:MM:SS 或小數秒） |
| `KeyFrame` | 只擷取關鍵幀 | 無額外參數 |
| `SceneChange` | 偵測場景切換 | `SceneChangeThreshold` (0.05–0.95) |
| `FirstLastFrame` | 第一幀 / 最後一幀 | `CaptureFirstFrame`, `CaptureLastFrame` |

---

## 四、已完成項目（Phase 1 ✅）

### ShotCutter.Core
- [x] `Models/` — VideoInfo, AppSettings, CaptureMode, OutputFormat, CaptureOptions, ScreenshotResult, BatchProgress, ScreenshotTask
- [x] `Capture/ICaptureStrategy` — 統一擷取介面
- [x] `Capture/IntervalCaptureStrategy`
- [x] `Capture/TimePointCaptureStrategy`
- [x] `Capture/KeyFrameCaptureStrategy`
- [x] `Capture/SceneChangeCaptureStrategy`
- [x] `Capture/FirstLastFrameStrategy`
- [x] `Services/FFmpegService` + `IFFmpegService`
- [x] `Services/FFprobeService` + `IFFprobeService`
- [x] `Services/ScreenshotEngine` + `IScreenshotEngine`
- [x] `Services/SettingsService` (JSON 持久化, `%AppData%\ShotCutter\settings.json`)
- [x] `Services/BrowserService` (HTML gallery 生成 + 瀏覽器開啟)

### ShotCutter.App
- [x] `App.xaml` / `App.xaml.cs` — DI 容器、主題、ObjectDataProvider
- [x] `MainWindow` — FluentWindow + NavigationView
- [x] `ViewModels/MainWindowViewModel`
- [x] `ViewModels/DashboardViewModel` — 影片清單、5 模式參數、批次擷取、進度回報
- [x] `ViewModels/ResultsViewModel` — 截圖清單瀏覽、開啟相簿
- [x] `ViewModels/SettingsViewModel` — AppSettings 雙向綁定
- [x] `Views/Pages/DashboardPage.xaml` — 兩欄佈局、拖放、DataTrigger 動態選項面板
- [x] `Views/Pages/ResultsPage.xaml` — WrapPanel 縮圖格
- [x] `Views/Pages/SettingsPage.xaml` — 設定表單
- [x] `build.bat` — 一鍵 Restore / Build / Publish / Clean

### 基礎設施
- [x] Git 初始化 + 初版提交 (286 files)
- [x] GitHub repo: https://github.com/stevenke1981/shot-cutter

---

## 五、待完成項目（Phase 2）

### 5.1 Bug 修復（優先級：高）

| # | 問題 | 修復方式 |
|---|------|---------|
| B-1 | `CountToVisibilityConverter` 未定義 → Runtime ResourceNotFound | 新增 `Converters/CountToVisibilityConverter.cs` + 在 App.xaml 註冊 |
| B-2 | 無 `EnumToDescriptionConverter` — ComboBox 顯示原始 enum 名稱 | 新增 converter + `[Description]` 標註 enum 值 |

### 5.2 Converters（優先級：高）

- [ ] `CountToVisibilityConverter` — `int→Visibility`，0=Visible（空狀態）, >0=Collapsed
- [ ] `EnumToDescriptionConverter` — 顯示 `[Description]` 或 fallback 到 enum 名稱
- [ ] `InverseBoolToVisibilityConverter` — 反轉 bool→Visibility
- [ ] `FileSizeConverter` — bytes → 人類可讀字串（KB / MB / GB）

### 5.3 ShotCutter.SmartAnalysis（優先級：中）

智慧場景分析 — 不依賴 ML.NET，純粹以 FFmpeg 輸出幀 + 直方圖差異計算：

- [ ] `ISceneAnalyzer` — `AnalyzeAsync(videoPath, ct)` → `IReadOnlyList<SceneSegment>`
- [ ] `SceneSegment` model — `StartTime`, `EndTime`, `Thumbnail`, `MotionScore`
- [ ] `HistogramSceneAnalyzer` — 用 FFmpeg 以低 fps 提取幀 → 計算每幀 RGB 直方圖差異 → 找分段點
- [ ] `SmartCaptureStrategy` — `ICaptureStrategy` 實作，使用 `HistogramSceneAnalyzer` 偵測 → 對每段取代表幀
- [ ] `SceneInfo` — 分析結果 DTO

### 5.4 Controls（優先級：中）

- [ ] `Controls/VideoThumbnailControl.xaml` — 顯示影片封面幀 + 時長 overlay (UserControl)
- [ ] `Controls/ImagePreviewOverlay.xaml` — 點擊縮圖後顯示全螢幕預覽 (Popup/Overlay)

### 5.5 UI 增強（優先級：中）

- [ ] DashboardPage: 拖放時顯示 DragOver 視覺回饋（半透明 overlay）
- [ ] ResultsPage: 左側縮圖格 + 右側選取圖片詳細資訊面板（路徑、時間戳、尺寸）
- [ ] ResultsPage: 支援鍵盤左右方向鍵切換截圖
- [ ] ResultsPage: 「另存新檔」右鍵選單
- [ ] MainWindow: 在 TitleBar 顯示作業進度（ProgressRing）

### 5.6 單元測試（優先級：高）

- [ ] `CaptureOptionsTests` — ParseTimePoints 邊界測試
- [ ] `SettingsServiceTests` — 讀寫 JSON 設定（使用暫存目錄）
- [ ] `BrowserServiceTests` — HTML gallery 內容驗證
- [ ] `ScreenshotEngineTests` — Mock IFFmpegService + 驗證批次行為
- [ ] `BatchProgressTests` — OverallProgress 計算

### 5.7 建置 / 打包（優先級：低）

- [ ] 打包 FFmpeg/FFprobe 到 `tools/ffmpeg/`（下載腳本 `scripts/get-ffmpeg.ps1`）
- [ ] 建立 NSIS 或 Inno Setup 安裝程式腳本
- [ ] GitHub Actions CI（dotnet build + test）

---

## 六、架構決策紀錄（ADR）

### ADR-001 — 策略模式 vs switch/case
- **決定：** 使用 `ICaptureStrategy` 策略模式
- **原因：** 可獨立測試、DI 注入、開放/關閉原則，未來新增模式不修改現有程式碼

### ADR-002 — SmartAnalysis 不依賴 ML.NET
- **決定：** 純色階直方圖差異 + FFmpeg lavfi `thumbnail` filter
- **原因：** 避免 ML.NET 的大型 NuGet 依賴與 runtime 要求，直方圖在常見場景切換效果足夠，執行輕量快速

### ADR-003 — SettingsService 使用 JSON 而非 Registry
- **決定：** `%AppData%\ShotCutter\settings.json`
- **原因：** 跨機器備份容易、可被版本控管工具觀察、測試時可指定暫存路徑

### ADR-004 — BrowserService 生成靜態 HTML
- **決定：** 生成自包含 HTML（inline CSS）到 `%TEMP%\ShotCutter\`
- **原因：** 不依賴外部 web server，圖片以 `file://` 絕對路徑引用，相容所有主流瀏覽器

---

## 七、執行計劃

```
Week 1   Phase 2 Bug 修復 + Converters + 單元測試
Week 2   SmartAnalysis (HistogramSceneAnalyzer + SmartCaptureStrategy)
Week 3   UI 增強 (DragOver overlay, ResultsPage 詳情面板, ImagePreviewOverlay)
Week 4   打包 + CI/CD + README
```

---

## 八、技術債清單

| 項目 | 嚴重度 | 說明 |
|------|--------|------|
| `CountToVisibilityConverter` 缺失 | 🔴 高 | App 啟動時 ResourceNotFoundException |
| SmartAnalysis 空專案 | 🟡 中 | csproj 存在但無任何來源 |
| 測試皆為佔位符 | 🟡 中 | `UnitTest1.cs` 僅含空測試 |
| ComboBox 顯示原始 enum 名 | 🟡 中 | 需 EnumToDescriptionConverter |
| 無 CI/CD pipeline | 🟢 低 | 需 GitHub Actions workflow |
| 無 FFmpeg bundling 腳本 | 🟢 低 | 需 `get-ffmpeg.ps1` |
