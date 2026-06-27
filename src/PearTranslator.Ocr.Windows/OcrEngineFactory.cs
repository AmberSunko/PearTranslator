using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Configuration;

namespace PearTranslator.Ocr.Windows;

public static class OcrEngineFactory
{
    public static IOcrEngine? TryCreate(OcrSettings settings)
    {
        if (settings.Engine == OcrEngineKind.LocalRapidOcr &&
            settings.Language == OcrLanguageKind.Auto &&
            MixedLanguageOcrEngine.TryCreate(out var mixedLanguageOcrEngine))
        {
            return mixedLanguageOcrEngine;
        }

        if (settings.Engine == OcrEngineKind.LocalRapidOcr &&
            RapidOcrEngine.TryCreate(settings.Language, out var rapidOcrEngine))
        {
            return rapidOcrEngine;
        }

        if (settings.Language == OcrLanguageKind.Auto)
        {
            return WindowsOcrEngine.TryCreateFromUserProfileLanguages()
                ?? WindowsOcrEngine.TryCreate();
        }

        return WindowsOcrEngine.TryCreate(ToWindowsLanguageTag(settings.Language))
            ?? WindowsOcrEngine.TryCreate();
    }

    private static string ToWindowsLanguageTag(OcrLanguageKind language)
    {
        return language switch
        {
            OcrLanguageKind.Chinese => "zh-CN",
            OcrLanguageKind.Japanese => "ja-JP",
            OcrLanguageKind.Korean => "ko-KR",
            _ => "en-US"
        };
    }
}
