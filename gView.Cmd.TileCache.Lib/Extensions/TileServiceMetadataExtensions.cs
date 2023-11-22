using gView.Framework.IO;
using gView.Framework.Metadata;
using gView.Server.Clients;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace gView.Cmd.TileCache.Lib.Extensions
{
    public static class TileServiceMetadataExtensions
    {
        public static Task<TileServiceMetadata?> FromService(this TileServiceMetadata metadata, string server, string service)
        {
            return new MapServerClient(server).GetTileServiceMetadata(service);
        }
    }
}
