using System;

namespace whatisthatService.SpeciesIndentifier.Classification.Exceptions
{
    public class NoClientResponseError : Exception
    {
        public NoClientResponseError(String message) : base(message)
        {
        }

        public NoClientResponseError(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}