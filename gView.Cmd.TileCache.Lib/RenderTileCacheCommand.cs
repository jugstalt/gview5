using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Core.Extensions;
using gView.Cmd.TileCache.Lib.Extensions;
using gView.Cmd.TileCache.Lib.Tools;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.Metadata;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Cmd.TileCache.Lib
{
    public class RenderTileCacheCommand : ICommand
    {
        public string Name => "TileCache.Render";

        public string Description => "Forces a gView Server instance to render service a tile cache";

        public string ExecutableName => "";

        public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
        {
            new RequiredCommandParameter<string>("server")
            {
                Description = "gView Server Instance, eg. https://my-server/gview-server"
            },
            new RequiredCommandParameter<string>("service")
            {
                Description = "The service to pre-render, eg. folder@servicename"
            },
            new CommandParameter<int>("epsg")
            {
                Description = "EPSG Code [default: first]"
            },
            new CommandParameter<bool>("compact")
            {
                Description = "create a compact tile cache",
            },
            new CommandParameter<GridOrientation>("orientation")
            {
                Description = "Orientation of origin [default: UpperLeft]"
            },
            new CommandParameter<TileImageFormat>("imageformat")
            {
                Description = "Imageformat: <png|jpg>"
            },
            new CommandParameter<IEnvelope>("bbox")
            {
                Description = "Boundingbox to render [default: full-extent]"
            },
            new CommandParameter<string>("scales")
            {
                Description = "Scales to render <scale-dominator1,scale2-dominator2> [default: all scales]"
            },
            new CommandParameter<int>("threads")
            {
                Description = "Maximum parallel requests [default: 1]"
            }
        };

        async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
        {
            try
            {
                string server = parameters.GetRequiredValue<string>("server");
                string service = parameters.GetRequiredValue<string>("service");

                #region Read Metadata

                var metadata = await new TileServiceMetadata().FromService(server, service);
                if (metadata == null)
                {
                    throw new Exception("Can't read metadata from server. Are you sure that ervice is a gView WMTS service?");
                }

                #endregion

                int epsg = parameters.GetValueOrDefault("epsg", metadata.EPSGCodes.First());
                var orientation = parameters.GetValueOrDefault("orientation", GridOrientation.UpperLeft);
                bool compact = parameters.HasKey("compact");
                IEnumerable<double>? scales = parameters.GetValueOrDefault<string>("scales", null)?
                    .Split(',')
                    .Select(s => double.Parse(s, System.Globalization.NumberFormatInfo.InvariantInfo))
                    .ToArray();
                var imageFormat = parameters.GetValueOrDefault("imageformat", metadata.FormatPng ? TileImageFormat.png : TileImageFormat.jpg);

                IEnvelope? bbox = null;

                if (parameters.ContainsKey("bbox_minx"))
                {
                    var envelopeBuilder = new EnvelopeParameterBuilder("bbox");
                    bbox = await envelopeBuilder.Build<IEnvelope>(parameters);
                }

                int threads = parameters.GetValueOrDefault<int>("threads", 1);

                var tileRender = new TileRenderer(metadata,
                                                  epsg > 0 ? epsg : metadata.EPSGCodes.First(),
                                                  cacheFormat: compact ? "compact" : "",
                                                  orientation: orientation,
                                                  imageFormat: imageFormat,
                                                  bbox: bbox,
                                                  preRenderScales: scales?.Count() > 0 ? scales : null,
                                                  maxParallelRequests: threads,
                                                  cancelTracker: cancelTracker);

                tileRender.Renderer(server, service, logger);

                return true;
            }
            catch (Exception ex)
            {
                logger?.LogLine($"ERROR: {ex.Message}");

                return false;
            }
        }
    }
}
