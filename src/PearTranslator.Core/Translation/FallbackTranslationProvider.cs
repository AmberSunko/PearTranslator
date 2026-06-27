using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Translation;

public sealed class FallbackTranslationProvider : ITranslationProvider, ITranslationProviderMetadata
{
    private readonly IReadOnlyList<ITranslationProvider> _providers;
    private string _providerLabel = string.Empty;

    public FallbackTranslationProvider(IReadOnlyList<ITranslationProvider> providers)
    {
        _providers = providers;
    }

    public string ProviderLabel => _providerLabel;

    public async Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        foreach (var provider in _providers)
        {
            try
            {
                var translated = await provider.TranslateAsync(sourceText, cancellationToken);
                if (string.IsNullOrWhiteSpace(translated))
                {
                    continue;
                }

                _providerLabel = provider is ITranslationProviderMetadata metadata
                    ? metadata.ProviderLabel
                    : string.Empty;
                return translated;
            }
            catch (LocalDictionaryMissException exception)
            {
                lastException = exception;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        throw lastException ?? new InvalidOperationException("No translation provider returned a translation.");
    }
}
