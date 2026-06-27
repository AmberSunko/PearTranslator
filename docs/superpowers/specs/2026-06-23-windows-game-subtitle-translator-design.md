# Windows Game Subtitle Translator Design

## Goal

Build a polished, high-performance Windows desktop app that translates English game subtitles in real time. The first version targets windowed and borderless-windowed Windows games, with an Apple-like visual style and a lightweight external overlay. The app must not inject into game processes.

## Non-Goals

- Exclusive fullscreen compatibility is not guaranteed in the first version.
- The app will not replace or erase original in-game subtitles.
- The app will not perform whole-screen OCR by default.
- macOS support is not included in the first version.
- Anti-cheat bypassing, render-pipeline hooks, and DLL injection are out of scope.

## Product Principles

- Beauty and performance come first.
- The app should feel like a refined desktop utility, not a developer tool.
- The translation overlay must stay readable without visually fighting the game.
- The first version should be genuinely usable, even if OCR and translation providers improve later.
- Core translation logic must stay separate from WPF so future platform work is not forced to rewrite everything.

## Target Platform

- OS: Windows 10/11.
- UI framework: WPF.
- Game mode: windowed or borderless window.
- Display setup: single monitor first, with a code boundary that allows multi-monitor support.

## Visual Direction

The main control surface uses an Apple-like style:

- Borderless rounded windows.
- Soft shadows and subtle translucent surfaces.
- Compact settings panels.
- Clear typography using Segoe UI Variable or Inter.
- Minimal color, restrained contrast, and calm spacing.
- Icon-led controls for actions such as pause, resume, dismiss, and reselect region.

The in-game overlay should be calmer than the settings UI:

- Transparent topmost window.
- Click-through when not being configured.
- Adjustable font size.
- Adjustable background opacity.
- Optional rounded translucent caption container.
- High-contrast text with shadow or outline for readability.

## MVP User Flow

1. User launches PearTranslator.
2. App starts as a tray-resident desktop utility.
3. User presses the region selection hotkey.
4. User draws a rectangle around the game's subtitle area.
5. App starts sampling only that region at a configurable interval.
6. App skips OCR when the sampled image has not changed enough.
7. OCR recognizes English subtitle text.
8. Text stabilization filters out transient OCR noise.
9. Translation provider translates stable text.
10. Translation cache prevents repeated requests for the same subtitle.
11. Overlay displays the translated Chinese subtitle.
12. User can pause/resume translation or dismiss the current subtitle with hotkeys.
13. User can trigger a one-shot screenshot translation without enabling continuous capture.

## Hotkeys

Default hotkeys:

- Ctrl+Alt+R: select or reselect subtitle region.
- Ctrl+Alt+T: pause or resume translation.
- Ctrl+Alt+X: dismiss the current translated subtitle.
- Ctrl+Alt+S: translate one screenshot of the selected region.

Hotkeys should be configurable later, but hardcoded defaults are acceptable for the first implementation pass.

## Translation Control State

The translator has three user-visible states:

- Running: capture, OCR, translation, and overlay updates are active.
- Paused: capture, OCR, and translation are stopped; overlay is hidden.
- Dismissed: the current overlay text is hidden, but capture continues and a new source subtitle can display a new translation.
- OneShot: a user-triggered single capture, OCR, translation, and overlay update. This does not enable the continuous capture loop.

State transitions:

- Running to Paused: user presses pause/resume.
- Paused to Running: user presses pause/resume again.
- Running to Dismissed: user presses dismiss current.
- Dismissed to Running: OCR detects a stable source text that differs from the dismissed text.
- Any state to OneShot: user presses one-shot screenshot translation.
- OneShot to previous state: one-shot translation completes, fails, or is cancelled.

When entering Paused, in-flight OCR and translation operations should be cancelled where possible.

One-shot screenshot translation should respect the selected region and translation cache. If no region is selected, it should prompt the user to select a region instead of starting continuous translation.

## Architecture

The solution is split into small projects with clear ownership:

