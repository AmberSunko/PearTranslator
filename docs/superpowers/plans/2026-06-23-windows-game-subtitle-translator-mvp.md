# Windows Game Subtitle Translator MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first usable Windows WPF slice of PearTranslator: a polished desktop shell, region selector, translation overlay, pause/resume, dismiss, one-shot screenshot translation, and a tested core loop using mock OCR and mock translation.

**Architecture:** Keep WPF presentation separate from translation behavior. `PearTranslator.Core` owns state, text stabilization, caching, and the runtime loop; `PearTranslator.App.Wpf` owns Windows UI, tray, hotkeys, region selection, and overlay rendering. The first slice uses mock capture/OCR/translation dependencies so the UI and pipeline can be verified before real providers are introduced.

**Tech Stack:** .NET 8 SDK, C# 12, WPF, xUnit, Windows Forms NotifyIcon for tray integration, Win32 hotkey registration through P/Invoke.

---

## Prerequisites

The current machine has .NET Runtime 8.0.21 but no .NET SDK. Before executing this plan, install:

- .NET 8 SDK for Windows x64.
- Git for Windows when commits are desired.

Verification commands:

```powershell
dotnet --list-sdks
git --version
```

Expected:

- `dotnet --list-sdks` prints at least one `8.0.x` SDK.
- `git --version` prints a Git version if commit steps will be run.

## File Structure

Create these projects and files:

```text
PearTranslator.sln
Directory.Build.props

src/PearTranslator.Core/PearTranslator.Core.csproj
src/PearTranslator.Core/Abstractions/CapturedFrame.cs
src/PearTranslator.Core/Abstractions/FrameRegion.cs
src/PearTranslator.Core/Abstractions/IFrameChangeDetector.cs
src/PearTranslator.Core/Abstractions/IOcrEngine.cs
src/PearTranslator.Core/Abstractions/IOverlayPresenter.cs
src/PearTranslator.Core/Abstractions/IRegionCapture.cs
src/PearTranslator.Core/Abstractions/ITranslationProvider.cs
src/PearTranslator.Core/Configuration/TranslatorOptions.cs
src/PearTranslator.Core/Control/TranslatorController.cs
src/PearTranslator.Core/Control/TranslatorRunState.cs
src/PearTranslator.Core/Pipeline/SubtitleTranslationLoop.cs
src/PearTranslator.Core/Text/TextNormalizer.cs
src/PearTranslator.Core/Text/TextStabilizer.cs
src/PearTranslator.Core/Translation/TranslationCache.cs

src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
src/PearTranslator.App.Wpf/App.xaml
src/PearTranslator.App.Wpf/App.xaml.cs
src/PearTranslator.App.Wpf/MainWindow.xaml
src/PearTranslator.App.Wpf/MainWindow.xaml.cs
src/PearTranslator.App.Wpf/CompositionRoot.cs
src/PearTranslator.App.Wpf/Hotkeys/GlobalHotkeyService.cs
src/PearTranslator.App.Wpf/Mocks/MockOcrEngine.cs
src/PearTranslator.App.Wpf/Mocks/MockRegionCapture.cs
src/PearTranslator.App.Wpf/Mocks/MockTranslationProvider.cs
src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml
src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml.cs
src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml
src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml.cs
src/PearTranslator.App.Wpf/Tray/TrayIconService.cs

tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj
tests/PearTranslator.Core.Tests/Control/TranslatorControllerTests.cs
tests/PearTranslator.Core.Tests/Text/TextStabilizerTests.cs
tests/PearTranslator.Core.Tests/Translation/TranslationCacheTests.cs
tests/PearTranslator.Core.Tests/Pipeline/SubtitleTranslationLoopTests.cs
```

## Task 1: Scaffold Solution

**Files:**
- Create: `PearTranslator.sln`
- Create: `Directory.Build.props`
- Create: `src/PearTranslator.Core/PearTranslator.Core.csproj`
- Create: `src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj`
- Create: `tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n PearTranslator
dotnet new classlib -n PearTranslator.Core -o src/PearTranslator.Core -f net8.0
dotnet new wpf -n PearTranslator.App.Wpf -o src/PearTranslator.App.Wpf -f net8.0
dotnet new xunit -n PearTranslator.Core.Tests -o tests/PearTranslator.Core.Tests -f net8.0
dotnet sln PearTranslator.sln add src/PearTranslator.Core/PearTranslator.Core.csproj
dotnet sln PearTranslator.sln add src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
dotnet sln PearTranslator.sln add tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj
dotnet add src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj reference src/PearTranslator.Core/PearTranslator.Core.csproj
dotnet add tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj reference src/PearTranslator.Core/PearTranslator.Core.csproj
```

Expected: all commands complete without errors.

- [ ] **Step 2: Replace `Directory.Build.props`**

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Replace WPF project file**

Replace `src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\PearTranslator.Core\PearTranslator.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Remove generated placeholder class**

Delete `src/PearTranslator.Core/Class1.cs`.

- [ ] **Step 5: Build solution**

Run:

```powershell
dotnet build PearTranslator.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit scaffold when Git is available**

Run:

```powershell
git add PearTranslator.sln Directory.Build.props src tests
git commit -m "chore: scaffold PearTranslator solution"
```

Expected: commit succeeds when Git is installed.

## Task 2: Core Translation Control State

**Files:**
- Create: `src/PearTranslator.Core/Control/TranslatorRunState.cs`
- Create: `src/PearTranslator.Core/Control/TranslatorController.cs`
- Create: `tests/PearTranslator.Core.Tests/Control/TranslatorControllerTests.cs`

- [ ] **Step 1: Write failing state tests**

Create `tests/PearTranslator.Core.Tests/Control/TranslatorControllerTests.cs`:

