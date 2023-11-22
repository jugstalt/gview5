using gView.Core.Framework.Exceptions;
using System.Text.RegularExpressions;

namespace gView.Security.Framework
{
    static public class Extensions
    {
        static public void ValidateUsername(this string username)
        {
            // ^([a-zA-Z0-9](?(?!__|--)[a-zA-Z0-9_\-])+[a-zA-Z0-9])$

            if (!Regex.IsMatch(username, @"^([a-zA-Z0-9](?(?!__|--)[a-zA-Z0-9_\-])+[a-zA-Z0-9])$") || username.Length < 4)
            {
                throw new MapServerException("Invalid username: lowercase chars, numbers, - and _ allowed (min length=4)");
            }
        }

        static public void ValidateRawUrlTokenName(this string rawUrlTokenName)
        {
            if (!Regex.IsMatch(rawUrlTokenName, @"^[a-z0-9]+$") || rawUrlTokenName.Length < 3)
            {
                throw new MapServerException("Invalid username: lowercase chars, numbers (min length=3)");
            }
        }

        static public void ValidatePassword(this string password)
        {
            // Forbidden Chars
            foreach (char c in " ".ToCharArray())
            {
                if (password.Contains(c.ToString()))
                {
                    throw new MapServerException("Invalid password char: '" + c.ToString() + "'");
                }
            }

            var result = Zxcvbn.Zxcvbn.MatchPassword(password);

            if (result.Score < 2)
            {
                throw new MapServerException("Password is not strong enough: Score=" + result.Score);
            }
        }
    }
}
