using System.Diagnostics;
using PearTranslator.Core.Abstractions;

namespace PearTranslator.Core.Pipeline;

public static class TranslationConnectionProbe
{
    private const string WarmupText = "Hello";

    public static async Task<TranslationConnectionProbeResult> ProbeAsync(
        ITranslationProvider provider,
        CancellationToken cancellationToken)
    {
        var providerLabel = provider is ITranslationProviderMetadata metadata
            ? metadata.ProviderLabel
            : string.Empty;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await provider.TranslateAsync(WarmupText, cancellationToken);
            stopwatch.Stop();
            return new TranslationConnectionProbeResult(
                Succeeded: true,
                ProviderLabel: providerLabel,
                stopwatch.Elapsed,
                ErrorMessage: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new TranslationConnectionProbeResult(
                Succeeded: false,
                ProviderLabel: providerLabel,
                stopwatch.Elapsed,
                ErrorMessage: exception.Message);
        }
    }
}

public sealed record TranslationConnectionProbeResult(
    bool Succeeded,
    string ProviderLabel,
    TimeSpan Latency,
    string? ErrorMessage);