```csharp
using PearTranslator.Core.Control;

namespace PearTranslator.Core.Tests.Control;

public sealed class TranslatorControllerTests
{
    [Fact]
    public void StartsRunning()
    {
        var controller = new TranslatorController();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void PauseHidesOverlayAndResumeShowsIt()
    {
        var controller = new TranslatorController();

        controller.TogglePause();

        Assert.Equal(TranslatorRunState.Paused, controller.State);
        Assert.False(controller.ShouldCapture);
        Assert.False(controller.ShouldShowOverlay);

        controller.TogglePause();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldCapture);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void DismissHidesCurrentSubtitleUntilDifferentSourceArrives()
    {
        var controller = new TranslatorController();
        controller.AcceptStableSourceText("Hello");

        controller.DismissCurrent();

        Assert.Equal(TranslatorRunState.Dismissed, controller.State);
        Assert.False(controller.ShouldShowOverlay);

        controller.AcceptStableSourceText("Hello");

        Assert.Equal(TranslatorRunState.Dismissed, controller.State);
        Assert.False(controller.ShouldShowOverlay);

        controller.AcceptStableSourceText("Welcome back");

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void DismissResumesWhenDifferentSourceArrivesImmediately()
    {
        var controller = new TranslatorController();
        controller.AcceptStableSourceText("Hello");

        controller.DismissCurrent();
        controller.AcceptStableSourceText("Welcome back");

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TranslatorControllerTests
```

Expected: test run fails because `TranslatorController` and `TranslatorRunState` do not exist.

- [ ] **Step 3: Add control state implementation**

Create `src/PearTranslator.Core/Control/TranslatorRunState.cs`:

```csharp
namespace PearTranslator.Core.Control;

public enum TranslatorRunState
{
    Running,
    Paused,
    Dismissed
}
```

Create `src/PearTranslator.Core/Control/TranslatorController.cs`:

```csharp
namespace PearTranslator.Core.Control;

public sealed class TranslatorController
{
    private string? _currentSourceText;
    private string? _dismissedSourceText;

    public TranslatorRunState State { get; private set; } = TranslatorRunState.Running;

    public bool ShouldCapture => State is TranslatorRunState.Running or TranslatorRunState.Dismissed;

    public bool ShouldShowOverlay => State == TranslatorRunState.Running;

    public void TogglePause()
    {
        if (State == TranslatorRunState.Paused)
        {
            State = TranslatorRunState.Running;
            return;
        }

        _dismissedSourceText = null;
        State = TranslatorRunState.Paused;
    }

    public void DismissCurrent()
    {
        if (State != TranslatorRunState.Running)
        {
            return;
        }

        _dismissedSourceText = _currentSourceText;
        State = TranslatorRunState.Dismissed;
    }

    public void AcceptStableSourceText(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return;
        }

        _currentSourceText = sourceText;

        if (State == TranslatorRunState.Dismissed)
        {
            if (_dismissedSourceText is not null &&
                string.Equals(_dismissedSourceText, sourceText, StringComparison.Ordinal))
            {
                return;
            }

            _dismissedSourceText = null;
            State = TranslatorRunState.Running;
        }
    }
}
```

- [ ] **Step 4: Run state tests**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TranslatorControllerTests
```

Expected: tests pass.

- [ ] **Step 5: Commit state control**

Run:

```powershell
git add src/PearTranslator.Core/Control tests/PearTranslator.Core.Tests/Control
git commit -m "feat: add translator control state"
```

Expected: commit succeeds when Git is installed.

## Task 3: Text Normalization And Stabilization

**Files:**
- Create: `src/PearTranslator.Core/Text/TextNormalizer.cs`
- Create: `src/PearTranslator.Core/Text/TextStabilizer.cs`
- Create: `tests/PearTranslator.Core.Tests/Text/TextStabilizerTests.cs`

- [ ] **Step 1: Write failing stabilization tests**

Create `tests/PearTranslator.Core.Tests/Text/TextStabilizerTests.cs`:

```csharp
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Tests.Text;

public sealed class TextStabilizerTests
{
    [Fact]
    public void RequiresSameNormalizedTextTwice()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe("  Hello   world "));
        Assert.Equal("Hello world", stabilizer.Observe("Hello world"));
    }

    [Fact]
    public void ResetsWhenTextChanges()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe("Hello"));
        Assert.Null(stabilizer.Observe("Welcome"));
        Assert.Equal("Welcome", stabilizer.Observe("Welcome"));
    }

    [Fact]
    public void IgnoresBlankText()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe(" "));
        Assert.Null(stabilizer.Observe(""));
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TextStabilizerTests
```

Expected: test run fails because `TextStabilizer` does not exist.

- [ ] **Step 3: Add text components**

Create `src/PearTranslator.Core/Text/TextNormalizer.cs`:

```csharp
using System.Text.RegularExpressions;

namespace PearTranslator.Core.Text;

public static partial class TextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return WhitespacePattern().Replace(text.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
```

Create `src/PearTranslator.Core/Text/TextStabilizer.cs`:

```csharp
namespace PearTranslator.Core.Text;

public sealed class TextStabilizer
{
    private readonly int _requiredRepeats;
    private string? _lastText;
    private int _repeatCount;

    public TextStabilizer(int requiredRepeats)
    {
        if (requiredRepeats < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredRepeats), "Repeat count must be at least 1.");
        }

        _requiredRepeats = requiredRepeats;
    }

    public string? Observe(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (normalized.Length == 0)
        {
            return null;
        }

        if (string.Equals(_lastText, normalized, StringComparison.Ordinal))
        {
            _repeatCount++;
        }
        else
        {
            _lastText = normalized;
            _repeatCount = 1;
        }

        return _repeatCount >= _requiredRepeats ? normalized : null;
    }
}
```

- [ ] **Step 4: Run stabilization tests**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TextStabilizerTests
```

Expected: tests pass.

- [ ] **Step 5: Commit text stabilization**

