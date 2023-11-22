using System;

namespace gView.Core.Framework.Exceptions
{
    public class NotAuthorizedException : MapServerException
    {
        public NotAuthorizedException() : base("Not Authorized") { }
        public NotAuthorizedException(string message) : base(message) { }
    }
}
