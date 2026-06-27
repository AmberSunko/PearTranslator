namespace PearTranslator.Core.Assets;

public sealed record RuntimeAssetDescriptor(
    string Name,
    string RelativePath,
    Uri Uri,
    long MinimumBytes);
