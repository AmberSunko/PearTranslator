# Local RapidOCR ONNX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a distributable local high-accuracy OCR option for Windows game subtitle translation, using ONNXRuntime-backed RapidOCR while keeping Windows OCR as the default fallback.

**Architecture:** Keep the core pipeline on the existing `IOcrEngine` abstraction. Add OCR settings to `TranslatorSettings`, let WPF expose OCR engine/language controls, and let `CompositionRoot` choose either Windows OCR or RapidOCR. The RapidOCR implementation lives in a separate OCR project boundary and maps recognized text plus line boxes back into the existing `OcrResult` geometry model.

**Tech Stack:** .NET 8, WPF, Windows OCR, RapidOcrNet, ONNXRuntime CPU, xUnit.

---

### Task 1: OCR Settings Model

**Files:**
- Modify: `src/PearTranslator.Core/Configuration/TranslatorSettings.cs`
- Test: `tests/PearTranslator.Core.Tests/Configuration/TranslatorSettingsTests.cs`

- [ ] Add `OcrSettings`, `OcrEngineKind`, and `OcrLanguageKind`.
- [ ] Keep defaults as `Windows` and `English`.
- [ ] Add a serialization test proving missing OCR settings load with defaults.
- [ ] Run `dotnet test .\tests\PearTranslator.Core.Tests\PearTranslator.Core.Tests.csproj --no-restore --filter FullyQualifiedName~TranslatorSettingsTests`.

### Task 2: UI Controls

**Files:**
- Modify: `src/PearTranslator.App.Wpf/MainWindow.xaml`
- Modify: `src/PearTranslator.App.Wpf/MainWindow.xaml.cs`

- [ ] Add Chinese UI labels for OCR engine and OCR language near the existing translation provider settings.
- [ ] Add choices: `Windows 内置`, `本地高精度`; language choices: `英文`, `日文`, `韩文`.
- [ ] Persist OCR choices through `ReadSettingsFromControls`.
- [ ] Apply settings through `CompositionRoot.ApplySettings`.
- [ ] Run `dotnet build .\src\PearTranslator.App.Wpf\PearTranslator.App.Wpf.csproj --no-restore`.

### Task 3: RapidOCR Engine Project

**Files:**
- Modify: `src/PearTranslator.Ocr.Windows/PearTranslator.Ocr.Windows.csproj`
- Create: `src/PearTranslator.Ocr.Windows/RapidOcrEngine.cs`
- Create: `src/PearTranslator.Ocr.Windows/OcrEngineFactory.cs`

- [ ] Add `RapidOcrNet` NuGet package to the Windows OCR project.
- [ ] Implement `RapidOcrEngine.TryCreate(language)` so failures return `null` and do not crash app startup.
- [ ] Map RapidOCR result lines into `OcrResult.Text`, `EstimatedTextHeightPixels`, `TextBoundsPixels`, and `TextLines`.
- [ ] Keep Windows OCR fallback when RapidOCR package/model loading fails.
- [ ] Run `dotnet build .\src\PearTranslator.Ocr.Windows\PearTranslator.Ocr.Windows.csproj --no-restore`.

### Task 4: Composition Wiring

**Files:**
- Modify: `src/PearTranslator.App.Wpf/CompositionRoot.cs`

- [ ] Replace `CreateOcrEngine()` with settings-aware OCR creation.
- [ ] Use `OcrEngineFactory.Create(settings.Ocr)` to select Windows or RapidOCR.
- [ ] Preserve mock fallback only if no real OCR engine can be created.
- [ ] Run `dotnet test .\PearTranslator.sln --no-restore`.

### Task 5: Runtime Verification

**Files:**
- Modify only if verification reveals compile/runtime issues.

- [ ] Stop any existing `PearTranslator.App.Wpf` process.
- [ ] Build the WPF app.
- [ ] Start the WPF app.
- [ ] Confirm process remains alive.
- [ ] Report whether RapidOCR package/model loading is active or currently falling back to Windows OCR.
