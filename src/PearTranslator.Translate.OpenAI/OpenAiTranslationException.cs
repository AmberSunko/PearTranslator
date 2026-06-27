using System.Net;

namespace PearTranslator.Translate.OpenAI;

public sealed class OpenAiTranslationException : Exception
{
    public OpenAiTranslationException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}
