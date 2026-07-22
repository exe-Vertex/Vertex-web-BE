using System;

namespace Vertex.Services.Exceptions
{
    public class AiProviderUnavailableException : Exception
    {
        public AiProviderUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}