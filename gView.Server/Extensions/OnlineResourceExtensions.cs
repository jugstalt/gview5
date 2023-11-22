using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Text;

namespace gView.Server.Extensions;

static public class OnlineResourceExtensions
{
    static public string AppendWmsServerPath(this string onlineResource,
                                             HttpRequest httpRequest,
                                             string service,
                                             string folder)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(onlineResource);

        var urlToken = httpRequest.GetGeoServicesUrlToken();
        if(!string.IsNullOrEmpty(urlToken) )
        {
            sb.Append("/geoservices(");
            sb.Append(urlToken);
            sb.Append(")/rest/services/");
        } 
        else
        {
            sb.Append("/geoservices/rest/services/");
        }
        
        if (!string.IsNullOrEmpty(folder))
        {
            sb.Append("/");
            sb.Append(folder);
        }

        sb.Append("/");
        sb.Append(service);
        sb.Append("/MapServer/WmsServer");

        var token = httpRequest.Query["token"];
        if(!string.IsNullOrEmpty(token))
        {
            sb.Append("?token=");
            sb.Append(token);
        }

        return sb.ToString();
    }
}
