using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Providers;

public sealed class OcrPreviewTranslationProvider : ITranslationProvider, ITranslationProviderMetadata
{
    public string ProviderLabel => "识别";

    public Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        return Task.FromResult(sourceText);
    }
}