```text
PearTranslator.App.Wpf
  WPF shell, tray, hotkeys, region selector, overlay, settings UI

PearTranslator.Core
  translation loop, state machine, text stabilization, cache, configuration models

PearTranslator.Capture.Windows
  Windows region capture implementations

PearTranslator.Ocr
  OCR interfaces and implementations

PearTranslator.Translate
  translation provider interfaces and implementations

PearTranslator.Tests
  focused tests for core state, caching, text stabilization, and provider boundaries
```

WPF owns presentation and Windows desktop interactions. Core owns behavior. OCR, capture, and translation are replaceable dependencies.

## Runtime Pipeline

```text
RegionSampler
  -> FrameChangeDetector
  -> OcrEngine
  -> TextStabilizer
  -> TranslationCache
  -> TranslationProvider
  -> OverlayPresenter
```

Pipeline rules:

- Sample only the selected subtitle region.
- Never run overlapping OCR jobs for the same region.
- Drop stale OCR or translation results after pause, region changes, or newer source text.
- Cache translations by normalized source text.
- Update the overlay only from stable source text.
- For one-shot translation, run a single capture/OCR/translation pass without requiring repeated OCR stabilization.

## Capture Strategy

The first implementation should start with the simplest reliable Windows region capture that works for windowed and borderless games. The capture layer must be abstracted so Windows Graphics Capture or DXGI Desktop Duplication can be introduced if the first implementation is not sufficient.

The capture module returns image frames for a selected screen rectangle and does not know about OCR, translation, or overlay rendering.

## OCR Strategy

The OCR layer exposes a simple interface that accepts a region image and returns recognized text with optional confidence data.

The first OCR implementation should prioritize easy local setup and acceptable English subtitle accuracy. Possible first candidates are Windows OCR or Tesseract. More advanced engines can replace this later without changing Core.

## Translation Strategy

The translation layer exposes provider implementations behind a common interface.

The first implementation may use a configurable provider, with an initial mock or local provider acceptable for development. Production providers can include OpenAI, DeepL, LibreTranslate, or another API-compatible service.

Provider concerns:

- Request cancellation.
- Timeout handling.
- API key configuration.
- Translation cache integration.
- Clear user-facing errors when translation fails.

## Performance Strategy

The first version targets smooth gameplay impact rather than maximum OCR throughput.

Performance techniques:

- Region-only capture.
- Configurable sampling interval, default around 500 ms.
- Frame difference detection before OCR.
- No overlapping OCR jobs.
- Source text normalization and deduplication.
- Translation cache.
- Pause state cancels or suppresses background work.
- One-shot translation bypasses the recurring timer and does not keep background OCR active after completion.

Expected latency budget:

- Capture: low milliseconds to tens of milliseconds.
- Image preprocessing: low milliseconds.
- OCR: dominant local cost.
- Translation: dominant network cost when using remote providers.
- Overlay update: low milliseconds.

## Error Handling

The app should fail softly:

- If no region is selected, show a compact prompt in the main panel or tray menu.
- If capture fails, pause the loop and show a recoverable status.
- If OCR fails, keep the last valid translation briefly or clear the overlay based on configuration.
- If translation fails, show a subtle error state outside the game overlay where possible.
- If a provider is misconfigured, prevent repeated failing calls.

## Testing Strategy

Core behavior should be testable without WPF:

- Translation state transitions.
- Pause/resume cancellation behavior.
- Dismiss-current behavior.
- Text stabilization.
- Translation cache hits and misses.
- Stale result suppression after region changes.

UI and capture behavior should be verified manually in the first version because game overlay behavior depends on local Windows display state and target applications.

## First Implementation Slice

The first usable slice should include:

- WPF shell window with Apple-like visual direction.
- Tray resident behavior.
- Region selection overlay.
- Translation overlay window.
- Hotkeys for reselect, pause/resume, and dismiss current.
- Hotkey for one-shot screenshot translation.
- Core translation loop with mock OCR and mock translation.
- Core one-shot translation path with mock OCR and mock translation.
- Unit tests for Core state behavior.

After that slice works, replace mock OCR and mock translation with real providers one at a time.
