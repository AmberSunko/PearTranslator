namespace PearTranslator.Core.Abstractions;

public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken);
}