Run:

```powershell
git add src/PearTranslator.Core/Text tests/PearTranslator.Core.Tests/Text
git commit -m "feat: stabilize OCR text"
```

Expected: commit succeeds when Git is installed.

## Task 4: Translation Cache

**Files:**
- Create: `src/PearTranslator.Core/Translation/TranslationCache.cs`
- Create: `tests/PearTranslator.Core.Tests/Translation/TranslationCacheTests.cs`

- [ ] **Step 1: Write failing cache tests**

Create `tests/PearTranslator.Core.Tests/Translation/TranslationCacheTests.cs`:

```csharp
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Translation;

public sealed class TranslationCacheTests
{
    [Fact]
    public void ReturnsCachedTranslationForNormalizedSource()
    {
        var cache = new TranslationCache();

        cache.Store("Hello   world", "你好，世界");

        Assert.True(cache.TryGet("Hello world", out var translation));
        Assert.Equal("你好，世界", translation);
    }

    [Fact]
    public void DoesNotStoreBlankSourceOrTranslation()
    {
        var cache = new TranslationCache();

        cache.Store("", "你好");
        cache.Store("Hello", "");

        Assert.False(cache.TryGet("", out _));
        Assert.False(cache.TryGet("Hello", out _));
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TranslationCacheTests
```

Expected: test run fails because `TranslationCache` does not exist.

- [ ] **Step 3: Add cache implementation**

Create `src/PearTranslator.Core/Translation/TranslationCache.cs`:

```csharp
using PearTranslator.Core.Text;

namespace PearTranslator.Core.Translation;

public sealed class TranslationCache
{
    private readonly Dictionary<string, string> _translations = new(StringComparer.Ordinal);

    public bool TryGet(string sourceText, out string translation)
    {
        var key = TextNormalizer.Normalize(sourceText);
        if (key.Length == 0)
        {
            translation = string.Empty;
            return false;
        }

        if (_translations.TryGetValue(key, out var cached))
        {
            translation = cached;
            return true;
        }

        translation = string.Empty;
        return false;
    }

    public void Store(string sourceText, string translation)
    {
        var key = TextNormalizer.Normalize(sourceText);
        var value = TextNormalizer.Normalize(translation);

        if (key.Length == 0 || value.Length == 0)
        {
            return;
        }

        _translations[key] = value;
    }
}
```

- [ ] **Step 4: Run cache tests**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter TranslationCacheTests
```

Expected: tests pass.

- [ ] **Step 5: Commit translation cache**

Run:

```powershell
git add src/PearTranslator.Core/Translation tests/PearTranslator.Core.Tests/Translation
git commit -m "feat: cache translations"
```

Expected: commit succeeds when Git is installed.

## Task 5: Core Runtime Loop With Replaceable Dependencies

**Files:**
- Create: `src/PearTranslator.Core/Abstractions/CapturedFrame.cs`
- Create: `src/PearTranslator.Core/Abstractions/FrameRegion.cs`
- Create: `src/PearTranslator.Core/Abstractions/IFrameChangeDetector.cs`
- Create: `src/PearTranslator.Core/Abstractions/IOcrEngine.cs`
- Create: `src/PearTranslator.Core/Abstractions/IOverlayPresenter.cs`
- Create: `src/PearTranslator.Core/Abstractions/IRegionCapture.cs`
- Create: `src/PearTranslator.Core/Abstractions/ITranslationProvider.cs`
- Create: `src/PearTranslator.Core/Configuration/TranslatorOptions.cs`
- Create: `src/PearTranslator.Core/Pipeline/SubtitleTranslationLoop.cs`
- Create: `tests/PearTranslator.Core.Tests/Pipeline/SubtitleTranslationLoopTests.cs`

- [ ] **Step 1: Write failing loop tests**

Create `tests/PearTranslator.Core.Tests/Pipeline/SubtitleTranslationLoopTests.cs`:

```csharp
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Pipeline;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Tests.Pipeline;

