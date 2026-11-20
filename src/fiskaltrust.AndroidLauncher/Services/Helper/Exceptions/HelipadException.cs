using System;
using System.Net;
using System.Runtime.Serialization;

namespace fiskaltrust.AndroidLauncher.Services.Helper.Exceptions
{
    [Serializable]
    public class HelipadException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public HelipadException(string message, HttpStatusCode httpStatusCode) : base(message)
        {
            StatusCode = httpStatusCode;
        }

    }
}
