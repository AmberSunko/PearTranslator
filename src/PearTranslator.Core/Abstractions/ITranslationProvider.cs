namespace PearTranslator.Core.Abstractions;

public interface ITranslationProvider
{
    Task<string> TranslateAsync(string sourceText, CancellationToken cancellationToken);
}
