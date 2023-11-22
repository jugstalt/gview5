using System;

namespace gView.Core.Framework.Exceptions
{
    public class InvalidTokenException : MapServerException
    {
        public InvalidTokenException() : base("Invalid Token (498)") { }
    }
}
