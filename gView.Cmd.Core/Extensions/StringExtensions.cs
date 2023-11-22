using System;

namespace gView.Cmd.Core.Extensions;
static public class StringExtensions
{
    static public string PrependPrefix(this string str, string prefix)
        => String.IsNullOrEmpty(prefix) ? str : $"{prefix}_{str}";
}
