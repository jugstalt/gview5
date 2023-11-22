using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.IO
{
    static public class IOExtensions
    {
        static private HttpClient _httpClient = null;

        static public string ToPlatformPath(this string path)
        {
            return path.Replace(@"\", "/");
        }

        static public void SetRequestUrl(this HttpRequestMessage requestMessage, string url)
        {
            var uri = new Uri(url);

            if (!String.IsNullOrEmpty(uri.UserInfo))
            {
                requestMessage.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(uri.UserInfo))}");
                uri = new Uri($"{uri.Scheme}://{uri.Authority}{uri.PathAndQuery}");
            }

            requestMessage.RequestUri = uri;
        }

        async static public Task<int> SaveOrUpload(this IBitmap bitmap, string path, ImageFormat format)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                string url = path.Substring(0, path.LastIndexOf("/"));
                string filename = path.Substring(path.LastIndexOf("/") + 1);

                Console.WriteLine($"upload {filename} to {url}");

                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, format);

                var file_bytes = ms.ToArray();

                // reuse HttpClient
                var client = _httpClient ?? (_httpClient = new HttpClient());

                try
                {
                    using (var requestMessage = new HttpRequestMessage())
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();
                        form.Add(new ByteArrayContent(file_bytes, 0, file_bytes.Length), "file", filename);

                        requestMessage.Method = HttpMethod.Post;
                        requestMessage.Content = form;
                        requestMessage.SetRequestUrl(url);

                        HttpResponseMessage response = await client.SendAsync(requestMessage);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"SaveOrUpload: Upload status code: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"SaveOrUpload: {ex.Message}", ex);
                }

                return file_bytes.Length;
            }
            else
            {
                bitmap.Save(path, format);
                return 1;
            }
        }
    }
}
