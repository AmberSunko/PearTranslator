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
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.Equal("fake", overlay.LastProviderLabel);
    }

    [Fact]
    public async Task ReadsProviderLabelAfterTranslationCompletes()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new LateLabelTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.Equal("late", overlay.LastProviderLabel);
    }

    [Fact]
    public async Task PassesRecognizedTextHeightToOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr(
            new OcrResult("Hello", EstimatedTextHeightPixels: 34),
            new OcrResult("Hello", EstimatedTextHeightPixels: 34));
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.Equal(34, overlay.LastTextHeightPixels);
    }

    [Fact]
    public async Task PassesRecognizedTextBoundsToOverlay()
    {
        var capture = new FakeCapture();
        var textBounds = new FrameRegion(42, 18, 180, 32);
        var ocr = new FakeOcr(
            new OcrResult("Hello", TextBoundsPixels: textBounds),
            new OcrResult("Hello", TextBoundsPixels: textBounds));
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.Equal(textBounds, overlay.LastTextBoundsPixels);
        Assert.Equal("Hello", overlay.LastSourceText);
    }

    [Fact]
    public async Task PassesRecognizedTextLinesToOverlay()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 42, 130, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door\nTake the key", TextLines: textLines),
            new OcrResult("Open the door\nTake the key", TextLines: textLines));
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Open the door\nTake the key", overlay.LastText);
        Assert.Collection(
            overlay.LastSourceTextLines!,
            line => Assert.Equal(textLines[0], line),
            line => Assert.Equal(textLines[1], line));
    }

    [Fact]
    public async Task OcrPositionPreviewShowsRecognizedLinesWithoutCallingTranslator()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 42, 130, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door\nTake the key", TextLines: textLines),
            new OcrResult("Open the door\nTake the key", TextLines: textLines));
        var translator = new FailingTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { ShowOcrPositionOnly = true });

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal("Open the door\nTake the key", overlay.LastText);
        Assert.Equal("OCR", overlay.LastProviderLabel);
        Assert.Collection(
            overlay.LastSourceTextLines!,
            line => Assert.Equal(textLines[0], line),
            line => Assert.Equal(textLines[1], line));
    }

    [Fact]
    public async Task PositionOverlayUsesNumberedBatchTranslationToPreserveLineMapping()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 42, 130, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door\nTake the key", TextLines: textLines),
            new OcrResult("Open the door\nTake the key", TextLines: textLines));
        var translator = new NumberedBatchTranslator(
            """[{"id":2,"text":"line-two"},{"id":1,"text":"line-one"}]""");
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 2, AlignTranslationToOcrLines = true });

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal("line-one\nline-two", overlay.LastText);
        Assert.Single(translator.SourceTexts);
        Assert.Contains("JSON", translator.SourceTexts[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Open the door", translator.SourceTexts[0]);
        Assert.Contains("Take the key", translator.SourceTexts[0]);
        Assert.Collection(
            overlay.LastSourceTextLines!,
            line => Assert.Equal(textLines[0], line),
            line => Assert.Equal(textLines[1], line));
    }

    [Fact]
    public async Task ShowsLocalFirstWordPreviewBeforeModelTranslationCompletes()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 42, 130, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door\nTake the key", TextLines: textLines),
            new OcrResult("Open the door\nTake the key", TextLines: textLines));
        var translator = new DelayedBatchTranslator();
        var overlay = new FakeOverlay();
        var previewProvider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["open"] = "打开;开启",
                ["take"] = "拿取,带走",
            }));
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 2, AlignTranslationToOcrLines = true },
            localPreviewProvider: previewProvider);

        await loop.TryTickAsync(CancellationToken.None);
        var tickTask = loop.TryTickAsync(CancellationToken.None);
        await translator.WaitUntilCalledAsync();

        Assert.Single(overlay.ShowCalls);
        Assert.Equal("local", overlay.ShowCalls[0].ProviderLabel);
        Assert.Equal("打开\n拿取", overlay.ShowCalls[0].Text);

        translator.Complete("""[{"id":1,"text":"开门"},{"id":2,"text":"拿钥匙"}]""");
        var result = await tickTask;

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal(2, overlay.ShowCalls.Count);
        Assert.Equal("开门\n拿钥匙", overlay.ShowCalls[1].Text);
        Assert.Equal("batch", overlay.ShowCalls[1].ProviderLabel);
    }

    [Fact]
    public async Task ShowsLocalFirstWordPreviewBeforeTextStabilizes()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door", TextLines: textLines),
            new OcrResult("Open the door", TextLines: textLines));
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var previewProvider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["open"] = "OPEN_LOCAL",
            }));
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 2 },
            localPreviewProvider: previewProvider);

        var firstResult = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, firstResult.Outcome);
        Assert.Equal(0, translator.CallCount);
        Assert.Single(overlay.ShowCalls);
        Assert.Equal("local", overlay.ShowCalls[0].ProviderLabel);
        Assert.Equal("OPEN_LOCAL", overlay.ShowCalls[0].Text);
    }

    [Fact]
    public async Task DoesNotShowLocalFirstWordPreviewWhenDisabledBeforeTextStabilizes()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door", TextLines: textLines),
            new OcrResult("Open the door", TextLines: textLines));
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var previewProvider = new FirstWordLocalPreviewProvider(new LocalDictionaryTranslationProvider(
            new Dictionary<string, string>
            {
                ["open"] = "OPEN_LOCAL",
            }));
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions
            {
                RequiredStableRepeats = 2,
                LocalPreviewEnabled = false
            },
            localPreviewProvider: previewProvider);

        var firstResult = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, firstResult.Outcome);
        Assert.Equal(0, translator.CallCount);
        Assert.Empty(overlay.ShowCalls);
    }

    [Fact]
    public async Task PositionOverlayFallsBackToPerLineTranslationWhenNumberedBatchCannotBeParsed()
    {
        var capture = new FakeCapture();
        var textLines = new[]
        {
            new OcrTextLine("Open the door", new FrameRegion(10, 12, 140, 20)),
            new OcrTextLine("Take the key", new FrameRegion(10, 42, 130, 20))
        };
        var ocr = new FakeOcr(
            new OcrResult("Open the door\nTake the key", TextLines: textLines),
            new OcrResult("Open the door\nTake the key", TextLines: textLines));
        var translator = new NumberedBatchTranslator("not json");
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 2, AlignTranslationToOcrLines = true });

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal("line-one\nline-two", overlay.LastText);
        Assert.Equal(3, translator.SourceTexts.Count);
        Assert.Equal(3, result.Telemetry?.RequestCount);
        Assert.Contains("JSON", translator.SourceTexts[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Open the door", translator.SourceTexts[1]);
        Assert.Equal("Take the key", translator.SourceTexts[2]);
    }

    [Fact]
    public async Task PassesCapturedFrameRegionToOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TickAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal(capture.Region, overlay.LastAnchorRegion);
    }

    [Fact]
    public async Task KeepsOverlayVisibleBeforeRealtimeCapture()
    {
        var overlay = new FakeOverlay();
        var capture = new FakeCapture { ReadOverlayHidden = () => overlay.WasHidden };
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new LabeledFakeTranslator();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);

        Assert.False(capture.OverlayWasHiddenAtCapture);
    }

    [Fact]
    public async Task KeepsOverlayVisibleBeforeOneShotCapture()
    {
        var overlay = new FakeOverlay();
        var capture = new FakeCapture { ReadOverlayHidden = () => overlay.WasHidden };
        var ocr = new FakeOcr("Inspect");
        var translator = new FakeTranslator();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryRunOneShotAsync(CancellationToken.None);

        Assert.False(capture.OverlayWasHiddenAtCapture);
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
    public async Task StabilizesUnchangedFrameByReusingLastOcrResult()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello");
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            frameChangeDetector: new SequenceChangedDetector(true, false));

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal(1, ocr.CallCount);
        Assert.Equal("ZH: Hello", overlay.LastText);
    }

    [Fact]
    public async Task SkipsTranslationForChineseSourceTextWithoutHidingRealtimeOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("你好，世界", "你好，世界");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Equal("未检测到英文，未请求翻译服务", result.ConnectionStatusMessage);
        Assert.Equal(0, translator.CallCount);
        Assert.False(overlay.WasHidden);
    }

    [Fact]
    public async Task KeepsVisibleRealtimeOverlayWhenSingleNonEnglishFrameLooksLikeCapturedOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "你好，世界");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 1 });

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.False(overlay.RealtimeWasHidden);
        Assert.Equal(1, translator.CallCount);
    }

    [Fact]
    public async Task KeepsVisibleRealtimeOverlayAfterRepeatedNonEnglishFrames()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "你好，世界", "你好，世界");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 1 });

        await loop.TryTickAsync(CancellationToken.None);
        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.False(overlay.RealtimeWasHidden);
        Assert.Equal(1, translator.CallCount);
    }

    [Fact]
    public async Task KeepsVisibleRealtimeOverlayWhenRepeatedNonEnglishFramesMatchCurrentTranslation()
    {
        var translated = "\u5df2\u7ffb\u8bd1";
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", translated, translated, translated);
        var translator = new LiteralTranslator(translated);
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 1 });

        await loop.TryTickAsync(CancellationToken.None);
        await loop.TryTickAsync(CancellationToken.None);
        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Equal(translated, overlay.LastText);
        Assert.False(overlay.RealtimeWasHidden);
        Assert.Equal(1, translator.CallCount);
    }

    [Fact]
    public async Task DoesNotRetranslateRecognizedTextFromVisibleRealtimeOverlay()
    {
        var translated = "\u5df2\u7ffb\u8bd1";
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", $"{translated}\nDeepSeek / deepseek-v4-flash");
        var translator = new LiteralTranslator(translated, "DeepSeek / deepseek-v4-flash");
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions { RequiredStableRepeats = 1 });

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Equal(translated, overlay.LastText);
        Assert.False(overlay.RealtimeWasHidden);
        Assert.Equal(1, translator.CallCount);
    }

    [Fact]
    public async Task RemovesChineseSourceTextBeforeTranslatingMixedText()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("按 E Open the door", "按 E Open the door");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal("E Open the door", translator.LastSourceText);
        Assert.Equal("ZH: E Open the door", overlay.LastText);
    }

    [Fact]
    public async Task PreservesLineBreaksWhenTranslatingMultilineText()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Open the door\r\nTake the key", "Open the door\nTake   the key");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal("Open the door\nTake the key", translator.LastSourceText);
        Assert.Equal("ZH: Open the door\nTake the key", overlay.LastText);
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

    [Fact]
    public async Task PausedRealtimeTickDoesNotHideOneShotOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Inspect");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var controller = new TranslatorController();
        controller.TogglePause();
        var loop = CreateLoop(capture, ocr, translator, overlay, controller);

        await loop.RunOneShotAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);

        Assert.Equal("ZH: Inspect", overlay.LastText);
        Assert.True(overlay.RealtimeWasHidden);
        Assert.False(overlay.OneShotWasHidden);
        Assert.Equal(1, capture.CaptureCount);
    }

    [Fact]
    public async Task OneShotWithProvidedFrameDoesNotCaptureAndUsesFrameRegionForOverlay()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Inspect");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);
        var oneShotRegion = new FrameRegion(40, 50, 320, 110);
        var frame = new CapturedFrame(oneShotRegion, DateTimeOffset.UtcNow, [9, 8, 7]);

        var result = await loop.TryRunOneShotAsync(frame, CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.Equal(0, capture.CaptureCount);
        Assert.Equal(1, translator.CallCount);
        Assert.Equal("ZH: Inspect", overlay.LastText);
        Assert.Equal(oneShotRegion, overlay.LastAnchorRegion);
    }

    [Fact]
    public async Task TryTickReturnsFailureWithoutHidingRealtimeOverlayWhenTranslationFails()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, new FailingTranslator(), overlay);

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.Failed, result.Outcome);
        Assert.Contains("translation unavailable", result.ErrorMessage);
        Assert.False(overlay.WasHidden);
    }

    [Fact]
    public async Task TryTickReturnsTranslationTelemetryWhenOverlayIsDisplayed()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new LabeledFakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);
        var result = await loop.TryTickAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.DisplayedTranslation, result.Outcome);
        Assert.NotNull(result.Telemetry);
        Assert.Equal("fake", result.Telemetry.ProviderLabel);
        Assert.True(result.Telemetry.Latency >= TimeSpan.Zero);
        Assert.Equal(1, result.Telemetry.RequestCount);
        Assert.NotNull(result.Telemetry.Pipeline);
        Assert.True(result.Telemetry.Pipeline.Capture >= TimeSpan.Zero);
        Assert.True(result.Telemetry.Pipeline.Ocr >= TimeSpan.Zero);
        Assert.True(result.Telemetry.Pipeline.Stabilization >= TimeSpan.Zero);
        Assert.True(result.Telemetry.Pipeline.Translation >= TimeSpan.Zero);
        Assert.True(result.Telemetry.Pipeline.Overlay >= TimeSpan.Zero);
    }

    [Fact]
    public async Task RestoreRealtimeOverlayReplaysLastOverlayWithoutOcrOrTranslation()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Hello", "Hello");
        var translator = new FakeTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, translator, overlay);

        await loop.TryTickAsync(CancellationToken.None);
        await loop.TryTickAsync(CancellationToken.None);
        var ocrCallsBeforeRestore = ocr.CallCount;
        var translationCallsBeforeRestore = translator.CallCount;

        await loop.HideRealtimeOverlayAsync(CancellationToken.None);
        var restored = await loop.TryRestoreRealtimeOverlayAsync(CancellationToken.None);

        Assert.True(restored);
        Assert.Equal("ZH: Hello", overlay.LastText);
        Assert.False(overlay.RealtimeWasHidden);
        Assert.Equal(ocrCallsBeforeRestore, ocr.CallCount);
        Assert.Equal(translationCallsBeforeRestore, translator.CallCount);
    }

    [Fact]
    public async Task DoesNotShowStaleRealtimeOverlayWhenRegionChangesBeforeTranslationCompletes()
    {
        var originalRegion = new FrameRegion(10, 20, 300, 90);
        var nextRegion = new FrameRegion(220, 260, 420, 130);
        var capture = new FakeCapture { Region = originalRegion };
        var ocr = new FakeOcr("Hello");
        var translator = new DelayedBatchTranslator();
        var overlay = new FakeOverlay();
        var loop = CreateLoop(
            capture,
            ocr,
            translator,
            overlay,
            options: new TranslatorOptions
            {
                RequiredStableRepeats = 1,
                LocalPreviewEnabled = false
            });

        var tickTask = loop.TryTickAsync(CancellationToken.None);
        await translator.WaitUntilCalledAsync();
        capture.Region = nextRegion;
        translator.Complete("ZH: Hello");
        var result = await tickTask;

        Assert.Equal(TranslationLoopOutcome.NoChange, result.Outcome);
        Assert.Empty(overlay.ShowCalls);
        Assert.Null(overlay.LastAnchorRegion);
    }

    [Fact]
    public async Task TryOneShotReturnsFailureAndHidesOverlayWhenTranslationFails()
    {
        var capture = new FakeCapture();
        var ocr = new FakeOcr("Inspect");
        var overlay = new FakeOverlay();
        var loop = CreateLoop(capture, ocr, new FailingTranslator(), overlay);

        var result = await loop.TryRunOneShotAsync(CancellationToken.None);

        Assert.Equal(TranslationLoopOutcome.Failed, result.Outcome);
        Assert.Contains("translation unavailable", result.ErrorMessage);
        Assert.True(overlay.OneShotWasHidden);
        Assert.False(overlay.RealtimeWasHidden);
    }

    [Fact]
    public void DisposeReleasesDisposableOcrEngine()
    {
        var ocr = new DisposableFakeOcr();
        var loop = CreateLoop(new FakeCapture(), ocr, new FakeTranslator(), new FakeOverlay());

        loop.Dispose();

        Assert.True(ocr.WasDisposed);
    }

    private static SubtitleTranslationLoop CreateLoop(
        IRegionCapture capture,
        IOcrEngine ocr,
        ITranslationProvider translator,
        IOverlayPresenter overlay,
        TranslatorController? controller = null,
        IFrameChangeDetector? frameChangeDetector = null,
        TranslatorOptions? options = null,
        FirstWordLocalPreviewProvider? localPreviewProvider = null)
    {
        return new SubtitleTranslationLoop(
            controller ?? new TranslatorController(),
            capture,
            frameChangeDetector ?? new AlwaysChangedDetector(),
            ocr,
            translator,
            overlay,
            new TranslationCache(),
            options ?? new TranslatorOptions { RequiredStableRepeats = 2 },
            localPreviewProvider);
    }

    private sealed class FakeCapture : IRegionCapture
    {
        public int CaptureCount { get; private set; }
        public FrameRegion Region { get; set; } = new(10, 20, 300, 90);
        public FrameRegion? CurrentRegion => Region;
        public Func<bool>? ReadOverlayHidden { get; init; }
        public bool? OverlayWasHiddenAtCapture { get; private set; }

        public Task<CapturedFrame> CaptureAsync(CancellationToken cancellationToken)
        {
            OverlayWasHiddenAtCapture = ReadOverlayHidden?.Invoke();
            CaptureCount++;
            return Task.FromResult(new CapturedFrame(Region, DateTimeOffset.UtcNow, [1, 2, 3]));
        }
    }

    private sealed class AlwaysChangedDetector : IFrameChangeDetector
    {
        public bool HasMeaningfulChange(CapturedFrame frame) => true;
    }

    private sealed class SequenceChangedDetector(params bool[] results) : IFrameChangeDetector
    {
        private int _index;

        public bool HasMeaningfulChange(CapturedFrame frame)
        {
            var result = results[Math.Min(_index, results.Length - 1)];
            _index++;
            return result;
        }
    }

    private sealed class FakeOcr : IOcrEngine
    {
        private readonly OcrResult[] _results;
        private int _index;
        public int CallCount { get; private set; }

        public FakeOcr(params string[] results)
            : this(results.Select(result => new OcrResult(result)).ToArray())
        {
        }

        public FakeOcr(params OcrResult[] results)
        {
            _results = results;
        }

        public Task<OcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
        {
            CallCount++;
            var result = _results[Math.Min(_index, _results.Length - 1)];
            _index++;
            return Task.FromResult(result);
        }
    }

    private sealed class DisposableFakeOcr : IOcrEngine, IDisposable
    {
        public bool WasDisposed { get; private set; }

        public Task<OcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
        {
            return Task.FromResult(new OcrResult("Hello"));
        }

        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    private sealed class FakeTranslator : ITranslationProvider
    {
        public int CallCount { get; private set; }
        public string? LastSourceText { get; private set; }

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            CallCount++;
            LastSourceText = sourceText;
            return Task.FromResult($"ZH: {sourceText}");
        }
    }

    private sealed class LiteralTranslator(string translatedText, string providerLabel = "literal")
        : ITranslationProvider, ITranslationProviderMetadata
    {
        public int CallCount { get; private set; }

        public string ProviderLabel => providerLabel;

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(translatedText);
        }
    }

    private sealed class LabeledFakeTranslator : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel => "fake";

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            return Task.FromResult($"ZH: {sourceText}");
        }
    }

    private sealed class LateLabelTranslator : ITranslationProvider, ITranslationProviderMetadata
    {
        public string ProviderLabel { get; private set; } = string.Empty;

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            ProviderLabel = "late";
            return Task.FromResult($"ZH: {sourceText}");
        }
    }

    private sealed class FailingTranslator : ITranslationProvider
    {
        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("translation unavailable");
        }
    }

    private sealed class NumberedBatchTranslator(string batchResponse) : ITranslationProvider, ITranslationProviderMetadata
    {
        private readonly List<string> _sourceTexts = [];

        public string ProviderLabel => "batch";

        public IReadOnlyList<string> SourceTexts => _sourceTexts;

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            _sourceTexts.Add(sourceText);
            if (sourceText.Contains("JSON", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(batchResponse);
            }

            return Task.FromResult(sourceText switch
            {
                "Open the door" => "line-one",
                "Take the key" => "line-two",
                _ => "whole-text"
            });
        }
    }

    private sealed class DelayedBatchTranslator : ITranslationProvider, ITranslationProviderMetadata
    {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<string> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string ProviderLabel => "batch";

        public Task WaitUntilCalledAsync() => _called.Task;

        public void Complete(string response) => _response.TrySetResult(response);

        public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
        {
            _called.TrySetResult();
            return _response.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class FakeOverlay : IOverlayPresenter
    {
        private readonly List<OverlayShowCall> _showCalls = [];

        public string? LastText { get; private set; }
        public string? LastProviderLabel { get; private set; }
        public double? LastTextHeightPixels { get; private set; }
        public FrameRegion? LastAnchorRegion { get; private set; }
        public FrameRegion? LastTextBoundsPixels { get; private set; }
        public string? LastSourceText { get; private set; }
        public IReadOnlyList<OcrTextLine>? LastSourceTextLines { get; private set; }
        public IReadOnlyList<OverlayShowCall> ShowCalls => _showCalls;
        public bool WasHidden => RealtimeWasHidden;
        public bool RealtimeWasHidden { get; private set; }
        public bool OneShotWasHidden { get; private set; }

        public Task ShowAsync(string translatedText, CancellationToken cancellationToken)
        {
            return ShowAsync(translatedText, string.Empty, cancellationToken);
        }

        public Task ShowAsync(string translatedText, string providerLabel, CancellationToken cancellationToken)
        {
            return ShowAsync(translatedText, providerLabel, null, cancellationToken);
        }

        public Task ShowAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            CancellationToken cancellationToken)
        {
            return ShowAsync(translatedText, providerLabel, sourceTextHeightPixels, null, cancellationToken);
        }

        public Task ShowAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            FrameRegion? anchorRegion,
            CancellationToken cancellationToken)
        {
            return ShowAsync(
                translatedText,
                providerLabel,
                sourceTextHeightPixels,
                anchorRegion,
                null,
                cancellationToken);
        }

        public Task ShowAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            FrameRegion? anchorRegion,
            FrameRegion? sourceTextBoundsPixels,
            CancellationToken cancellationToken)
        {
            return ShowAsync(
                translatedText,
                providerLabel,
                sourceTextHeightPixels,
                anchorRegion,
                sourceTextBoundsPixels,
                null,
                cancellationToken);
        }

        public Task ShowAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            FrameRegion? anchorRegion,
            FrameRegion? sourceTextBoundsPixels,
            string? sourceText,
            CancellationToken cancellationToken)
        {
            LastText = translatedText;
            LastProviderLabel = providerLabel;
            LastTextHeightPixels = sourceTextHeightPixels;
            LastAnchorRegion = anchorRegion;
            LastTextBoundsPixels = sourceTextBoundsPixels;
            LastSourceText = sourceText;
            LastSourceTextLines = null;
            _showCalls.Add(new OverlayShowCall(translatedText, providerLabel, SourceTextLines: null));
            RealtimeWasHidden = false;
            return Task.CompletedTask;
        }

        public Task ShowAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            FrameRegion? anchorRegion,
            FrameRegion? sourceTextBoundsPixels,
            string? sourceText,
            IReadOnlyList<OcrTextLine>? sourceTextLines,
            CancellationToken cancellationToken)
        {
            LastText = translatedText;
            LastProviderLabel = providerLabel;
            LastTextHeightPixels = sourceTextHeightPixels;
            LastAnchorRegion = anchorRegion;
            LastTextBoundsPixels = sourceTextBoundsPixels;
            LastSourceText = sourceText;
            LastSourceTextLines = sourceTextLines;
            _showCalls.Add(new OverlayShowCall(translatedText, providerLabel, sourceTextLines));
            RealtimeWasHidden = false;
            return Task.CompletedTask;
        }

        public Task ShowOneShotAsync(
            string translatedText,
            string providerLabel,
            double? sourceTextHeightPixels,
            FrameRegion? anchorRegion,
            FrameRegion? sourceTextBoundsPixels,
            string? sourceText,
            IReadOnlyList<OcrTextLine>? sourceTextLines,
            CancellationToken cancellationToken)
        {
            LastText = translatedText;
            LastProviderLabel = providerLabel;
            LastTextHeightPixels = sourceTextHeightPixels;
            LastAnchorRegion = anchorRegion;
            LastTextBoundsPixels = sourceTextBoundsPixels;
            LastSourceText = sourceText;
            LastSourceTextLines = sourceTextLines;
            _showCalls.Add(new OverlayShowCall(translatedText, providerLabel, sourceTextLines));
            OneShotWasHidden = false;
            return Task.CompletedTask;
        }

        public Task HideAsync(CancellationToken cancellationToken)
        {
            RealtimeWasHidden = true;
            return Task.CompletedTask;
        }

        public Task HideOneShotAsync(CancellationToken cancellationToken)
        {
            OneShotWasHidden = true;
            return Task.CompletedTask;
        }
    }

    private sealed record OverlayShowCall(
        string Text,
        string ProviderLabel,
        IReadOnlyList<OcrTextLine>? SourceTextLines);
}
