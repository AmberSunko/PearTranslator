namespace PearTranslator.Core.Assets;

public sealed record RuntimeAssetSetupProgress(
    string Message,
    int CompletedAssets,
    int TotalAssets);
