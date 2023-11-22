using System;
using System.Diagnostics.CodeAnalysis;

namespace gView.Blazor.Core.Exceptions;
internal class MapRendererNotIntializedException : Exception
{
    static public void ThrowIfNull([NotNull] object? obj)
    {
        if (obj == null) throw new MapRendererNotIntializedException();
    }
}
