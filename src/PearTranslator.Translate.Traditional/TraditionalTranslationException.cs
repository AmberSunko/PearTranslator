using System.Net;

namespace PearTranslator.Translate.Traditional;

public sealed class TraditionalTranslationException : Exception
{
    public TraditionalTranslationException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}
