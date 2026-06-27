namespace PearTranslator.Core.Translation;

public sealed class LocalDictionaryMissException : Exception
{
    public LocalDictionaryMissException(string sourceText)
        : base($"No local dictionary entry matched '{sourceText}'.")
    {
    }
}
