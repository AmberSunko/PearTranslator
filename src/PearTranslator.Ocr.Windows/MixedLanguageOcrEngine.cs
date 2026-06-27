using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Ocr.Windows;

internal sealed class MixedLanguageOcrEngine : IOcrEngine, IDisposable
{
    private readonly IOcrEngine _primaryEngine;
    private readonly IOcrEngine _koreanEngine;

    private MixedLanguageOcrEngine(IOcrEngine primaryEngine, IOcrEngine koreanEngine)
    {
        _primaryEngine = primaryEngine;
        _koreanEngine = koreanEngine;
    }

    public static bool TryCreate(out IOcrEngine engine)
    {
        var hasPrimary = RapidOcrEngine.TryCreate(OcrLanguageKind.Auto, out var primaryEngine);
        var hasKorean = RapidOcrEngine.TryCreate(OcrLanguageKind.Korean, out var koreanEngine);

        if (hasPrimary && hasKorean)
        {
            engine = new MixedLanguageOcrEngine(primaryEngine, koreanEngine);
            return true;
        }

        if (hasPrimary)
        {
            engine = primaryEngine;
            return true;
        }

        if (hasKorean)
        {
            engine = koreanEngine;
            return true;
        }

        engine = null!;
        return false;
    }

    public async Task<OcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        var primaryTask = _primaryEngine.RecognizeAsync(frame, cancellationToken);
        var koreanTask = _koreanEngine.RecognizeAsync(frame, cancellationToken);

        try
        {
            await Task.WhenAll(primaryTask, koreanTask);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            if (primaryTask.Status == TaskStatus.RanToCompletion)
            {
                return primaryTask.Result;
            }

            if (koreanTask.Status == TaskStatus.RanToCompletion)
            {
                return koreanTask.Result;
            }

            throw;
        }

        return MixedLanguageOcrResultMerger.Merge(primaryTask.Result, koreanTask.Result);
    }

    public void Dispose()
    {
        (_primaryEngine as IDisposable)?.Dispose();
        (_koreanEngine as IDisposable)?.Dispose();
    }
}