public sealed class SubtitleTranslationLoopTests
{
    [Fact]
    public async Task TranslatesStableChangedTextAndShowsOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.Equal(1, translator.CallCount);
    }

    [Fact]
    public async Task UsesCacheForRepeatedStableText()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello", "Hello", "Hello");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal(1, translator.CallCount);
        Assert.Equal("ZH: Hello", overlay.LastText);
    }

    [Fact]
    public async Task PauseSkipsCaptureAndHidesOverlay()
    {
        var capture = new FakeCapture();
        var overlay = new FakeOverlay();
        var controller = new TranslatorController();
        controller.TogglePause();
        var loop = CreateLoop(capture, new FakeOcr("Hello"), new FakeTranslator(), overlay, controller);

        await loop.TickAsync(CancellationToken.None);

        Assert.Equal(0, capture.CaptureCount);
        Assert.True(overlay.WasHidden);
    }

    [Fact]
    public async Task OneShotTranslatesOnceWhilePaused()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Inspect");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var controller = new TranslatorController();
        controller.TogglePause();
        var loop = CreateLoop(capture, ocr, translator, overlay, controller);

        await loop.RunOneShotAsync(CancellationToken.None);

        Assert.Equal(1, capture.CaptureCount);
        Assert.Equal(1, translator.CallCount);
        Assert.Equal("ZH: Inspect", overlay.LastText);
        Assert.Equal(TranslatorRunState.Paused, controller.State);
    }

    private static SubtitleTranslationLoop CreateLoop(
        IRegionCapture capture,
        IOcrEngine ocr,
        ITranslationProvider translator,
        IOverlayPresenter overlay,
        TranslatorController? controller = null)
    {
        return new SubtitleTranslationLoop(
            controller ?? new TranslatorController(),
            capture,
            new AlwaysChangedDetector(),
            ocr,
            translator,
            overlay,
            new TranslationCache(),
            new TranslatorOptions { RequiredStableRepeats = 2 });
    }

    private sealed class FakeCapture : IRegionCapture
    {
        public int CaptureCount { get; private set; }
        public FrameRegion Region { get; } = new(10, 20, 300, 90);

        public Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken)
        {
            CaptureCount++;
            return Task.FromResult(new CapturedFrame(Region, DateTimeOffset.UtcNow, [1, 2, 3]));
        }
    }

    private sealed class AlwaysChangedDetector : IFrameChangeDetector
    {
        public bool HasMeaningfulChange(CapturedFrame frame) => true;
    }

    private sealed class FakeOcr(params string[] results) : IOcrEngine
    {
        private int _index;

        public Task<string> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
        {
            var result = results[Math.Min(_index, results.Length - 1)];
            _index++;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeTranslator : ITranslationProvider
    {
        public int CallCount { get; private set; }

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult($"ZH: {sourceText}");
        }
    }

    private sealed class FakeOverlay : IOverlayPresenter
    {
        public string? LastText { get; private set; }
        public bool WasHidden { get; private set; }

        public Task ShowAsync(string translatedText, CancellationToken cancellationToken)
        {
            LastText = translatedText;
            WasHidden = false;
            return Task.CompletedTask;
        }

        public Task HideAsync(CancellationToken cancellationToken)
        {
            WasHidden = true;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter SubtitleTranslationLoopTests
```

Expected: test run fails because pipeline abstractions do not exist.

- [ ] **Step 3: Add abstractions and options**

Create `src/PearTranslator.Core/Abstractions/FrameRegion.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public readonly record struct FrameRegion(int X, int Y, int Width, int Height);
```

Create `src/PearTranslator.Core/Abstractions/CapturedFrame.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public sealed record CapturedFrame(FrameRegion Region, DateTimeOffset CapturedAt, byte[] Fingerprint);
```

Create `src/PearTranslator.Core/Abstractions/IRegionCapture.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public interface IRegionCapture
{
    Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken);
}
```

Create `src/PearTranslator.Core/Abstractions/IFrameChangeDetector.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public interface IFrameChangeDetector
{
    bool HasMeaningfulChange(CapturedFrame frame);
}
```

Create `src/PearTranslator.Core/Abstractions/IOcrEngine.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public interface IOcrEngine
{
    Task<string> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken);
}
```

Create `src/PearTranslator.Core/Abstractions/ITranslationProvider.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public interface ITranslationProvider
{
    Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken);
}
```

Create `src/PearTranslator.Core/Abstractions/IOverlayPresenter.cs`:

```csharp
namespace PearTranslator.Core.Abstractions;

public interface IOverlayPresenter
{
    Task ShowAsync(string translatedText, CancellationToken cancellationToken);

    Task HideAsync(CancellationToken cancellationToken);
}
```

Create `src/PearTranslator.Core/Configuration/TranslatorOptions.cs`:

```csharp
namespace PearTranslator.Core.Configuration;

public sealed class TranslatorOptions
{
    public int RequiredStableRepeats { get; init; } = 2;

    public TimeSpan SamplingInterval { get; init; } = TimeSpan.FromMilliseconds(500);
}
```

- [ ] **Step 4: Add runtime loop**

Create `src/PearTranslator.Core/Pipeline/SubtitleTranslationLoop.cs`:

```csharp
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Text;
using PearTranslator.Core.Translation;

namespace PearTranslator.Core.Pipeline;

public sealed class SubtitleTranslationLoop
{
    private readonly TranslatorController _controller;
    private readonly IRegionCapture _capture;
    private readonly IFrameChangeDetector _frameChangeDetector;
    private readonly IOcrEngine _ocrEngine;
    private readonly ITranslationProvider _translationProvider;
    private readonly IOverlayPresenter _overlayPresenter;
    private readonly TranslationCache _translationCache;
    private readonly TextStabilizer _textStabilizer;

    public SubtitleTranslationLoop(
        TranslatorController controller,
        IRegionCapture capture,
        IFrameChangeDetector frameChangeDetector,
        IOcrEngine ocrEngine,
        ITranslationProvider translationProvider,
        IOverlayPresenter overlayPresenter,
        TranslationCache translationCache,
        TranslatorOptions options)
    {
        _controller = controller;
        _capture = capture;
        _frameChangeDetector = frameChangeDetector;
        _ocrEngine = ocrEngine;
        _translationProvider = translationProvider;
        _overlayPresenter = overlayPresenter;
        _translationCache = translationCache;
        _textStabilizer = new TextStabilizer(options.RequiredStableRepeats);
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        if (!_controller.ShouldCapture)
        {
            await _overlayPresenter.HideAsync(cancellationToken);
            return;
        }

        var frame = await _capture.CaptureAsync(cancellationToken);
        if (!_frameChangeDetector.HasMeaningfulChange(frame))
        {
            return;
        }

        var recognized = await _ocrEngine.RecognizeAsync(frame, cancellationToken);
        var stableText = _textStabilizer.Observe(recognized);
        if (stableText is null)
        {
            return;
        }

        _controller.AcceptStableSourceText(stableText);
        if (!_controller.ShouldShowOverlay)
        {
            await _overlayPresenter.HideAsync(cancellationToken);
            return;
        }

        if (!_translationCache.TryGet(stableText, out var translated))
        {
            translated = await _translationProvider.TranslateAsync(stableText, cancellationToken);
            _translationCache.Store(stableText, translated);
        }

        await _overlayPresenter.ShowAsync(translated, cancellationToken);
    }

