namespace PearTranslator.Core.Assets;

public sealed record RuntimeAssetStatus(
    bool IsComplete,
    IReadOnlyList<RuntimeAssetDescriptor> MissingAssets);
