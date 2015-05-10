using System;

namespace whatisthatService.SpeciesIndentifier.Clarifai.Exceptions
{
    public class ApiThrottleError : Exception
    {
        private readonly Int32 _waitSeconds;

        public ApiThrottleError(String message, Int32 waitSeconds) : base(message)
        {
            _waitSeconds = waitSeconds;
        }

        public ApiThrottleError(String message, Int32 waitSeconds, Exception innerException)
            : base(message, innerException)
        {
            _waitSeconds = waitSeconds;
        }

        public Int32 WaitSeconds
        {
            get { return _waitSeconds; }
        }
    }
}