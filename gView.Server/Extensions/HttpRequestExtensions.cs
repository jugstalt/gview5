using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace gView.Server.Extensions;

internal static class HttpRequestExtensions
{
    async static public Task<string> GetBody(this HttpRequest httpRequest)
    {
        if (httpRequest.Body.CanRead)
        {
            using (var reader = new StreamReader(httpRequest.Body))
            {
                var body = await reader.ReadToEndAsync();
                return body;
            }
        }

        return String.Empty;
    }

    static public string GetGeoservicesToken(this HttpRequest httpRequest)
    {
        string token = httpRequest.Query["token"];
        if (String.IsNullOrWhiteSpace(token) && httpRequest.HasFormContentType)
        {
            try
            {
                token = httpRequest.Form["token"];
            }
            catch { }
        }

        return token;
    }

    static public string GetGeoServicesUrlToken(this HttpRequest httpRequest)
    {
        int tokenPos1 = httpRequest.Path.ToString().IndexOf("/geoservices(", StringComparison.OrdinalIgnoreCase);
        int tokenPos2 = tokenPos1 >= 0
            ? httpRequest.Path.ToString().IndexOf(")/", tokenPos1)
            : -1;

        if (tokenPos1 >= 0 && tokenPos2 > tokenPos1)
        {
            tokenPos1 += "/geoservices(".Length;
            return httpRequest.Path.ToString().Substring(tokenPos1, tokenPos2 - tokenPos1);
        }

        return string.Empty;
    }
}
