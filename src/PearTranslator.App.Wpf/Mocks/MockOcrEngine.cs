using PearTranslator.Core.Abstractions;

namespace PearTranslator.App.Wpf.Mocks;

public sealed class MockOcrEngine : IOcrEngine
{
    private readonly string[] _samples =
    [
        "Welcome back",
        "Welcome back",
        "Open the ancient gate",
        "Open the ancient gate",
        "We must leave before sunrise",
        "We must leave before sunrise"
    ];

    private int _index;

    public Task<OcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        var text = _samples[_index % _samples.Length];
        _index++;
        return Task.FromResult(new OcrResult(text, EstimatedTextHeightPixels: 28));
    }
}
