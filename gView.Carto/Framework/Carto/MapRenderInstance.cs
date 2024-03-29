﻿using gView.Data.Framework.Data;
using gView.Data.Framework.Data.Abstraction;
using gView.Framework.Carto.LayerRenderers;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gView.Framework.Carto
{
    public class MapRenderInstance : Map
    {
        private Map _original;
        private MapRenderInstance(Map original)
        {
            _original = original;
        }

        async static public Task<MapRenderInstance> CreateAsync(Map original)
        {
            var mapRenderInstance = new MapRenderInstance(original);

            mapRenderInstance._layers = original._layers;
            mapRenderInstance._datasets = original._datasets;

            mapRenderInstance.m_imageMerger = new ImageMerger2();

            mapRenderInstance.m_name = original.Name;
            mapRenderInstance._toc = original._toc;
            mapRenderInstance.Title = original.Title;

            //serviceMap._ceckLayerVisibilityBeforeDrawing = true;
            mapRenderInstance._mapUnits = original.MapUnits;
            mapRenderInstance._displayUnits = original.DisplayUnits;
            mapRenderInstance.refScale = original.refScale;

            mapRenderInstance.SpatialReference = original.Display.SpatialReference;
            mapRenderInstance.LayerDefaultSpatialReference = original.LayerDefaultSpatialReference != null ? original.LayerDefaultSpatialReference.Clone() as ISpatialReference : null;

            mapRenderInstance._drawScaleBar = false;

            // Metadata
            await mapRenderInstance.SetMetadataProviders(await original.GetMetadataProviders());
            mapRenderInstance._debug = false;

            mapRenderInstance._layerDescriptions = original.LayerDescriptions;
            mapRenderInstance._layerCopyrightTexts = original.LayerCopyrightTexts;

            mapRenderInstance.SetResourceContainer(original.ResourceContainer);

            mapRenderInstance.Display.iWidth = original.Display.iWidth;
            mapRenderInstance.Display.iHeight = original.Display.iHeight;
            mapRenderInstance.Display.ZoomTo(original.Envelope);
            mapRenderInstance.Display.dpi = original.Display.dpi;

            mapRenderInstance.DrawingLayer += (string layerName) =>
            {
                original.FireDrawingLayer(layerName);
            };
            mapRenderInstance.OnUserInterface += (sender, lockUI) =>
            {
                original.FireOnUserInterface(lockUI);
            };

            return mapRenderInstance;
        }

        #region IMap

        #region Fields

        private Envelope _lastRenderExtent = null;
        static private MemoryStream _msGeometry = null, _msSelection = null;

        #endregion

        #region Events

        public override event NewBitmapEvent NewBitmap;
        public override event DrawingLayerEvent DrawingLayer;
        public override event DoRefreshMapViewEvent DoRefreshMapView;

        #endregion

        async override public Task<bool> RefreshMap(DrawPhase phase, ICancelTracker cancelTracker)
        {
            base.ResetRequestExceptions();
            bool printerMap = (this.GetType() == typeof(PrinterMap));

            try
            {
                _original.FireStartRefreshMap();

                using (var datasetCachingContext = new DatasetCachingContext(this))
                {
                    this.IsRefreshing = true;

                    _lastException = null;

                    if (_canvas != null && phase == DrawPhase.Graphics)
                    {
                        return true;
                    }

                    #region Start Drawing/Initialisierung

                    this.ZoomTo(m_actMinX, m_actMinY, m_actMaxX, m_actMaxY);

                    if (cancelTracker == null)
                    {
                        cancelTracker = new CancelTracker();
                    }

                    IGeometricTransformer geoTransformer = GeometricTransformerFactory.Create();

                    //geoTransformer.ToSpatialReference = this.SpatialReference;
                    if (!printerMap)
                    {
                        if (phase == DrawPhase.All)
                        {
                            DisposeStreams();
                        }

                        if (_bitmap != null && (_bitmap.Width != iWidth || _bitmap.Height != iHeight))
                        {

                            if (!DisposeImage())
                            {
                                return false;
                            }
                        }

                        if (_bitmap == null)
                        {
                            //DisposeStreams();
                            _bitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgba32);
                            //if (NewBitmap != null && cancelTracker.Continue) NewBitmap(_image);
                        }

                        _canvas = _bitmap.CreateCanvas();
                        //this.dpi = /*96f*/ /* _canvas.DpiX*/ GraphicsEngine.Current.Engine.ScreenDpi;

                        // NewBitmap immer aufrufen, da sonst neuer DataView nix mitbekommt
                        if (NewBitmap != null && cancelTracker.Continue)
                        {
                            NewBitmap?.BeginInvoke(_bitmap, new AsyncCallback(AsyncInvoke.RunAndForget), null);
                        }

                        using (var brush = GraphicsEngine.Current.Engine.CreateSolidBrush(_backgroundColor))
                        {
                            _canvas.FillRectangle(brush, new GraphicsEngine.CanvasRectangle(0, 0, iWidth, iHeight));
                        }
                    }

                    #endregion

                    #region Geometry

                    if (Bit.Has(phase, DrawPhase.Geography))
                    //if (phase == DrawPhase.All || phase == DrawPhase.Geography)
                    {
                        LabelEngine.Init(this.Display, printerMap);

                        this.GeometricTransformer = geoTransformer;

                        // Thread für MapServer Datasets starten...
                        #region WebServiceLayer
                        List<IWebServiceLayer> webServices;
                        if (this.TOC != null)
                        {
                            webServices = ListOperations<IWebServiceLayer>.Swap(this.TOC.VisibleWebServiceLayers);
                        }
                        else
                        {
                            webServices = new List<IWebServiceLayer>();
                            foreach (IDatasetElement layer in this.MapElements)
                            {
                                if (!(layer is IWebServiceLayer))
                                {
                                    continue;
                                }

                                if (((ILayer)layer).Visible)
                                {
                                    webServices.Add((IWebServiceLayer)layer);
                                }
                            }
                        }
                        int webServiceOrder = 0;
                        foreach (IWebServiceLayer element in webServices)
                        {
                            if (!element.Visible)
                            {
                                continue;
                            }

                            RenderServiceRequest srt = new RenderServiceRequest(this, element, webServiceOrder++);
                            srt.finish += new RenderServiceRequest.RequestThreadFinished(MapRequestThread_finished);
                            //Thread thread = new Thread(new ThreadStart(srt.ImageRequest));
                            m_imageMerger.max++;
                            //thread.Start();
                            var task = srt.ImageRequest();  // start the task...
                        }
                        #endregion

                        #region Layerlisten erstellen
                        List<ILayer> layers;
                        if (this.TOC != null)
                        {
                            if (this.ToString() == "gView.MapServer.Instance.ServiceMap")
                            {
                                layers = ListOperations<ILayer>.Swap(this.TOC.Layers);
                            }
                            else
                            {
                                layers = ListOperations<ILayer>.Swap(this.TOC.VisibleLayers);
                            }
                        }
                        else
                        {
                            layers = new List<ILayer>();
                            foreach (IDatasetElement layer in this.MapElements)
                            {
                                if (!(layer is ILayer))
                                {
                                    continue;
                                }

                                if (((ILayer)layer).Visible)
                                {
                                    layers.Add((ILayer)layer);
                                }
                            }
                        }

                        List<IFeatureLayer> labelLayers = this.OrderedLabelLayers(layers);

                        #endregion

                        #region Renderer Features

                        foreach (ILayer layer in layers)
                        {
                            if (!cancelTracker.Continue)
                            {
                                break;
                            }

                            if (!layer.RenderInScale(this))
                            {
                                continue;
                            }

                            SetGeotransformer(layer, geoTransformer);

                            DateTime startTime = DateTime.Now;

                            FeatureCounter fCounter = new FeatureCounter();
                            if (layer is IFeatureLayer)
                            {

                                if (layer.Class?.Dataset is IFeatureCacheDataset)
                                {
                                    await ((IFeatureCacheDataset)layer.Class.Dataset).InitFeatureCache(datasetCachingContext);
                                }

                                IFeatureLayer fLayer = (IFeatureLayer)layer;
                                if (fLayer.FeatureRenderer == null &&
                                    (
                                     fLayer.LabelRenderer == null ||
                                    (fLayer.LabelRenderer != null && fLayer.LabelRenderer.RenderMode != LabelRenderMode.RenderWithFeature)
                                    ))
                                {
                                    //continue;
                                }
                                else
                                {
                                    RenderFeatureLayer rlt = new RenderFeatureLayer(this, datasetCachingContext, fLayer, cancelTracker, fCounter);
                                    if (fLayer.LabelRenderer != null && fLayer.LabelRenderer.RenderMode == LabelRenderMode.RenderWithFeature)
                                    {
                                        rlt.UseLabelRenderer = fLayer.LabelInScale(this); // true;
                                    }
                                    else
                                    {
                                        rlt.UseLabelRenderer = labelLayers.IndexOf(fLayer) == 0;  // letzten Layer gleich mitlabeln
                                    }

                                    if (rlt.UseLabelRenderer)
                                    {
                                        labelLayers.Remove(fLayer);
                                    }

                                    if (cancelTracker.Continue)
                                    {
                                        DrawingLayer?.BeginInvoke(layer.Title, new AsyncCallback(AsyncInvoke.RunAndForget), null);
                                    }

                                    await rlt.Render();
                                }
                            }
                            if (layer is IRasterLayer && ((IRasterLayer)layer).RasterClass != null)
                            {
                                IRasterLayer rLayer = (IRasterLayer)layer;
                                if (rLayer.RasterClass.Polygon == null)
                                {
                                    continue;
                                }

                                IEnvelope dispEnvelope = this.DisplayTransformation.TransformedBounds(this); //this.Envelope;
                                if (Display.GeometricTransformer != null)
                                {
                                    dispEnvelope = ((IGeometry)Display.GeometricTransformer.InvTransform2D(dispEnvelope)).Envelope;
                                }

                                if (gView.Framework.SpatialAlgorithms.Algorithm.IntersectBox(rLayer.RasterClass.Polygon, dispEnvelope))
                                {
                                    if (rLayer.Class is IParentRasterLayer)
                                    {
                                        if (cancelTracker.Continue)
                                        {
                                            DrawingLayer?.BeginInvoke(layer.Title, new AsyncCallback(AsyncInvoke.RunAndForget), null);
                                        }

                                        await DrawRasterParentLayer((IParentRasterLayer)rLayer.Class, cancelTracker, rLayer);
                                    }
                                    else
                                    {
                                        RenderRasterLayer rlt = new RenderRasterLayer(this, rLayer, rLayer, cancelTracker);

                                        if (cancelTracker.Continue)
                                        {
                                            DrawingLayer?.BeginInvoke(layer.Title, new AsyncCallback(AsyncInvoke.RunAndForget), null);
                                        }

                                        await rlt.Render();
                                    }
                                }
                            }
                            // Andere Layer (zB IRasterLayer)

                            _original.FireDrawingLayerFinished(new gView.Framework.system.TimeEvent("Drawing: " + layer.Title, startTime, DateTime.Now, fCounter.Counter));

                            FireRefreshMapView(1000);
                        }
                        #endregion

                        #region Label Features

                        if (labelLayers.Count != 0)
                        {
                            StreamImage(ref _msGeometry, _bitmap);
                            foreach (IFeatureLayer fLayer in labelLayers)
                            {
                                this.SetGeotransformer(fLayer, geoTransformer);

                                FeatureCounter fCounter = new FeatureCounter();
                                DateTime startTime = DateTime.Now;

                                RenderLabel rlt = new RenderLabel(this, fLayer, cancelTracker, fCounter);

                                if (cancelTracker.Continue)
                                {
                                    DrawingLayer?.BeginInvoke(fLayer.Title, new AsyncCallback(AsyncInvoke.RunAndForget), null);
                                }

                                await rlt.Render();

                                _original.FireDrawingLayerFinished(new gView.Framework.system.TimeEvent("Labelling: " + fLayer.Title, startTime, DateTime.Now, fCounter.Counter));

                            }

                            DrawStream(_canvas, _msGeometry);
                        }

                        if (!printerMap)
                        {
                            LabelEngine.Draw(this.Display, cancelTracker);
                        }

                        LabelEngine.Release();

                        #endregion

                        #region Waiting for Webservices

                        if (cancelTracker.Continue)
                        {
                            if (webServices != null && webServices.Count != 0)
                            {
                                DrawingLayer?.BeginInvoke("...Waiting for WebServices...", new AsyncCallback(AsyncInvoke.RunAndForget), null);
                            }

                            while (m_imageMerger.Count < m_imageMerger.max)
                            {
                                await Task.Delay(100);
                            }
                        }
                        if (_drawScaleBar)
                        {
                            m_imageMerger.mapScale = this.mapScale;
                            m_imageMerger.dpi = this.dpi;
                        }
                        if (m_imageMerger.Count > 0)
                        {
                            var clonedBitmap = _bitmap.Clone(GraphicsEngine.PixelFormat.Rgba32);
                            clonedBitmap.MakeTransparent(_backgroundColor);
                            m_imageMerger.Add(new GeorefBitmap(clonedBitmap), 999);

                            if (!m_imageMerger.Merge(_bitmap, this.Display) &&
                                (this is IServiceMap) &&
                                ((IServiceMap)this).MapServer != null)
                            {
                                await ((IServiceMap)this).MapServer.LogAsync(
                                    this.Name,
                                    "Image Merger:",
                                    loggingMethod.error,
                                    m_imageMerger.LastErrorMessage);
                            }
                            m_imageMerger.Clear();
                        }

                        StreamImage(ref _msGeometry, _bitmap);

                        #endregion
                    }
                    #endregion

                    #region Draw Selection

                    if (Bit.Has(phase, DrawPhase.Selection))
                    {
                        if (phase != DrawPhase.All)
                        {
                            DrawStream(_canvas, _msGeometry);
                        }

                        foreach (IDatasetElement layer in this.MapElements)
                        {
                            if (!cancelTracker.Continue)
                            {
                                break;
                            }

                            if (!(layer is ILayer))
                            {
                                continue;
                            }

                            if (layer is IFeatureLayer &&
                                layer is IFeatureSelection &&
                                ((IFeatureSelection)layer).SelectionSet != null &&
                                ((IFeatureSelection)layer).SelectionSet.Count > 0)
                            {
                                SetGeotransformer((ILayer)layer, geoTransformer);
                                await RenderSelection(layer as IFeatureLayer, cancelTracker);
                            } // Andere Layer (zB IRasterLayer)
                            else if (layer is IWebServiceLayer)
                            {
                                IWebServiceLayer wLayer = (IWebServiceLayer)layer;
                                if (wLayer.WebServiceClass == null)
                                {
                                    continue;
                                }

                                foreach (IWebServiceTheme theme in wLayer.WebServiceClass.Themes)
                                {
                                    if (theme is IFeatureLayer &&
                                        theme.SelectionRenderer != null &&
                                        theme is IFeatureSelection &&
                                        ((IFeatureSelection)theme).SelectionSet != null &&
                                        ((IFeatureSelection)theme).SelectionSet.Count > 0)
                                    {
                                        SetGeotransformer(theme, geoTransformer);
                                        await RenderSelection(theme as IFeatureLayer, cancelTracker);
                                    }
                                }
                            }
                        }

                        StreamImage(ref _msSelection, _bitmap);
                    }

                    #endregion

                    #region Graphics

                    if (Bit.Has(phase, DrawPhase.Graphics))
                    //if (phase == DrawPhase.All || phase == DrawPhase.Graphics)
                    {
                        if (phase != DrawPhase.All)
                        {
                            DrawStream(_canvas, (_msSelection != null) ? _msSelection : _msGeometry);
                        }

                        foreach (IGraphicElement grElement in Display.GraphicsContainer.Elements)
                        {
                            grElement.Draw(Display);
                        }
                        foreach (IGraphicElement grElement in Display.GraphicsContainer.SelectedElements)
                        {
                            if (grElement is IGraphicElement2)
                            {
                                if (((IGraphicElement2)grElement).Ghost != null)
                                {
                                    ((IGraphicElement2)grElement).Ghost.Draw(Display);
                                } ((IGraphicElement2)grElement).DrawGrabbers(Display);
                            }
                        }
                    }

                    #endregion

                    #region Cleanup

                    if (geoTransformer != null)
                    {
                        this.GeometricTransformer = null;
                        geoTransformer.Release();
                        geoTransformer = null;
                    }

                    #endregion

                    #region Send Events

                    // Überprüfen, ob sich Extent seit dem letztem Zeichnen geändert hat...
                    if (cancelTracker.Continue)
                    {
                        if (_lastRenderExtent == null)
                        {
                            _lastRenderExtent = new Envelope();
                        }

                        if (!_lastRenderExtent.Equals(Display.Envelope))
                        {
                            _original.FireNewExtentRendered();
                        }

                        _lastRenderExtent.minx = Display.Envelope.minx;
                        _lastRenderExtent.miny = Display.Envelope.miny;
                        _lastRenderExtent.maxx = Display.Envelope.maxx;
                        _lastRenderExtent.maxy = Display.Envelope.maxy;
                    }

                    #endregion

                    return true;
                }
            }
            catch (Exception ex)
            {
                _lastException = ex;
                AddRequestException(ex);
                //System.Windows.Forms.MessageBox.Show(ex.Message+"\n"+ex.InnerException+"\n"+ex.Source);
                return false;
            }
            finally
            {
                AppendRequestExceptionsToImage();

                if (!printerMap)
                {
                    if (_canvas != null)
                    {
                        _canvas.Dispose();
                    }

                    _canvas = null;
                }

                this.IsRefreshing = false;
            }
        }

        public override void HighlightGeometry(IGeometry geometry, int milliseconds)
        {
            if (geometry == null || _canvas != null)
            {
                return;
            }

            GeometryType type = GeometryType.Unknown;
            if (geometry is IPolygon)
            {
                type = GeometryType.Polygon;
            }
            else if (geometry is IPolyline)
            {
                type = GeometryType.Polyline;
            }
            else if (geometry is IPoint)
            {
                type = GeometryType.Point;
            }
            else if (geometry is IMultiPoint)
            {
                type = GeometryType.Multipoint;
            }
            else if (geometry is IEnvelope)
            {
                type = GeometryType.Envelope;
            }
            if (type == GeometryType.Unknown)
            {
                return;
            }

            ISymbol symbol = null;
            PlugInManager compMan = new PlugInManager();
            IFeatureRenderer renderer = compMan.CreateInstance(gView.Framework.system.KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer;
            if (renderer is ISymbolCreator)
            {
                symbol = ((ISymbolCreator)renderer).CreateStandardHighlightSymbol(type);
            }
            if (symbol == null)
            {
                return;
            }

            try
            {
                using (var bm = GraphicsEngine.Current.Engine.CreateBitmap(Display.iWidth, Display.iHeight, GraphicsEngine.PixelFormat.Rgba32))
                using (_canvas = bm.CreateCanvas())
                {
                    DrawStream(_canvas, _msGeometry);
                    DrawStream(_canvas, _msSelection);

                    this.Draw(symbol, geometry);
                    NewBitmap?.Invoke(bm); //.BeginInvoke(bm, new AsyncCallback(AsyncInvoke.RunAndForget), null);

                    DoRefreshMapView?.Invoke();

                    Thread.Sleep(milliseconds);

                    _canvas.Clear();
                    DrawStream(_canvas, _msGeometry);
                    DrawStream(_canvas, _msSelection);

                    //NewBitmap?.Invoke(bm); //.BeginInvoke(_bitmap, new AsyncCallback(AsyncInvoke.RunAndForget), null);

                    DoRefreshMapView?.Invoke();
                }
            }
            finally
            {
                _canvas = null;
            }
        }

        private DateTime _lastRefresh = DateTime.UtcNow;
        internal override void FireRefreshMapView(double suppressPeriode = 500)
        {
            if (this.DoRefreshMapView != null)
            {
                if ((DateTime.UtcNow - _lastRefresh).TotalMilliseconds > suppressPeriode)
                {
                    this.DoRefreshMapView();
                    _lastRefresh = DateTime.UtcNow;
                }
            }
        }

        #endregion

        #region Private Members

        private void DrawStream(GraphicsEngine.Abstraction.ICanvas canvas, MemoryStream stream)
        {
            if (stream == null || canvas == null)
            {
                return;
            }

            try
            {
                using (var ms = new MemoryStream(stream.ToArray())) // Clone Stream => Skia disposes stream automatically
                using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(ms))
                {
                    canvas.DrawBitmap(bitmap, new GraphicsEngine.CanvasPoint(0, 0));
                }
            }
            catch
            {
            }
        }

        private void StreamImage(ref MemoryStream stream, GraphicsEngine.Abstraction.IBitmap bitmap)
        {
            try
            {
                if (bitmap == null)
                {
                    return;
                }

                if (stream != null)
                {
                    stream.Dispose();
                }

                stream = new MemoryStream();
                bitmap.Save(stream, GraphicsEngine.ImageFormat.Png);
            }
            catch (Exception)
            {
            }
        }

        async private Task RenderSelection(IFeatureLayer fLayer, ICancelTracker cancelTracker)
        {
            if (fLayer == null || !(fLayer is IFeatureSelection))
            {
                return;
            }

            if (fLayer.SelectionRenderer == null)
            {
                return;
            }

            IFeatureSelection fSelection = (IFeatureSelection)fLayer;
            if (fSelection.SelectionSet == null || fSelection.SelectionSet.Count == 0)
            {
                return;
            }

            RenderFeatureLayerSelection rlt = new RenderFeatureLayerSelection(this, fLayer, cancelTracker);
            //rlt.Render();

            //Thread thread = new Thread(new ThreadStart(rlt.Render));
            //thread.Start();

            DrawingLayer.BeginInvoke(fLayer.Title, new AsyncCallback(AsyncInvoke.RunAndForget), null);

            await rlt.Render();
            //while (thread.IsAlive)
            //{
            //    Thread.Sleep(10);
            //    if (DoRefreshMapView != null && (count % 100) == 0 && cancelTracker.Continue)
            //    {
            //        DoRefreshMapView();
            //    }
            //    count++;
            //}
            if (cancelTracker.Continue)
            {
                DoRefreshMapView?.Invoke();
            }
        }

        #endregion

        #region Disposing

        public override void Dispose()
        {
            if (_original != null)
            {
                _original = null;
            }

            base.Dispose();
        }

        internal bool DisposeImage()
        {
            if (_bitmap != null)
            {
                if (_canvas != null)
                {
                    return false;  // irgendwas tut sich noch
                    //lock (_canvas)
                    //{
                    //    _canvas.Dispose();
                    //    _canvas = null;
                    //}
                }
                NewBitmap?.BeginInvoke(null, new AsyncCallback(AsyncInvoke.RunAndForget), null);

                _bitmap.Dispose();
                _bitmap = null;

                //DisposeStreams();
            }
            return true;
        }

        private void DisposeStreams()
        {
            if (_msGeometry != null)
            {
                _msGeometry.Dispose();
                _msGeometry = null;
            }
            if (_msSelection != null)
            {
                _msSelection.Dispose();
                _msSelection = null;
            }
        }

        public bool DisposeGraphics()
        {
            if (_canvas == null)
            {
                return true;
            }

            lock (_canvas)
            {
                _canvas.Dispose();
                _canvas = null;
            }
            return true;
        }

        #endregion
    }
}