    public async Task RunOneShotAsync(CancellationToken cancellationToken)
    {
        var frame = await _capture.CaptureAsync(cancellationToken);
        var recognized = await _ocrEngine.RecognizeAsync(frame, cancellationToken);
        var normalized = TextNormalizer.Normalize(recognized);
        if (normalized.Length == 0)
        {
            await _overlayPresenter.HideAsync(cancellationToken);
            return;
        }

        if (!_translationCache.TryGet(normalized, out var translated))
        {
            translated = await _translationProvider.TranslateAsync(normalized, cancellationToken);
            _translationCache.Store(normalized, translated);
        }

        await _overlayPresenter.ShowAsync(translated, cancellationToken);
    }
}
```

- [ ] **Step 5: Run loop tests**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj --filter SubtitleTranslationLoopTests
```

Expected: tests pass.

- [ ] **Step 6: Commit runtime loop**

Run:

```powershell
git add src/PearTranslator.Core/Abstractions src/PearTranslator.Core/Configuration src/PearTranslator.Core/Pipeline tests/PearTranslator.Core.Tests/Pipeline
git commit -m "feat: add subtitle translation loop"
```

Expected: commit succeeds when Git is installed.

## Task 6: WPF Shell Visual Foundation

**Files:**
- Modify: `src/PearTranslator.App.Wpf/App.xaml`
- Modify: `src/PearTranslator.App.Wpf/App.xaml.cs`
- Modify: `src/PearTranslator.App.Wpf/MainWindow.xaml`
- Modify: `src/PearTranslator.App.Wpf/MainWindow.xaml.cs`

- [ ] **Step 1: Replace app resources**

Replace `src/PearTranslator.App.Wpf/App.xaml`:

```xml
<Application x:Class="PearTranslator.App.Wpf.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources>
    <Color x:Key="WindowChromeColor">#F7F7F9</Color>
    <Color x:Key="PanelColor">#EFFFFFFF</Color>
    <Color x:Key="TextPrimaryColor">#202124</Color>
    <Color x:Key="TextSecondaryColor">#666A73</Color>
    <SolidColorBrush x:Key="WindowChromeBrush" Color="{StaticResource WindowChromeColor}" />
    <SolidColorBrush x:Key="PanelBrush" Color="{StaticResource PanelColor}" />
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}" />

    <Style TargetType="Button">
      <Setter Property="MinHeight" Value="34" />
      <Setter Property="Padding" Value="14,6" />
      <Setter Property="BorderThickness" Value="0" />
      <Setter Property="Background" Value="#1F1F22" />
      <Setter Property="Foreground" Value="White" />
      <Setter Property="FontWeight" Value="SemiBold" />
      <Setter Property="Cursor" Value="Hand" />
    </Style>
  </Application.Resources>
</Application>
```

- [ ] **Step 2: Replace main window layout**

Replace `src/PearTranslator.App.Wpf/MainWindow.xaml`:

```xml
<Window x:Class="PearTranslator.App.Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="PearTranslator"
        Width="520"
        Height="360"
        MinWidth="460"
        MinHeight="320"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="CanResizeWithGrip">
  <Border Margin="12"
          Background="{StaticResource WindowChromeBrush}"
          CornerRadius="22"
          SnapsToDevicePixels="True">
    <Border.Effect>
      <DropShadowEffect BlurRadius="26"
                        Direction="270"
                        ShadowDepth="4"
                        Opacity="0.18" />
    </Border.Effect>
    <Grid Margin="22">
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
      </Grid.RowDefinitions>

      <DockPanel MouseLeftButtonDown="OnDragWindow">
        <StackPanel>
          <TextBlock Text="PearTranslator"
                     FontSize="22"
                     FontWeight="SemiBold"
                     Foreground="{StaticResource TextPrimaryBrush}" />
          <TextBlock Text="Real-time subtitle translation for windowed games"
                     Margin="0,4,0,0"
                     FontSize="13"
                     Foreground="{StaticResource TextSecondaryBrush}" />
        </StackPanel>
        <Button DockPanel.Dock="Right"
                Width="34"
                Height="34"
                Padding="0"
                Content="X"
                Click="OnCloseClicked" />
      </DockPanel>

      <Border Grid.Row="1"
              Margin="0,24"
              Padding="18"
              CornerRadius="16"
              Background="{StaticResource PanelBrush}">
        <StackPanel>
          <TextBlock x:Name="StatusText"
                     Text="Ready. Select a subtitle region to begin."
                     FontSize="16"
                     FontWeight="SemiBold"
                     Foreground="{StaticResource TextPrimaryBrush}" />
          <TextBlock Margin="0,8,0,0"
                     Text="Ctrl+Alt+R: region  |  Ctrl+Alt+T: pause  |  Ctrl+Alt+X: dismiss  |  Ctrl+Alt+S: one-shot"
                     FontSize="12"
                     Foreground="{StaticResource TextSecondaryBrush}" />
        </StackPanel>
      </Border>

      <StackPanel Grid.Row="2"
                  Orientation="Horizontal"
                  HorizontalAlignment="Right">
        <Button Content="Select Region"
                Margin="0,0,10,0"
                Click="OnSelectRegionClicked" />
        <Button Content="One Shot"
                Margin="0,0,10,0"
                Click="OnOneShotClicked" />
        <Button Content="Pause"
                Click="OnPauseClicked" />
      </StackPanel>
    </Grid>
  </Border>
</Window>
```

- [ ] **Step 3: Replace main window code-behind**

Replace `src/PearTranslator.App.Wpf/MainWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Input;

namespace PearTranslator.App.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnSelectRegionClicked(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Region selection will open in the next task.";
    }

    private void OnPauseClicked(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Pause control will be wired in the composition task.";
    }

    private void OnOneShotClicked(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "One-shot translation will be wired in the composition task.";
    }
}
```

- [ ] **Step 4: Build WPF app**

Run:

