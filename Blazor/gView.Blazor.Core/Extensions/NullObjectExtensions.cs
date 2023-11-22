using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace gView.Blazor.Core.Extensions;

public static class ObjectNullExtensions
{
    public static T ThrowIfNull<T>([NotNull] this T? obj, Func<string> messageFunc)
        where T : class
    {
        return obj ?? throw new Exception(messageFunc());
    }

    public static IEnumerable<T> ThrowIfNullOrEmpty<T>(this IEnumerable<T> list, Func<string> messageFunc)
    {
        if (list == null || list.Count() == 0)
        {
            throw new Exception(messageFunc());
        }

        return list;
    }

    public static bool ThrowIfFalse(this bool boolean, Func<string> messageFunc)
    {
        if (boolean == false)
        {
            throw new Exception(messageFunc());
        }

        return boolean;
    }

    public static bool ThrowIfTrue(this bool boolean, Func<string> messageFunc)
    {
        if (boolean == true)
        {
            throw new Exception(messageFunc());
        }

        return boolean;
    }
}
