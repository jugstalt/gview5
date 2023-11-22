using gView.Data.Framework.Data.TileCache;
using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.IO;
using gView.Framework.Metadata;
using gView.Framework.system;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.Interoperability.Server.TileService.Extensions;
using gView.MapServer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.Interoperability.Server.TileService
{
    [gView.Framework.system.RegisterPlugIn("ED770739-12FA-40d7-8EF9-38FE9299564A")]
    public class TileServiceInterpreter : IServiceRequestInterpreter
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        private IMapServer _mapServer = null;
        private static Guid _metaprovider = new Guid("D33D3DD2-DD63-4a47-9F84-F840FE0D01C0");

        #region IServiceRequestInterpreter Member

        public void OnCreate(IMapServer mapServer)
        {
            _mapServer = mapServer;
        }

        async public Task Request(IServiceRequestContext context)
        {
            try
            {
                if (context == null || context.ServiceRequest == null)
                {
                    return;
                }

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    if (serviceMap == null)
                    {
                        return;
                    }

                    if (_mapServer == null)
                    {
                        context.ServiceRequest.Response = "<FATALERROR>MapServer Object is not available!</FATALERROR>";
                        return;
                    }

                    TileServiceMetadata metadata = serviceMap.MetadataProvider(_metaprovider) as TileServiceMetadata;
                    if (metadata == null || metadata.Use == false)
                    {
                        context.ServiceRequest.Response = "<ERROR>Service is not used with Tile Service</ERROR>";
                    }

                    string service = context.ServiceRequest.Service;
                    string request = context.ServiceRequest.Request;
                    if (request.ToLower().StartsWith("path="))
                    {
                        request = request.Substring(5);
                    }

                    string[] args = request.Split('/');

                    string command = args[0].ToLower();
                    bool renderOnTheFly = false;
                    if (command.Contains(":"))
                    {
                        switch (command.Split(':')[1])
                        {
                            case "render":
                                renderOnTheFly = true;
                                break;
                        }

                        command = command.Split(':')[0];
                    }

                    switch (command)
                    {
                        case "metadata":
                            XmlStream stream = new XmlStream("metadata");
                            metadata.Save(stream);
                            StringWriter sw = new StringWriter();
                            stream.WriteStream(sw);
                            sw.Close();
                            context.ServiceRequest.Response = sw.ToString();
                            break;
                        case "osm":
                        case "tms":
                            if (args.Length == 4)
                            {
                                int epsg = int.Parse(args[1]);
                                if (metadata.EPSGCodes.Contains(epsg))
                                {
                                    context.ServiceRequest.Response = TmsCapabilities(context, serviceMap, metadata, epsg);
                                }
                            }
                            else if (args.Length == 7) // tms/srs/1.0.0/service/0/0/0.png
                            {
                                int epsg = int.Parse(args[1]);
                                double scale = metadata.Scales.InnerList[int.Parse(args[4])];
                                int row = (args[0] == "tms" ? int.Parse(args[5]) : int.Parse(args[6].Split('.')[0]));
                                int col = (args[0] == "tms" ? int.Parse(args[6].Split('.')[0]) : int.Parse(args[5]));
                                string format = ".png";
                                if (args[6].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                await GetTile(context, serviceMap, metadata, epsg, scale, row, col, format, (args[0] == "tms" ? GridOrientation.LowerLeft : GridOrientation.UpperLeft), renderOnTheFly);
                            }
                            else if (args.Length == 10)  // tms/srs/service/01/000/000/001/000/000/001.png
                            {
                                int epsg = int.Parse(args[1]);
                                double scale = metadata.Scales.InnerList[int.Parse(args[3])];
                                int col = int.Parse(args[4]) * 1000000 + int.Parse(args[5]) * 1000 + int.Parse(args[6]);
                                int row = int.Parse(args[7]) * 1000000 + int.Parse(args[8]) * 1000 + int.Parse(args[9].Split('.')[0]);
                                string format = ".png";
                                if (args[9].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                await GetTile(context, serviceMap, metadata, epsg, scale, row, col, format, (args[0] == "tms" ? GridOrientation.LowerLeft : GridOrientation.UpperLeft), renderOnTheFly);
                            }
                            break;
                        case "init":
                            if (args.Length >= 5)
                            {
                                string cacheFormat = args[1].ToLower() == "compact" ? "compact" : "";
                                if (args[2].ToLower() != "ul" &&
                                    args[2].ToLower() != "ll")
                                {
                                    throw new ArgumentException();
                                }

                                int epsg = int.Parse(args[3]);
                                string format = "image/" + args[4].ToLower();
                                if (args[4].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                WriteConfFile(context, serviceMap, metadata, cacheFormat, epsg, format,
                                    (args[2].ToLower() == "ul" ? GridOrientation.UpperLeft : GridOrientation.LowerLeft));
                            }
                            break;
                        case "tile":
                            if (args.Length == 5)
                            {
                                int epsg = int.Parse(args[1]);
                                double scale = GetScale(metadata, args[2]); // double.Parse(args[2].Replace(",", "."), _nhi);
                                int row = int.Parse(args[3]);
                                int col = int.Parse(args[4].Split('.')[0]);
                                string format = ".png";
                                if (args[4].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                await GetTile(context, serviceMap, metadata, epsg, scale, row, col, format, GridOrientation.UpperLeft, renderOnTheFly);
                            }
                            else if (args.Length == 6)
                            {
                                if (args[1].ToLower() != "ul" &&
                                    args[1].ToLower() != "ll")
                                {
                                    throw new ArgumentException();
                                }

                                int epsg = int.Parse(args[2]);
                                double scale = GetScale(metadata, args[3]); // double.Parse(args[3].Replace(",", "."), _nhi);
                                int row = int.Parse(args[4]);
                                int col = int.Parse(args[5].Split('.')[0]);
                                string format = ".png";
                                if (args[5].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                await GetTile(context, serviceMap, metadata, epsg, scale, row, col, format,
                                    (args[1].ToLower() == "ul" ? GridOrientation.UpperLeft : GridOrientation.LowerLeft), renderOnTheFly);
                            }
                            else if (args.Length >= 7)
                            {
                                string cacheFormat = args[1].ToLower();
                                if (args[2].ToLower() != "ul" &&
                                    args[2].ToLower() != "ll")
                                {
                                    throw new ArgumentException();
                                }

                                int epsg = int.Parse(args[3]);
                                double scale = GetScale(metadata, args[4]); // double.Parse(args[4].Replace(",", "."), _nhi);
                                int row = int.Parse(args[5]);
                                int col = int.Parse(args[6].Split('.')[0]);
                                string format = ".png";
                                if (args[6].ToLower().EndsWith(".jpg"))
                                {
                                    format = ".jpg";
                                }

                                if (cacheFormat == "compact")
                                {
                                    var boundingTiles = args.Length > 7 ? new BoundingTiles(args[7]) : null;

                                    await GetCompactTile(context, serviceMap, metadata, epsg, scale, row, col, format,
                                        (args[2].ToLower() == "ul" ? GridOrientation.UpperLeft : GridOrientation.LowerLeft), boundingTiles, renderOnTheFly);
                                }
                                else
                                {
                                    await GetTile(context, serviceMap, metadata, epsg, scale, row, col, format, (args[2].ToLower() == "ul" ? GridOrientation.UpperLeft : GridOrientation.LowerLeft), renderOnTheFly);
                                }
                            }
                            else
                            {
                                throw new ArgumentException();
                            }

                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                if (context != null && context.ServiceRequest != null)
                {
                    context.ServiceRequest.Response = "<Exception>" + ex.Message + "</Exception>";
                }
            }
        }

        public InterpreterCapabilities Capabilities
        {
            get
            {
                return null;
            }
        }

        public string IdentityName => "tiles";

        public string IdentityLongName => "Tile Service Interpreter";

        public int Priority => 10;

        #endregion

        async private Task GetTile(IServiceRequestContext context, IServiceMap serviceMap, TileServiceMetadata metadata, int epsg, double scale, int row, int col, string format, GridOrientation orientation, bool renderOnTheFly)
        {
            if (!metadata.EPSGCodes.Contains(epsg))
            {
                throw new ArgumentException("Wrong epsg argument");
            }

            //if (!metadata.Scales.Contains(scale))
            //    throw new ArgumentException("Wrong scale argument");
            scale = metadata.Scales.GetScale(scale);
            if (scale <= 0.0)
            {
                throw new ArgumentException("Wrong scale argument");
            }

            //IEnvelope bounds = metadata.GetEPSGEnvelope(epsg);
            //if (bounds == null || bounds.Width == 0.0 || bounds.Height == 0.0)
            //    throw new Exception("No bounds defined for EPSG:" + epsg);

            format = format.ToLower();
            if (format != ".png" && format != ".jpg")
            {
                throw new Exception("Unsupported image format");
            }

            if (format == ".png" && metadata.FormatPng == false)
            {
                throw new Exception("Format image/png not supported");
            }

            if (format == ".jpg" && metadata.FormatJpg == false)
            {
                throw new Exception("Format image/jpeg no supported");
            }

            string path = _mapServer.TileCachePath + @"\" + serviceMap.Name + @"\_alllayers\" +
                TileServiceMetadata.TilePath(orientation, epsg, scale, row, col) + format;
            if ((orientation == GridOrientation.UpperLeft && metadata.UpperLeftCacheTiles) ||
                (orientation == GridOrientation.LowerLeft && metadata.LowerLeftCacheTiles))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    context.ServiceRequest.Response = fi.FullName;
                    return;
                }
                else if (!renderOnTheFly && !metadata.RenderTilesOnTheFly)
                {
                    return;  // Empty
                }
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
            }
            else
            {
                path = _mapServer.OutputPath + @"\tile_" + Guid.NewGuid().ToString("N").ToLower() + format;
            }

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + epsg);

            serviceMap.Display.SpatialReference = sRef;
            serviceMap.Display.Dpi = metadata.Dpi;

            serviceMap.Display.ImageWidth = metadata.TileWidth;
            serviceMap.Display.ImageHeight = metadata.TileHeight;

            double res = (double)scale / (metadata.Dpi / 0.0254);
            if (serviceMap.Display.MapUnits != GeoUnits.Meters)
            {
                GeoUnitConverter converter = new GeoUnitConverter();
                res = converter.Convert(res, GeoUnits.Meters, serviceMap.Display.MapUnits);
            }

            var origin = orientation == GridOrientation.UpperLeft ? metadata.GetOriginUpperLeft(epsg) : metadata.GetOriginLowerLeft(epsg);

            double H = metadata.TileHeight * res;
            double y = (orientation == GridOrientation.UpperLeft ?
                origin.Y - H * (row + 1) :
                origin.Y + H * row);

            double W = metadata.TileWidth * res;
            //if (map.Display.MapUnits == GeoUnits.DecimalDegrees)
            //{
            //    double phi = (2 * y + H) / 2.0;
            //    W /= Math.Cos(phi / 180.0 * Math.PI);
            //}
            double x = origin.X + W * col;

            serviceMap.Display.ZoomTo(new Envelope(x, y, x + W, y + H));

            if (format != ".jpg" && metadata.MakeTransparentPng == true)
            {
                serviceMap.Display.BackgroundColor = ArgbColor.Transparent;
            }

            await serviceMap.TryRender(3);
            await serviceMap.SaveImage(path, format == ".jpg" ? ImageFormat.Jpeg : ImageFormat.Png);

            context.ServiceRequest.Response = path;
        }

        async private Task GetCompactTile(IServiceRequestContext context, IServiceMap serviceMap, TileServiceMetadata metadata, int epsg, double scale, int row, int col, string format, GridOrientation orientation, BoundingTiles boundingTiles, bool renderOnTheFly)
        {
            if (!metadata.EPSGCodes.Contains(epsg))
            {
                throw new ArgumentException("Wrong epsg argument");
            }

            if (orientation != GridOrientation.UpperLeft)
            {
                throw new ArgumentException("Compact Tiles Orientation must bei Upper Left!");
            }

            scale = metadata.Scales.GetScale(scale);
            if (scale <= 0.0)
            {
                throw new ArgumentException("Wrong scale argument");
            }

            //IEnvelope bounds = metadata.GetEGPSEnvelope(epsg);
            //if (bounds == null || bounds.Width == 0.0 || bounds.Height == 0.0)
            //    throw new Exception("No bounds defined for EPSG:" + epsg);
            IPoint origin = metadata.GetOriginUpperLeft(epsg);
            if (origin == null)
            {
                throw new Exception("No origin defined for EPSG:" + epsg);
            }

            format = format.ToLower();
            if (format != ".png" && format != ".jpg")
            {
                throw new Exception("Unsupported image format");
            }

            if (format == ".png" && metadata.FormatPng == false)
            {
                throw new Exception("Format image/png not supported");
            }

            if (format == ".jpg" && metadata.FormatJpg == false)
            {
                throw new Exception("Format image/jpeg no supported");
            }

            string path = _mapServer.TileCachePath + @"\" + serviceMap.Name + @"\_alllayers\compact\" +
                TileServiceMetadata.ScalePath(orientation, epsg, scale);

            string compactTileName = CompactTileName(row, col);

            string bundleFilename = path + @"\" + compactTileName + ".tilebundle";
            string bundleDoneFilename = path + @"\" + compactTileName + ".tilebundle.done";
            string bundleCalcFilename = path + @"\" + compactTileName + ".tilebundle.calc";

            if (new FileInfo(bundleFilename).Exists)
            {
                GetCompactTileBytes(context, serviceMap, path, row, col);
                return;
            }
            else if (!renderOnTheFly || new FileInfo(bundleDoneFilename).Exists || new FileInfo(bundleCalcFilename).Exists /* && !metadata.RenderTilesOnTheFly*/)
            {


                return; // Empty
            }

            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di.Create();
            }

            try { File.Delete(bundleFilename); }
            catch { }

            //temp
            //string pathTemp = path + @"\temp";
            //DirectoryInfo diTemp = new DirectoryInfo(pathTemp);
            //if (!diTemp.Exists)
            //    diTemp.Create();

            File.WriteAllText(bundleCalcFilename, "calc...");
            CompactTilesIndexBuilder indexBuilder = new CompactTilesIndexBuilder();

            int startRow = CompactTileStart(row), startCol = CompactTileStart(col);

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + epsg);

            serviceMap.Display.SpatialReference = sRef;
            serviceMap.Display.Dpi = metadata.Dpi;

            double res = (double)scale / (metadata.Dpi / 0.0254);
            if (serviceMap.Display.MapUnits != GeoUnits.Meters)
            {
                GeoUnitConverter converter = new GeoUnitConverter();
                res = converter.Convert(res, GeoUnits.Meters, serviceMap.Display.MapUnits);
            }


            string bundleTempFilename = path + @"\" + compactTileName + "." + Guid.NewGuid().ToString("N").ToLower() + ".tilebundle";
            string bundleIndexFilename = path + @"\" + compactTileName + ".tilebundlx";

            File.WriteAllBytes(bundleTempFilename, new byte[0]);
            int bundlePos = 0;

            int tileMatrixWidth = 8, tileMatrixHeight = 8;

            serviceMap.Display.ImageWidth = metadata.TileWidth * tileMatrixWidth;
            serviceMap.Display.ImageHeight = metadata.TileHeight * tileMatrixHeight;

            for (int r = 0; r < 128; r += 8)
            {
                File.WriteAllText(bundleCalcFilename, "calc...row" + r);
                for (int c = 0; c < 128; c += 8)
                {
                    int currentRow = r + startRow, currentCol = c + startCol;

                    if (boundingTiles != null)
                    {
                        if (!boundingTiles.Check(currentRow, currentCol, 8, 8))
                        {
                            continue;
                        }
                    }

                    double H = metadata.TileHeight * res;
                    double y = origin.Y - H * (currentRow + tileMatrixHeight);

                    double W = metadata.TileWidth * res;
                    double x = origin.X + W * currentCol;

                    serviceMap.Display.ZoomTo(new Envelope(x, y, x + W * tileMatrixWidth, y + H * tileMatrixHeight));
                    if (format != ".jpg" && metadata.MakeTransparentPng == true)
                    {
                        serviceMap.Display.BackgroundColor = ArgbColor.Transparent;
                    }

                    serviceMap.ReleaseImage();  // Delete old Image !!! Because there is no map.SaveImage()!!!!
                    await serviceMap.TryRender(3);

                    if (IsEmptyBitmap(serviceMap.MapImage, serviceMap.Display.BackgroundColor))
                    {
                        continue;
                    }

                    // Temp
                    //map.MapImage.Save(pathTemp + @"\matrix_" + (startRow + r) + "_" + (startCol + c) + ".png", ImageFormat.Png);

                    for (int j = 0; j < tileMatrixHeight; j++)
                    {
                        for (int i = 0; i < tileMatrixWidth; i++)
                        {
                            int tileRow = currentRow + j, tileCol = currentCol + i;

                            if (boundingTiles != null)
                            {
                                if (!boundingTiles.Check(tileRow, tileCol, 8, 8))
                                {
                                    continue;
                                }
                            }

                            using (var bitmap = Current.Engine.CreateBitmap(metadata.TileWidth, metadata.TileHeight, serviceMap.MapImage.PixelFormat))
                            using (var canvas = bitmap.CreateCanvas())
                            {
                                canvas.InterpolationMode = InterpolationMode.NearestNeighbor;
                                canvas.DrawBitmap(serviceMap.MapImage,
                                    new CanvasRectangleF(0f, 0f, (float)bitmap.Width, (float)bitmap.Height),
                                    new CanvasRectangleF(-0.5f + (float)(i * metadata.TileWidth), -0.5f + (float)(j * metadata.TileHeight), (float)metadata.TileWidth, (float)metadata.TileHeight));

                                //for (int py = 0, to_py = bm.Height; py < to_py; py++)
                                //{
                                //    for (int px = 0, to_px = bm.Width; px < to_px; px++)
                                //    {
                                //        var pCol = map.MapImage.GetPixel(px + i * metadata.TileHeight, py + j * metadata.TileHeight);
                                //        bm.SetPixel(px, py, pCol);
                                //    }
                                //}

                                if (IsEmptyBitmap(bitmap, serviceMap.Display.BackgroundColor))
                                {
                                    continue;
                                }

                                // Temp
                                //bm.Save(pathTemp + @"\tile_" + tileRow + "_" + tileCol + ".png", ImageFormat.Png);

                                //try
                                //{
                                //    if (format != ".jpg" && map.Display.BackgroundColor.A > 0)   // Make PNG Transparent
                                //        bm.MakeTransparent(map.Display.BackgroundColor);
                                //}
                                //catch { }

                                canvas.Flush();
                                MemoryStream ms = new MemoryStream();
                                bitmap.Save(ms, format == ".jpg" ? ImageFormat.Jpeg : ImageFormat.Png);


                                byte[] imageBytes = ms.ToArray();
                                using (var stream = new FileStream(bundleTempFilename, FileMode.Append))
                                {
                                    stream.Write(imageBytes, 0, imageBytes.Length);
                                }

                                indexBuilder.SetValue(r + j, c + i, bundlePos, imageBytes.Length);

                                bundlePos += imageBytes.Length;
                            }
                        }
                    }
                }

                serviceMap.ReleaseImage();
                GC.Collect();
            }

            try { File.Delete(bundleFilename); }
            catch { }
            if (bundlePos == 0)
            {
                File.Delete(bundleTempFilename);
                File.WriteAllText(bundleDoneFilename, "");
            }
            else
            {
                File.Move(bundleTempFilename, bundleFilename);
                indexBuilder.Save(bundleIndexFilename);
            }
            try { File.Delete(bundleCalcFilename); }
            catch { }


            GC.Collect();
        }

        private void GetCompactTileBytes(IServiceRequestContext context, IServiceMap serviceMap, string path, int row, int col)
        {
            string compactTileName = CompactTileName(row, col);

            string bundleFilename = path + @"\" + compactTileName + ".tilebundle";
            string bundleIndexFilename = path + @"\" + compactTileName + ".tilebundlx";

            FileInfo fi = new FileInfo(bundleIndexFilename);
            if (!fi.Exists)
            {
                return;
            }

            CompactTileIndex bundleIndex = new CompactTileIndex(bundleIndexFilename);

            int bundleStartRow = CompactTileStart(row);
            int bundleStartCol = CompactTileStart(col);

            try
            {

                int tileLength, tilePosition = bundleIndex.TilePosition(row - bundleStartRow, col - bundleStartCol, out tileLength);

                if (tilePosition < 0)
                {
                    return;
                }

                using (FileStream fs = File.Open(bundleFilename, FileMode.Open, FileAccess.Read, FileShare.Read)) //new FileStream(bundleFilename, FileMode.Open, FileAccess.Read))
                {
                    fs.Position = tilePosition;

                    byte[] data = new byte[tileLength];
                    fs.Read(data, 0, tileLength);

                    context.ServiceRequest.Response = new MapServerResponse()
                    {
                        Data = data,
                        ContentType = "image/jpg",
                        Expires = DateTime.UtcNow.AddDays(7)
                    }.ToString();
                }
            }
            catch (Exception ex)
            {
                TileServiceMetadata metadata = serviceMap.MetadataProvider(_metaprovider) as TileServiceMetadata;
                using (var bitmap = Current.Engine.CreateBitmap(metadata.TileWidth, metadata.TileHeight))
                using (var canvas = bitmap.CreateCanvas())
                using (var font = Current.Engine.CreateFont("Arial", 9f))
                using (var redBrush = Current.Engine.CreateSolidBrush(ArgbColor.Red))
                {
                    canvas.DrawText(ex.Message, font, redBrush, new CanvasRectangleF(0f, 0f, (float)bitmap.Width, (float)bitmap.Height));
                    canvas.Flush();

                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);

                    context.ServiceRequest.Response = new MapServerResponse()
                    {
                        Data = ms.ToArray(),
                        ContentType = "image/jpg"
                    }.ToString();
                }
            }
        }

        private byte[] GetCompactTileBytes(string path, int row, int col)
        {
            string compactTileName = CompactTileName(row, col);

            string bundleFilename = path + @"\" + compactTileName + ".tilebundle";
            string bundleIndexFilename = path + @"\" + compactTileName + ".tilebundlx";

            FileInfo fi = new FileInfo(bundleIndexFilename);
            if (!fi.Exists)
            {
                return null;
            }

            CompactTileIndex bundleIndex = new CompactTileIndex(bundleIndexFilename);

            int bundleStartRow = CompactTileStart(row);
            int bundleStartCol = CompactTileStart(col);

            try
            {

                int tileLength, tilePosition = bundleIndex.TilePosition(row - bundleStartRow, col - bundleStartCol, out tileLength);

                if (tilePosition < 0)
                {
                    return null;
                }

                using (FileStream fs = File.Open(bundleFilename, FileMode.Open, FileAccess.Read, FileShare.Read)) //new FileStream(bundleFilename, FileMode.Open, FileAccess.Read))
                {
                    fs.Position = tilePosition;

                    byte[] data = new byte[tileLength];
                    fs.Read(data, 0, tileLength);

                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        private void WriteConfFile(IServiceRequestContext context, IServiceMap serviceMap, TileServiceMetadata metadata, string cacheFormat, int epsg, string format, GridOrientation orientation)
        {
            FileInfo configFileInfo = new FileInfo(_mapServer.TileCachePath + @"\" + serviceMap.Name + @"\_alllayers\" + cacheFormat + @"\" + TileServiceMetadata.EpsgPath(orientation, epsg) + @"\conf.json");

            IPoint origin = orientation == GridOrientation.UpperLeft ? metadata.GetOriginUpperLeft(epsg) : metadata.GetOriginLowerLeft(epsg);
            IEnvelope bounds = metadata.GetEPSGEnvelope(epsg);
            if (origin == null || bounds == null)
            {
                return;
            }

            List<CompactTileConfig.LevelConfig> levels = new List<CompactTileConfig.LevelConfig>();
            for (int i = 0; i < metadata.Scales.InnerList.Count; i++)
            {
                levels.Add(new CompactTileConfig.LevelConfig()
                {
                    Level = i,
                    Scale = metadata.Scales.InnerList[i]
                });
            }

            CompactTileConfig config = new CompactTileConfig()
            {
                Epsg = epsg,
                Dpi = metadata.Dpi,
                Origin = new double[] { origin.X, origin.Y },
                Extent = new double[] { bounds.minx, bounds.miny, bounds.maxx, bounds.maxy },
                TileSize = new int[] { metadata.TileWidth, metadata.TileHeight },
                Format = format,
                Orientation = orientation.ToString(),
                Levels = levels.ToArray()
            };

            if (configFileInfo.Exists)
            {
                configFileInfo.Delete();
            }

            if (!configFileInfo.Directory.Exists)
            {
                configFileInfo.Directory.Create();
            }

            File.WriteAllText(configFileInfo.FullName, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        #region Helper

        private double GetScale(TileServiceMetadata metadata, string scaleArgument)
        {
            if (scaleArgument.StartsWith("~"))
            {
                scaleArgument = scaleArgument.Substring(1);
                return metadata.Scales.InnerList[int.Parse(scaleArgument)];
            }
            return double.Parse(scaleArgument.Replace(",", "."), _nhi);
        }

        #endregion

        #region Compact Helper

        public int CompactTileStart(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            return (index >> 7) * 128;   // 128 x 128 Tiles werden zu einem Bundle zusammengefasst
        }

        public string CompactTileName(int row, int col)
        {
            return "R" + CompactTileStart(row).ToString("X8") + "C" + CompactTileStart(col).ToString("X8");
        }

        public double GetStdDev(IBitmap bitmap)
        {
            double total = 0, totalVariance = 0;
            int count = 0;
            double stdDev = 0;

            // First get all the bytes
            BitmapPixelData bmData = null;
            try
            {
                bmData = bitmap.LockBitmapPixelData(BitmapLockMode.ReadOnly, bitmap.PixelFormat);

                int stride = bmData.Stride;
                IntPtr Scan0 = bmData.Scan0;

                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    int nOffset = stride - bitmap.Width * 3;

                    for (int y = 0; y < bitmap.Height; ++y)
                    {

                        for (int x = 0; x < bitmap.Width; ++x)
                        {

                            count++;

                            byte blue = p[0];
                            byte green = p[1];
                            byte red = p[2];

                            int pixelValue = ArgbColor.FromArgb(0, red, green, blue).ToArgb();

                            total += pixelValue;
                            double avg = total / count;

                            totalVariance += Math.Pow(pixelValue - avg, 2);
                            stdDev = Math.Sqrt(totalVariance / count);

                            p += 3;

                        }

                        p += nOffset;

                    }

                }

            }
            finally
            {
                if (bmData != null)
                {
                    bitmap.UnlockBitmapPixelData(bmData);
                }
            }

            return stdDev;
        }

        public bool IsEmptyBitmap(IBitmap bitmap, ArgbColor backgroundColor)
        {
            BitmapPixelData bmData = null;
            try
            {
                bmData = bitmap.LockBitmapPixelData(BitmapLockMode.ReadOnly, PixelFormat.Rgba32);

                int stride = bmData.Stride;
                IntPtr Scan0 = bmData.Scan0;

                int backgroundColorValue = ArgbColor.FromArgb(
                    backgroundColor.A,
                    backgroundColor.R,
                    backgroundColor.G,
                    backgroundColor.B).ToArgb();

                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    int nOffset = stride - bitmap.Width * 4;

                    for (int y = 0; y < bitmap.Height; ++y)
                    {

                        for (int x = 0; x < bitmap.Width; ++x)
                        {
                            byte blue = p[0];
                            byte green = p[1];
                            byte red = p[2];
                            byte alpha = p[3];

                            if (alpha != 0)  // Not transparent
                            {
                                int pixelValue = ArgbColor.FromArgb(alpha, red, green, blue).ToArgb();

                                if (!pixelValue.Equals(backgroundColorValue))
                                {
                                    return false;
                                }
                            }
                            p += 4;
                        }

                        p += nOffset;

                    }

                }


            }
            finally
            {
                if (bitmap != null && bmData != null)
                {
                    bitmap.UnlockBitmapPixelData(bmData);
                }
            }
            return true;
        }

        #endregion

        #region Compact Classes

        public class CompactTilesIndexBuilder
        {
            private int[] _index = new int[128 * 128 * 2];

            public CompactTilesIndexBuilder()
            {
                InitIndex();
            }

            private void InitIndex()
            {
                for (int i = 0; i < _index.Length; i++)
                {
                    _index[i] = -1;
                }
            }

            public void SetValue(int row, int col, int position, int length)
            {
                if (row < 0 || row > 128 || col < 0 || col > 128)
                {
                    throw new ArgumentException("Compact Tile Index out of range");
                }

                int indexPosition = ((row * 128) + col) * 2;
                if (indexPosition > _index.Length - 2)
                {
                    throw new AggregateException("Index!!!");
                }

                _index[indexPosition] = position;
                _index[indexPosition + 1] = length;
            }

            public void Save(string filename)
            {
                try { File.Delete(filename); }
                catch { }

                using (var stream = new FileStream(filename, FileMode.Create))
                {
                    for (int i = 0; i < _index.Length; i++)
                    {
                        byte[] data = BitConverter.GetBytes(_index[i]);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
        }

        public class CompactTileIndex
        {
            public CompactTileIndex(string filename)
            {
                this.Filename = filename;
            }

            public string Filename { get; private set; }

            public int TilePosition(int row, int col, out int tileLength)
            {
                if (row < 0 || row > 128 || col < 0 || col > 128)
                {
                    throw new ArgumentException("Compact Tile Index out of range");
                }

                int indexPosition = ((row * 128) + col) * 8;

                using (FileStream fs = File.Open(this.Filename, FileMode.Open, FileAccess.Read, FileShare.Read)) // new FileStream(this.Filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = new byte[8];
                    fs.Position = indexPosition;
                    fs.Read(data, 0, 8);

                    int position = BitConverter.ToInt32(data, 0);
                    tileLength = BitConverter.ToInt32(data, 4);

                    return position;
                }
            }
        }

        public class BoundingTiles
        {
            public BoundingTiles(string bounds)
            {
                string[] b = bounds.Split('|');

                this.FromRow = int.Parse(b[0]);
                this.ToRow = int.Parse(b[1]);
                this.FromCol = int.Parse(b[2]);
                this.ToCol = int.Parse(b[3]);
            }

            public int FromRow { get; private set; }
            public int ToRow { get; private set; }

            public int FromCol { get; private set; }
            public int ToCol { get; private set; }

            public bool Check(int row, int col)
            {
                return row >= FromRow && row <= ToRow && col >= FromCol && col <= ToCol;
            }

            public bool Check(int row, int col, int matrixWidth, int matrixHeight)
            {
                for (int c = col; c < col + matrixHeight; c++)
                {
                    for (int r = row; r < row + matrixWidth; r++)
                    {
                        if (Check(r, c))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        #endregion

        #region Classes

        public class QueryString
        {
            NameValueCollection nvc = new NameValueCollection();

            public QueryString(string queryString)
            {
                queryString = queryString.ToLower();

                foreach (string argument in queryString.Split('&'))
                {
                    int pos = argument.IndexOf("=");
                    if (pos > 0)
                    {
                        nvc.Add(argument.Substring(0, pos), argument.Substring(pos + 1));
                    }
                    else
                    {
                        nvc.Add(argument, String.Empty);
                    }
                }
            }

            public string GetValue(string key)
            {
                return nvc[key.ToLower()];
            }

            public bool HasValue(string key, string val)
            {
                return nvc[key.ToLower()] == val.ToLower();
            }
        }

        #endregion

        private string TmsCapabilities(IServiceRequestContext context, IServiceMap serviceMap, TileServiceMetadata metadata, int srs)
        {
            IEnvelope box = metadata.GetEPSGEnvelope(srs);
            if (box == null)
            {
                return String.Empty;
            }

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + srs);

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<TileMap version=\"1.0.0\" tilemapservice=\"" + context.ServiceRequest.OnlineResource + "\" >");
            sb.Append("<Title>" + serviceMap.Name + "</Title>");
            sb.Append("<Abstract>gView Tile Cache</Abstract>");
            sb.Append("<SRS>EPSG:" + srs + "</SRS>");

            sb.Append("<BoundingBox minx=\"" + box.minx.ToString(_nhi) +
                                "\" miny=\"" + box.miny.ToString(_nhi) +
                                "\" maxx=\"" + box.maxx.ToString(_nhi) +
                                "\" maxy=\"" + box.maxy.ToString(_nhi) + "\" />");
            sb.Append("<Origin x=\"" + box.minx.ToString(_nhi) +
                           "\" y=\"" + box.miny.ToString(_nhi) + "\" />");

            sb.Append("<TileFormat width=\"" + metadata.TileWidth + "\" height=\"" + metadata.TileHeight + "\" mime-type=\"image/png\" extension=\"png\" />");
            sb.Append("<TileSets>");

            int level = 0;
            foreach (double scale in metadata.Scales.InnerList)
            {
                double res = (double)scale / (metadata.Dpi / 0.0254);
                if (sRef.SpatialParameters.IsGeographic)
                {
                    GeoUnitConverter converter = new GeoUnitConverter();
                    res = converter.Convert(res, GeoUnits.Meters, GeoUnits.DecimalDegrees);
                }
                sb.Append("<TileSet href=\"" + context.ServiceRequest.OnlineResource + "/" + level + "\" ");
                sb.Append("units-per-pixel=\"" + res.ToString(_nhi) + "\" order=\"" + level + "\" />");
                level++;
            }
            sb.Append("</TileSets>");

            sb.Append("</TileMap>");
            return sb.ToString();
        }

        public AccessTypes RequiredAccessTypes(IServiceRequestContext context)
        {
            return AccessTypes.None;
        }
    }
}
