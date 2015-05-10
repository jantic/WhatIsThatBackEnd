using System;

namespace whatisthatService.Core.Clarifai.Exceptions
{
    public class ExpiredTokenError : Exception
    {
        public ExpiredTokenError(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}