```powershell
dotnet build src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit shell UI**

Run:

```powershell
git add src/PearTranslator.App.Wpf/App.xaml src/PearTranslator.App.Wpf/App.xaml.cs src/PearTranslator.App.Wpf/MainWindow.xaml src/PearTranslator.App.Wpf/MainWindow.xaml.cs
git commit -m "feat: add polished WPF shell"
```

Expected: commit succeeds when Git is installed.

## Task 7: Overlay Window And Region Selection Window

**Files:**
- Create: `src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml`
- Create: `src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml.cs`
- Create: `src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml`
- Create: `src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml.cs`

- [ ] **Step 1: Add overlay XAML**

Create `src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml`:

```xml
<Window x:Class="PearTranslator.App.Wpf.Overlay.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Width="720"
        Height="120"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True"
        ResizeMode="NoResize">
  <Border x:Name="CaptionHost"
          Background="#B8202024"
          CornerRadius="18"
          Padding="22,14">
    <TextBlock x:Name="CaptionText"
               Text=""
               TextWrapping="Wrap"
               TextAlignment="Center"
               FontSize="28"
               FontWeight="SemiBold"
               Foreground="White">
      <TextBlock.Effect>
        <DropShadowEffect BlurRadius="8"
                          ShadowDepth="1"
                          Opacity="0.65" />
      </TextBlock.Effect>
    </TextBlock>
  </Border>
</Window>
```

- [ ] **Step 2: Add overlay code-behind**

Create `src/PearTranslator.App.Wpf/Overlay/OverlayWindow.xaml.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Overlay;

public partial class OverlayWindow : Window, IOverlayPresenter
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => EnableClickThrough();
    }

    public Task ShowAsync(string translatedText, CancellationToken cancellationToken)
    {
        Dispatcher.Invoke(() =>
        {
            CaptionText.Text = translatedText;
            if (!IsVisible)
            {
                Show();
            }
        });

        return Task.CompletedTask;
    }

    public Task HideAsync(CancellationToken cancellationToken)
    {
        Dispatcher.Invoke(Hide);
        return Task.CompletedTask;
    }

    private void EnableClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var currentStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, currentStyle | WsExTransparent | WsExToolwindow);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
```

- [ ] **Step 3: Add region selector XAML**

Create `src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml`:

```xml
<Window x:Class="PearTranslator.App.Wpf.RegionSelection.RegionSelectionWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="#33000000"
        ShowInTaskbar="False"
        Topmost="True"
        WindowState="Maximized"
        Cursor="Cross">
  <Canvas x:Name="SelectionCanvas"
          MouseLeftButtonDown="OnMouseDown"
          MouseMove="OnMouseMove"
          MouseLeftButtonUp="OnMouseUp">
    <Rectangle x:Name="SelectionRectangle"
               Stroke="#FFFFFFFF"
               StrokeThickness="2"
               Fill="#22FFFFFF"
               Visibility="Collapsed" />
  </Canvas>
</Window>
```

- [ ] **Step 4: Add region selector code-behind**

Create `src/PearTranslator.App.Wpf/RegionSelection/RegionSelectionWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.RegionSelection;

public partial class RegionSelectionWindow : Window
{
    private Point? _start;

    public RegionSelectionWindow()
    {
        InitializeComponent();
    }

    public FrameRegion? SelectedRegion { get; private set; }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(SelectionCanvas);
        SelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRectangle, _start.Value.X);
        Canvas.SetTop(SelectionRectangle, _start.Value.Y);
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_start is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(SelectionCanvas);
        var left = Math.Min(_start.Value.X, current.X);
        var top = Math.Min(_start.Value.Y, current.Y);
        var width = Math.Abs(current.X - _start.Value.X);
        var height = Math.Abs(current.Y - _start.Value.Y);

        Canvas.SetLeft(SelectionRectangle, left);
        Canvas.SetTop(SelectionRectangle, top);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_start is null)
        {
            return;
        }

        ReleaseMouseCapture();
        var current = e.GetPosition(SelectionCanvas);
        var left = (int)Math.Round(Math.Min(_start.Value.X, current.X));
        var top = (int)Math.Round(Math.Min(_start.Value.Y, current.Y));
        var width = (int)Math.Round(Math.Abs(current.X - _start.Value.X));
        var height = (int)Math.Round(Math.Abs(current.Y - _start.Value.Y));

        if (width >= 30 && height >= 20)
        {
            SelectedRegion = new FrameRegion(left, top, width, height);
            DialogResult = true;
        }
        else
        {
            DialogResult = false;
        }

        Close();
    }
}
```

- [ ] **Step 5: Build WPF app**

Run:

```powershell
dotnet build src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected: build succeeds.

- [ ] **Step 6: Commit overlay and selector**

Run:

```powershell
git add src/PearTranslator.App.Wpf/Overlay src/PearTranslator.App.Wpf/RegionSelection
git commit -m "feat: add overlay and region selector"
```

Expected: commit succeeds when Git is installed.

## Task 8: Mock Runtime Dependencies For End-To-End UI

**Files:**
- Create: `src/PearTranslator.App.Wpf/Mocks/MockRegionCapture.cs`
- Create: `src/PearTranslator.App.Wpf/Mocks/MockOcrEngine.cs`
- Create: `src/PearTranslator.App.Wpf/Mocks/MockTranslationProvider.cs`

- [ ] **Step 1: Add mock capture**

Create `src/PearTranslator.App.Wpf/Mocks/MockRegionCapture.cs`:

```csharp
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockRegionCapture : IRegionCapture
{
    private FrameRegion? _region;
    private int _frameNumber;

    public bool HasRegion => _region.HasValue;

    public void SetRegion(FrameRegion region)
    {
        _region = region;
    }

    public Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        if (_region is not { } region)
        {
            throw new InvalidOperationException("A subtitle region must be selected before capture starts.");
        }

        _frameNumber++;
        return Task.FromResult(new CapturedFrame(region, DateTimeOffset.UtcNow, BitConverter.GetBytes(_frameNumber)));
    }
}
```

- [ ] **Step 2: Add mock OCR**

