using System;

namespace gView.Blazor.Core.Exceptions;

public class GeneralException : Exception
{
    public GeneralException(string message) : base(message) { }
}
