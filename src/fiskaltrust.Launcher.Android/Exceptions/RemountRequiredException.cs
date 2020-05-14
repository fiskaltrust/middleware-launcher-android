using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Launcher.Android.Exceptions
{
    [Serializable]
    public class RemountRequiredException : Exception
    {
        public RemountRequiredException()
        {
        }

        public RemountRequiredException(string message) : base(message)
        {
        }

        public RemountRequiredException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RemountRequiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}