Create `src/PearTranslator.App.Wpf/Mocks/MockOcrEngine.cs`:

```csharp
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockOcrEngine : IOcrEngine
{
    private readonly string[] _samples =
    [
        "Welcome back",
        "Welcome back",
        "Open the ancient gate",
        "Open the ancient gate",
        "We must leave before sunrise",
        "We must leave before sunrise"
    ];

    private int _index;

    public Task<string> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        var text = _samples[_index % _samples.Length];
        _index++;
        return Task.FromResult(text);
    }
}
```

- [ ] **Step 3: Add mock translation**

Create `src/PearTranslator.App.Wpf/Mocks/MockTranslationProvider.cs`:

```csharp
using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockTranslationProvider : ITranslationProvider
{
    public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        var translation = sourceText switch
        {
            "Welcome back" => "欢迎回来",
            "Open the ancient gate" => "打开古老的大门",
            "We must leave before sunrise" => "我们必须在日出前离开",
            _ => $"译文：{sourceText}"
        };

        return Task.FromResult(translation);
    }
}
```

- [ ] **Step 4: Build app**

Run:

```powershell
dotnet build src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit mock dependencies**

Run:

```powershell
git add src/PearTranslator.App.Wpf/Mocks
git commit -m "feat: add mock translation dependencies"
```

Expected: commit succeeds when Git is installed.

## Task 9: Hotkeys And Tray Integration

**Files:**
- Create: `src/PearTranslator.App.Wpf/Hotkeys/GlobalHotkeyService.cs`
- Create: `src/PearTranslator.App.Wpf/Tray/TrayIconService.cs`

- [ ] **Step 1: Add global hotkey service**

Create `src/PearTranslator.App.Wpf/Hotkeys/GlobalHotkeyService.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PearTranslator.App.Wpf.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private readonly Window _window;
    private HwndSource? _source;

    public GlobalHotkeyService(Window window)
    {
        _window = window;
        _window.SourceInitialized += OnSourceInitialized;
    }

    public event EventHandler? SelectRegionPressed;
    public event EventHandler? PausePressed;
    public event EventHandler? DismissPressed;
    public event EventHandler? OneShotPressed;

    public void Dispose()
    {
        if (_source is not null)
        {
            var handle = new WindowInteropHelper(_window).Handle;
            UnregisterHotKey(handle, 1);
            UnregisterHotKey(handle, 2);
            UnregisterHotKey(handle, 3);
            UnregisterHotKey(handle, 4);
            _source.RemoveHook(OnWindowMessage);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(OnWindowMessage);

        RegisterHotKey(handle, 1, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.R));
        RegisterHotKey(handle, 2, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.T));
        RegisterHotKey(handle, 3, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.X));
        RegisterHotKey(handle, 4, ModControl | ModAlt, (uint)KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.S));
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        handled = true;
        var id = wParam.ToInt32();
        if (id == 1)
        {
            SelectRegionPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (id == 2)
        {
            PausePressed?.Invoke(this, EventArgs.Empty);
        }
        else if (id == 3)
        {
            DismissPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (id == 4)
        {
            OneShotPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
```

- [ ] **Step 2: Add tray service**

Create `src/PearTranslator.App.Wpf/Tray/TrayIconService.cs`:

```csharp
using System.Windows;
using Forms = System.Windows.Forms;

namespace PearTranslator.App.Wpf.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "PearTranslator",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());
        return menu;
    }

    private void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.Activate();
    }
}
```

- [ ] **Step 3: Build app**

Run:

```powershell
dotnet build src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected: build succeeds.

- [ ] **Step 4: Commit hotkeys and tray**

Run:

```powershell
git add src/PearTranslator.App.Wpf/Hotkeys src/PearTranslator.App.Wpf/Tray
git commit -m "feat: add hotkeys and tray integration"
```

Expected: commit succeeds when Git is installed.

## Task 10: Wire WPF Composition To Core Loop

**Files:**
- Create: `src/PearTranslator.App.Wpf/CompositionRoot.cs`
- Modify: `src/PearTranslator.App.Wpf/MainWindow.xaml.cs`
- Modify: `src/PearTranslator.App.Wpf/App.xaml.cs`

- [ ] **Step 1: Add composition root**

Create `src/PearTranslator.App.Wpf/CompositionRoot.cs`:

```csharp
using System.Windows.Threading;
using PearTranslator.App.Wpf.Mocks;
using PearTranslator.App.Wpf.Overlay;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Pipeline;
using PearTranslator.Core.Translation;

namespace PearTranslator.App.Wpf;

public sealed class CompositionRoot
{
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _shutdown = new();
    private bool _isTicking;

    public CompositionRoot(OverlayWindow overlayWindow)
    {
        Controller = new TranslatorController();
        Capture = new MockRegionCapture();

        var options = new TranslatorOptions();
        Loop = new SubtitleTranslationLoop(
            Controller,
            Capture,
            new AlwaysChangedFrameDetector(),
            new MockOcrEngine(),
            new MockTranslationProvider(),
            overlayWindow,
            new TranslationCache(),
            options);

        _timer = new DispatcherTimer { Interval = options.SamplingInterval };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public TranslatorController Controller { get; }

    public MockRegionCapture Capture { get; }

    public SubtitleTranslationLoop Loop { get; }

    public void Start() => _timer.Start();

    public void Stop()
    {
        _timer.Stop();
        _shutdown.Cancel();
    }

    private async Task TickAsync()
    {
        if (_isTicking)
        {
            return;
        }

        _isTicking = true;
        try
        {
            await Loop.TickAsync(_shutdown.Token);
        }
        finally
        {
            _isTicking = false;
        }
    }

    private sealed class AlwaysChangedFrameDetector : IFrameChangeDetector
    {
        public bool HasMeaningfulChange(CapturedFrame frame) => true;
    }
}
```

