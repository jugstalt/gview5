using System.Web;

namespace gView.Web.Framework.Web.Extensions
{
    static public class StringExtensions
    {
        public static string AddParameterToUrl(this string url, string paramName, string paramValue)
        {
            if (url is null)
            {
                url = string.Empty;
            }

            var query = $"{paramName}={HttpUtility.UrlEncode(paramValue)}";

            if (url.EndsWith("&") || url.EndsWith("?"))
            {
                return $"{url}{query}";
            }
            else if (url.Contains("?"))
            {
                return $"{url}&{query}";
            }
            else
            {
                return $"{url}?{query}";
            }

            //var uriBuilder = new UriBuilder(url);
            //var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            //query[paramName] = paramValue;
            //uriBuilder.Query = query.ToString();
            //return uriBuilder.ToString();
        }
    }
}
