using gView.Data.Framework.Data;
using gView.Data.Framework.Data.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.LayerRenderers;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Server.AppCode
{
    public class ServiceMap : Map, IServiceMap, IDisposable
    {
        private IMapServer _mapServer = null;
        private IServiceRequestInterpreter _interpreter = null;
        private ServiceRequest _request = null;
        private IEnumerable<IMapApplicationModule> _modules = null;

        //private bool _ceckLayerVisibilityBeforeDrawing;

        private ServiceMap() { }

        async static public Task<ServiceMap> CreateAsync(Map original, IMapServer mapServer, IEnumerable<IMapApplicationModule> modules)
        {
            var serviceMap = new ServiceMap();

            serviceMap._mapServer = mapServer;
            serviceMap._modules = modules;
            serviceMap._layers = original._layers;
            serviceMap._datasets = original._datasets;

            serviceMap.m_imageMerger = new ImageMerger2();

            serviceMap.m_name = original.Name;
            serviceMap._toc = original._toc;
            serviceMap.Title = original.Title;
            //serviceMap._ceckLayerVisibilityBeforeDrawing = true;
            serviceMap._mapUnits = original.MapUnits;
            serviceMap._displayUnits = original.DisplayUnits;
            serviceMap._backgroundColor = original.BackgroundColor;
            serviceMap.ReferenceScale = original.ReferenceScale;

            serviceMap.SpatialReference = original.Display.SpatialReference;
            serviceMap.LayerDefaultSpatialReference = original.LayerDefaultSpatialReference != null ? original.LayerDefaultSpatialReference.Clone() as ISpatialReference : null;

            serviceMap._drawScaleBar = false;

            // Metadata
            await serviceMap.SetMetadataProviders(await original.GetMetadataProviders());
            serviceMap._debug = false;

            serviceMap._layerDescriptions = original.LayerDescriptions;
            serviceMap._layerCopyrightTexts = original.LayerCopyrightTexts;

            serviceMap.SetResourceContainer(original.ResourceContainer);

            return serviceMap;
        }

        async public Task<int> SaveImage(string path, ImageFormat format)
        {
            if (_bitmap == null)
            {
                return -1;
            }

            try
            {
                try
                {
                    if (Display.MakeTransparent &&
                        format != ImageFormat.Jpeg &&
                        format != ImageFormat.Gif &&
                        _bitmap.PixelFormat != PixelFormat.Rgba32)  // dont make this transparent, this should be transparent from beginning !!!
                    {
                        _bitmap.MakeTransparent(Display.TransparentColor);
                    }
                }
                catch (Exception ex)
                {
                    if (this.MapServer != null)
                    {
                        await this.MapServer.LogAsync(
                            this.Name,
                            "RenderRasterLayerThread", loggingMethod.error,
                            "Image.MakeTransparent\nPath='" + path + "'\nFormat=" + format.ToString() + "\n" +
                            ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
                    }
                }
                //_image.Save(path, format);
                int size = await _bitmap.SaveOrUpload(path, format);
                _bitmap.Dispose();
                _bitmap = null;
                return size;
            }
            catch (Exception ex)
            {
                if (this.MapServer != null)
                {
                    await this.MapServer.LogAsync(
                        this.Name,
                        "RenderRasterLayerThread", loggingMethod.error,
                        "Image.Save\nPath='" + path + "'\nFormat=" + format.ToString() + "\n" +
                        ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
                }
                //return false;

                throw;
            }
        }

        async public Task<int> SaveImage(Stream ms, ImageFormat format)
        {
            if (_bitmap == null)
            {
                return -1;
            }

            try
            {
                try
                {
                    if (Display.MakeTransparent &&
                        format != ImageFormat.Jpeg &&
                        format != ImageFormat.Gif &&
                        _bitmap.PixelFormat != PixelFormat.Rgba32)   // dont make this transparent, this should be transparent from beginning !!!
                    {
                        _bitmap.MakeTransparent(Display.TransparentColor);
                    }
                }
                catch (Exception ex)
                {
                    if (this.MapServer != null)
                    {
                        await this.MapServer.LogAsync(
                            this.Name,
                            "RenderRasterLayerThread", loggingMethod.error,
                            "Image.MakeTransparent\n'\nFormat=" + format.ToString() + "\n" +
                            ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
                    }
                }

                if (_canvas != null)
                {
                    _canvas.Flush();
                }

                _bitmap.Save(ms, format);
                _bitmap.Dispose();
                _bitmap = null;
                return (int)ms.Length;
            }
            catch (Exception ex)
            {
                if (this.MapServer != null)
                {
                    await this.MapServer.LogAsync(
                        this.Name,
                        "RenderRasterLayerThread", loggingMethod.error,
                        "Image.Save\n'\nFormat=" + format.ToString() + "\n" +
                        ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
                }
                return -1;
            }
        }

        public void ReleaseImage()
        {
            if (_bitmap == null)
            {
                return;
            }

            try
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            catch { }
        }

        public IBitmap MapImage
        {
            get { return _bitmap; }
        }
        async public Task<IBitmap> Legend()
        {
            IToc toc = _toc.Clone(this) as IToc;

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
            foreach (IWebServiceLayer element in webServices)
            {
                if (!(element is ILayer))
                {
                    continue;
                }

                if (!element.Visible)
                {
                    continue;
                }

                IWebServiceLayer wsLayer = LayerFactory.Create(element.WebServiceClass.Clone() as IClass, element) as IWebServiceLayer;
                if (wsLayer == null || wsLayer.WebServiceClass == null)
                {
                    continue;
                }

                if (BeforeRenderLayers != null)
                {
                    // layer im geklonten TOC austauschen...
                    // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                    // verwendet werden kann zB bei gesplitteten Layern...
                    //ITOCElement tocElement = toc.GetTOCElement(element.Class);
                    ITocElement tocElement = toc.GetTOCElement(element);
                    tocElement.RemoveLayer(element);
                    tocElement.AddLayer(wsLayer);

                    List<ILayer> modLayers = new List<ILayer>();
                    foreach (IWebServiceTheme theme in wsLayer.WebServiceClass.Themes)
                    {
                        if (theme is ILayer)
                        {
                            modLayers.Add(theme);
                        }
                    }
                    BeforeRenderLayers(this, modLayers);
                }
            }
            #endregion

            List<ILayer> layers = new List<ILayer>();
            if (this.TOC != null)
            {
                if (this.GetType().Equals(typeof(ServiceMap)))
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

            if (BeforeRenderLayers != null)
            {
                //
                // Kopie der Original Layer erstellen
                // ACHTUNG: Renderer werden nicht kopiert!
                // dürfen in BeforeRenderLayers nicht verändert werden...
                // Eine zuweisung eines neuen Renderers ist jedoch legitim.
                //
                List<ILayer> modLayers = new List<ILayer>();
                foreach (IDatasetElement element in layers)
                {
                    if (!(element is ILayer) || element is IWebServiceTheme)
                    {
                        continue;
                    }

                    ILayer layer = (ILayer)element;
                    if (layer.MinimumScale > 1 && layer.MinimumScale > this.MapScale)
                    {
                        continue;
                    }

                    if (layer.MaximumScale > 1 && layer.MaximumScale < this.MapScale)
                    {
                        continue;
                    }

                    ILayer newLayer = null;
                    modLayers.Add(newLayer = LayerFactory.Create(layer.Class, layer));

                    // layer im geklonten TOC austauschen...
                    if (element is ILayer && newLayer != null)
                    {
                        // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                        // verwendet werden kann zB bei gesplitteten Layern...
                        //ITOCElement tocElement = toc.GetTOCElement(layer.Class);
                        ITocElement tocElement = toc.GetTOCElement(layer);
                        tocElement.RemoveLayer(element as ILayer);
                        tocElement.AddLayer(newLayer);
                    }
                }
                BeforeRenderLayers(this, modLayers);
                layers = modLayers;
            }

            return await toc.Legend();
        }

        async override public Task<bool> RefreshMap(DrawPhase phase, ICancelTracker cancelTracker)
        {
            base.ResetRequestExceptions();

            if (_canvas != null && phase == DrawPhase.Graphics)
            {
                return true;
            }

            this.ZoomTo(m_actMinX, m_actMinY, m_actMaxX, m_actMaxY);

            if (cancelTracker == null)
            {
                cancelTracker = new CancelTracker();
            }

            using (var datasetCachingContext = new DatasetCachingContext(this))
            using (var geoTransformer = GeometricTransformerFactory.Create())
            {
                //geoTransformer.ToSpatialReference = this.SpatialReference;

                if (_bitmap == null)
                {
                    _bitmap = Current.Engine.CreateBitmap(ImageWidth, ImageHeight, PixelFormat.Rgba32);
                }

                _canvas = _bitmap.CreateCanvas();
                //_canvas.CompositingMode = CompositingMode.SourceCopy;
                //this.dpi = _canvas.DpiX * this.ScaleSymbolFactor;

                if (BackgroundColor.A != 0 && !Display.MakeTransparent)
                {
                    using (var brush = Current.Engine.CreateSolidBrush(BackgroundColor))
                    {
                        _canvas.FillRectangle(brush, new CanvasRectangle(0, 0, _bitmap.Width, _bitmap.Height));
                    }
                }

                if (phase == DrawPhase.All || phase == DrawPhase.Geography)
                {
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
                    int webServiceOrder = 0, webServiceOrder2 = 1;
                    foreach (IWebServiceLayer element in webServices)
                    {
                        if (!element.Visible)
                        {
                            continue;
                        }

                        IWebServiceLayer wsLayer = LayerFactory.Create(element.WebServiceClass.Clone() as IClass, element) as IWebServiceLayer;

                        if (wsLayer == null || wsLayer.WebServiceClass == null)
                        {
                            continue;
                        }

                        wsLayer.WebServiceClass.SpatialReference = this.SpatialReference;

                        List<IWebServiceClass> additionalWebServices = new List<IWebServiceClass>();
                        if (BeforeRenderLayers != null)
                        {
                            List<ILayer> modLayers = new List<ILayer>();
                            foreach (IWebServiceTheme theme in wsLayer.WebServiceClass.Themes)
                            {
                                if (theme is ILayer)
                                {
                                    modLayers.Add(theme);
                                }
                            }
                            BeforeRenderLayers(this, modLayers);

                            foreach (ILayer additionalLayer in MapServerHelper.FindAdditionalWebServiceLayers(wsLayer.WebServiceClass, modLayers))
                            {
                                IWebServiceClass additionalWebService = MapServerHelper.CloneNonVisibleWebServiceClass(wsLayer.WebServiceClass);
                                MapServerHelper.CopyWebThemeProperties(additionalWebService, additionalLayer);

                                if (MapServerHelper.HasVisibleThemes(additionalWebService))
                                {
                                    additionalWebServices.Add(additionalWebService);
                                }
                            }
                        }


                        var srt = new RenderServiceRequest(this, wsLayer, webServiceOrder++);
                        srt.finish += new RenderServiceRequest.RequestThreadFinished(MapRequestThread_finished);
                        //Thread thread = new Thread(new ThreadStart(srt.ImageRequest));
                        m_imageMerger.max++;
                        //thread.Start();
                        var task = srt.ImageRequest(); // start Task and continue...


                        foreach (IWebServiceClass additionalWebService in additionalWebServices)
                        {
                            wsLayer = LayerFactory.Create(additionalWebService, element) as IWebServiceLayer;
                            if (wsLayer == null || wsLayer.WebServiceClass == null)
                            {
                                continue;
                            }

                            wsLayer.WebServiceClass.SpatialReference = this.SpatialReference;

                            srt = new RenderServiceRequest(this, wsLayer, (++webServiceOrder2) + webServices.Count);
                            srt.finish += new RenderServiceRequest.RequestThreadFinished(MapRequestThread_finished);
                            //thread = new Thread(new ThreadStart(srt.ImageRequest));
                            m_imageMerger.max++;
                            //thread.Start();
                            var additionalTask = srt.ImageRequest(); // start task and continue...
                        }
                    }
                    #endregion

                    List<ILayer> layers = new List<ILayer>();
                    if (this.TOC != null)
                    {
                        if (this.GetType().Equals(typeof(ServiceMap)))
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

                    if (BeforeRenderLayers != null)
                    {
                        //
                        // Kopie der Original Layer erstellen
                        // ACHTUNG: Renderer werden nicht kopiert!
                        // dürfen in BeforeRenderLayers nicht verändert werden...
                        // Eine zuweisung eines neuen Renderers ist jedoch legitim.
                        //
                        List<ILayer> modLayers = new List<ILayer>();
                        foreach (IDatasetElement element in layers)
                        {
                            if (!(element is ILayer) || element is IWebServiceTheme)
                            {
                                continue;
                            }

                            ILayer layer = (ILayer)element;
                            if (layer.MinimumScale > 1 && layer.MinimumScale > this.MapScale)
                            {
                                continue;
                            }

                            if (layer.MaximumScale > 1 && layer.MaximumScale < this.MapScale)
                            {
                                continue;
                            }

                            modLayers.Add(LayerFactory.Create(layer.Class, layer));
                        }
                        BeforeRenderLayers(this, modLayers);
                        layers = modLayers;
                    }
                    //layers = ModifyLayerList(layers);
                    List<IFeatureLayer> labelLayers = this.OrderedLabelLayers(layers);

                    LabelEngine.Init(this.Display, false);
                    foreach (IDatasetElement element in layers)
                    {
                        if (!cancelTracker.Continue)
                        {
                            break;
                        }

                        if (!(element is ILayer))
                        {
                            continue;
                        }

                        ILayer layer = (ILayer)element;

                        //if (_ceckLayerVisibilityBeforeDrawing)
                        //{
                        //    if (!LayerIsVisible(layer)) continue;
                        //}
                        if (!layer.Visible)
                        {
                            continue;
                        }

                        if (!layer.RenderInScale(this))
                        {
                            continue;
                        }
#if (DEBUG)
                        //Logger.LogDebug("Drawing Layer:" + element.Title);
#endif
                        SetGeotransformer((ILayer)element, geoTransformer);

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
                                RenderFeatureLayer rlt = new RenderFeatureLayer(this, datasetCachingContext, fLayer, cancelTracker, new FeatureCounter());
                                if (fLayer.LabelRenderer != null && fLayer.LabelRenderer.RenderMode == LabelRenderMode.RenderWithFeature)
                                {
                                    rlt.UseLabelRenderer = true;
                                }
                                else
                                {
                                    rlt.UseLabelRenderer = labelLayers.IndexOf(fLayer) == 0;  // letzten Layer gleich mitlabeln
                                }

                                if (rlt.UseLabelRenderer)
                                {
                                    labelLayers.Remove(fLayer);
                                }

                                await rlt.Render();
                            }
                            //thread = new Thread(new ThreadStart(rlt.Render));
                            //thread.Start();
                        }
                        if (layer is IRasterLayer && ((IRasterLayer)layer).RasterClass != null)
                        {
                            IRasterLayer rLayer = (IRasterLayer)layer;
                            if (rLayer.RasterClass.Polygon == null)
                            {
                                continue;
                            }

                            IEnvelope dispEnvelope = this.Envelope;
                            if (Display.GeometricTransformer != null)
                            {
                                dispEnvelope = ((IGeometry)Display.GeometricTransformer.InvTransform2D(dispEnvelope)).Envelope;
                            }

                            if (gView.Framework.SpatialAlgorithms.Algorithm.IntersectBox(rLayer.RasterClass.Polygon, dispEnvelope))
                            {
                                if (rLayer.Class is IParentRasterLayer)
                                {
                                    await DrawRasterParentLayer((IParentRasterLayer)rLayer.Class, cancelTracker, rLayer);
                                }
                                else
                                {
                                    RenderRasterLayer rlt = new RenderRasterLayer(this, rLayer, rLayer, cancelTracker);
                                    await rlt.Render();

                                    //thread = new Thread(new ThreadStart(rlt.Render));
                                    //thread.Start();
                                }
                            }
                        }
                        // Andere Layer (zB IRasterLayer)

#if (DEBUG)
                        //Logger.LogDebug("Finished drawing layer: " + element.Title);
#endif
                    }

                    // Label Features
                    if (labelLayers.Count != 0)
                    {
                        foreach (IFeatureLayer fLayer in labelLayers)
                        {
                            this.SetGeotransformer(fLayer, geoTransformer);

                            if (!fLayer.Visible)
                            {
                                continue;
                            }

                            RenderLabel rlt = new RenderLabel(this, fLayer, cancelTracker, new FeatureCounter());
                            await rlt.Render();
                        }
                    }

                    LabelEngine.Draw(this.Display, cancelTracker);
                    LabelEngine.Release();

                    if (cancelTracker.Continue)
                    {
                        while (m_imageMerger.Count < m_imageMerger.max)
                        {
                            await Task.Delay(10);
                        }
                    }
                    if (_drawScaleBar)
                    {
                        m_imageMerger.mapScale = this.MapScale;
                        m_imageMerger.dpi = this.Dpi;
                    }
#if (DEBUG)
                    //Logger.LogDebug("Merge Images");
#endif
                    m_imageMerger.Merge(_bitmap, this.Display);
                    m_imageMerger.Clear();
#if (DEBUG)
                    //Logger.LogDebug("Merge Images Finished");
#endif
                }

                if (phase == DrawPhase.All || phase == DrawPhase.Graphics)
                {
                    foreach (IGraphicElement grElement in Display.GraphicsContainer.Elements)
                    {
                        grElement.Draw(Display);
                    }
                }

                base.AppendRequestExceptionsToImage();

                if (_canvas != null)
                {
                    _canvas.Dispose();
                }

                _canvas = null;

                this.GeometricTransformer = null;
            }

            return this.HasRequestExceptions == false;
        }

        async override protected Task DrawRasterParentLayer(IParentRasterLayer rLayer, ICancelTracker cancelTracker, IRasterLayer rootLayer)
        {
            IRasterPaintContext paintContext = null;

            try
            {
                if (rLayer is ILayer && ((ILayer)rLayer).Class is IRasterClass)
                {
                    paintContext = await ((IRasterClass)((ILayer)rLayer).Class).BeginPaint(this.Display, cancelTracker);
                }
                else if (rLayer is IRasterClass)
                {
                    paintContext = await ((IRasterClass)rLayer).BeginPaint(this.Display, cancelTracker);
                }
                string filterClause = String.Empty;
                if (rootLayer is IRasterCatalogLayer)
                {
                    filterClause = ((((IRasterCatalogLayer)rootLayer).FilterQuery != null) ?
                        ((IRasterCatalogLayer)rootLayer).FilterQuery.WhereClause : String.Empty);
                }

                using (IRasterLayerCursor cursor = await rLayer.ChildLayers(this, filterClause))
                {
                    ILayer child;

                    while ((child = await cursor.NextRasterLayer()) != null)
                    //foreach (ILayer child in ((IParentRasterLayer)rLayer).ChildLayers(this, filterClause))
                    {
                        if (!cancelTracker.Continue)
                        {
                            break;
                        }

                        if (child.Class is IParentRasterLayer)
                        {
                            await DrawRasterParentLayer((IParentRasterLayer)child.Class, cancelTracker, rootLayer);
                            continue;
                        }
                        if (!(child is IRasterLayer))
                        {
                            continue;
                        }

                        IRasterLayer cLayer = (IRasterLayer)child;

                        RenderRasterLayer rlt = new RenderRasterLayer(this, cLayer, rootLayer, cancelTracker);
                        await rlt.Render();

                        if (child.Class is IDisposable)
                        {
                            ((IDisposable)child.Class).Dispose();
                        }
                    }
                }
            }
            finally
            {
                if (paintContext != null)
                {
                    paintContext.Dispose();
                }
            }
        }

        #region IServiceMap Member

        public event BeforeRenderLayersEvent BeforeRenderLayers;

        async public Task<bool> Render()
        {
            return await RefreshMap(DrawPhase.All, null);
        }

        public void SetRequestContext(IServiceRequestContext context)
        {
            if (context != null)
            {
                _mapServer = context.MapServer;
                _interpreter = context.ServiceRequestInterpreter;
                _request = context.ServiceRequest;
                this.Metrics = context.Metrics;
            }
        }

        public IDictionary<string, double> Metrics { get; private set; }

        public T GetModule<T>()
        {
            if (_modules == null)
            {
                return default(T);
            }

            var module = _modules.Where(m => m.GetType().Equals(typeof(T))).FirstOrDefault();
            return (T)module;
        }

        #endregion

        #region IServiceRequestContext Member

        public IMapServer MapServer
        {
            get { return _mapServer; }
        }

        public IServiceRequestInterpreter ServiceRequestInterpreter
        {
            get { return _interpreter; }
        }

        public ServiceRequest ServiceRequest
        {
            get { return _request; }
        }

        Task<IServiceMap> IServiceRequestContext.CreateServiceMapInstance()
        {
            throw new NotImplementedException();
            //return this;
        }

        public Task<IMetadataProvider> GetMetadtaProviderAsync(Guid metadataProviderId)
        {
            return Task.FromResult(this.MetadataProvider(metadataProviderId));
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
