using gView.Server.AppCode;
using System;
using System.Linq;

namespace gView.Server.Extensions;

static internal class StringExtensions
{

    static public bool UserNameIsUrlToken(this string username) => username.StartsWith(Globals.UrlTokenNamePrefix);

    static public bool IsValueUrlToken(this string token) => token.UserNameIsUrlToken() && token.Contains("~");

    static public string NameOfUrlToken(this string token) =>
        token.IsValueUrlToken()
            ? token.Split('~')[0]
            : throw new Exception("Invalid url token");
}
