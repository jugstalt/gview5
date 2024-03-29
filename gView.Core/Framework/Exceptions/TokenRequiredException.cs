﻿using System;

namespace gView.Core.Framework.Exceptions
{
    public class TokenRequiredException : Exception
    {
        public TokenRequiredException() : base("Token required (499)") { }
        public TokenRequiredException(string message) : base("Token required (499): " + message) { }
    }
}
