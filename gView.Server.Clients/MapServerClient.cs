using gView.Framework.IO;
using gView.Framework.Metadata;
using gView.Server.Clients.Extensions;
using gView.Server.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace gView.Server.Clients;

public class MapServerClient
{
    private static HttpClient? _httpClient;

    private readonly string _server;

    public MapServerClient(string server)
    {
        if (_httpClient == null)
        {
            _httpClient = new HttpClient();
        }
        _server = server;
    }

    async public Task<ServicesModel?> GetServices(string? username = null, string? password = null, string requestFormat = "xml")
    {
        string url = $"{_server}/mapserver/catalog?format={requestFormat}";

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
        {
            if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
            {
                var authenticationString = $"{username}:{password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.UTF8.GetBytes(authenticationString));
                requestMessage.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
            }

            var response = await _httpClient!.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error: StatusCode {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            if ("json".Equals(responseString, StringComparison.OrdinalIgnoreCase))
            {
                return JsonConvert.DeserializeObject<ServicesModel>(responseString);
            }

            // xml
            XElement root = XElement.Parse(responseString);
            var services = root.Descendants("SERVICE")
                               .Select(x => new ServiceModel
                               {
                                   Name = (string)x.Attribute("name"),
                                   Type = (string)x.Attribute("type")
                               })
                               .ToList();

            return new ServicesModel { Services = services };
        }
    }

    async public Task<TileServiceMetadata?> GetTileServiceMetadata(string service)
    {
        var url = _server.ToWmtsUrl(service, "GetMetadata");

        var response = await _httpClient!.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"{url} return with status code {response.StatusCode}");
        }

        var responseStream = await response.Content.ReadAsStreamAsync();
        XmlStream xmlStream = new XmlStream("WmtsMetadata");
        xmlStream.ReadStream(responseStream);

        return xmlStream.Load("TileServiceMetadata") as TileServiceMetadata;
    }
}
