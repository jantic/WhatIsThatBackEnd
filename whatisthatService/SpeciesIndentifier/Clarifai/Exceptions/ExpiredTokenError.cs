using System;

namespace whatisthatService.SpeciesIndentifier.Clarifai.Exceptions
{
    public class ExpiredTokenError : Exception
    {
        public ExpiredTokenError(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}