- [ ] **Step 2: Replace main window code-behind**

Replace `src/PearTranslator.App.Wpf/MainWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Input;
using PearTranslator.App.Wpf.Hotkeys;
using PearTranslator.App.Wpf.Overlay;
using PearTranslator.App.Wpf.RegionSelection;
using PearTranslator.App.Wpf.Tray;

namespace PearTranslator.App.Wpf;

public partial class MainWindow : Window
{
    private readonly OverlayWindow _overlayWindow;
    private readonly CompositionRoot _composition;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly TrayIconService _tray;

    public MainWindow()
    {
        InitializeComponent();

        _overlayWindow = new OverlayWindow();
        _composition = new CompositionRoot(_overlayWindow);
        _hotkeys = new GlobalHotkeyService(this);
        _tray = new TrayIconService(this);

        _hotkeys.SelectRegionPressed += (_, _) => SelectRegion();
        _hotkeys.PausePressed += (_, _) => TogglePause();
        _hotkeys.DismissPressed += (_, _) => DismissCurrent();
        _hotkeys.OneShotPressed += async (_, _) => await RunOneShotAsync();

        Closed += (_, _) =>
        {
            _composition.Stop();
            _hotkeys.Dispose();
            _tray.Dispose();
            _overlayWindow.Close();
        };
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnSelectRegionClicked(object sender, RoutedEventArgs e)
    {
        SelectRegion();
    }

    private void OnPauseClicked(object sender, RoutedEventArgs e)
    {
        TogglePause();
    }

    private async void OnOneShotClicked(object sender, RoutedEventArgs e)
    {
        await RunOneShotAsync();
    }

    private void SelectRegion()
    {
        var selector = new RegionSelectionWindow { Owner = this };
        if (selector.ShowDialog() == true && selector.SelectedRegion is { } region)
        {
            _composition.Capture.SetRegion(region);
            _composition.Start();
            StatusText.Text = $"Region selected: {region.Width}x{region.Height}";
        }
    }

    private void TogglePause()
    {
        _composition.Controller.TogglePause();
        StatusText.Text = _composition.Controller.ShouldCapture ? "Running." : "Paused.";
    }

    private void DismissCurrent()
    {
        _composition.Controller.DismissCurrent();
        StatusText.Text = "Current subtitle dismissed.";
    }

    private async Task RunOneShotAsync()
    {
        if (!_composition.Capture.HasRegion)
        {
            StatusText.Text = "Select a subtitle region before one-shot translation.";
            return;
        }

        StatusText.Text = "Translating one screenshot...";
        await _composition.Loop.RunOneShotAsync(CancellationToken.None);
        StatusText.Text = "One-shot translation complete.";
    }
}
```

- [ ] **Step 3: Replace app shutdown behavior**

Replace `src/PearTranslator.App.Wpf/App.xaml.cs`:

```csharp
using System.Windows;

namespace PearTranslator.App.Wpf;

public partial class App : Application
{
}
```

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj
dotnet build src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected:

- Core tests pass.
- WPF app builds.

- [ ] **Step 5: Manually verify app behavior**

Run:

```powershell
dotnet run --project src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected:

- Main window opens with rounded Apple-like styling.
- Tray icon appears.
- `Ctrl+Alt+R` opens the region selector.
- Selecting a region updates status text.
- Overlay displays rotating mock Chinese subtitles.
- `Ctrl+Alt+T` pauses and resumes the loop.
- `Ctrl+Alt+X` hides the current subtitle until a different mock source appears.
- `Ctrl+Alt+S` runs one screenshot translation even when the continuous loop is paused.
- Closing the main window hides it instead of exiting; tray exit closes the app.

- [ ] **Step 6: Commit wired MVP slice**

Run:

```powershell
git add src/PearTranslator.App.Wpf
git commit -m "feat: wire WPF subtitle translator MVP"
```

Expected: commit succeeds when Git is installed.

## Task 11: Final Verification

**Files:**
- Read: `docs/superpowers/specs/2026-06-23-windows-game-subtitle-translator-design.md`
- Read: `docs/superpowers/plans/2026-06-23-windows-game-subtitle-translator-mvp.md`

- [ ] **Step 1: Run full automated verification**

Run:

```powershell
dotnet test tests/PearTranslator.Core.Tests/PearTranslator.Core.Tests.csproj
dotnet build PearTranslator.sln
```

Expected:

- Test run passes.
- Solution build succeeds.

- [ ] **Step 2: Run manual smoke test**

Run:

```powershell
dotnet run --project src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

Expected:

- Region selection, overlay, pause/resume, dismiss-current, one-shot screenshot translation, and tray exit all work with mock dependencies.

- [ ] **Step 3: Record known execution limits**

Add a short note to the final implementation report:

```text
Known limits: this slice uses mock OCR and mock translation, targets windowed/borderless Windows games, and does not guarantee exclusive fullscreen overlay behavior.
```

- [ ] **Step 4: Commit final verification note if a report file is created**

Run only if an implementation report file is added:

```powershell
git add docs
git commit -m "docs: record MVP verification"
```

Expected: commit succeeds when Git is installed.

## Self-Review

- Spec coverage: The plan implements the WPF shell, region selector, overlay, hotkeys, tray behavior, state machine, pause/resume, dismiss-current behavior, one-shot screenshot translation, mock OCR, mock translation, text stabilization, cache, and core tests from the approved design.
- Platform boundary: The plan remains Windows-only and does not include macOS, injection, exclusive fullscreen support, or whole-screen OCR.
- Type consistency: `TranslatorController`, `TranslatorRunState`, `TextStabilizer`, `TranslationCache`, `SubtitleTranslationLoop`, `CapturedFrame`, and `FrameRegion` names are consistent across tasks.
- Execution limit: implementation cannot start on the current machine until the .NET SDK is installed.
