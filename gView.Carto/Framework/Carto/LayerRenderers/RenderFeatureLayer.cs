using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.system.Diagnostics;
using gView.GraphicsEngine.Extensions;
using System;
using System.Threading.Tasks;

namespace gView.Framework.Carto.LayerRenderers
{
    public sealed class RenderFeatureLayer
    {
        private Map _map;
        private IDatasetCachingContext _datasetCachingContext;
        private IFeatureLayer _layer;
        private ICancelTracker _cancelTracker;
        private bool _useLabelRenderer = false;
        private FeatureCounter _counter;
        private bool _isServiceMap = false;

        public RenderFeatureLayer(Map map, IDatasetCachingContext datasetCachingContext, IFeatureLayer layer, ICancelTracker cancelTracker, FeatureCounter counter)
        {
            _map = map;
            _datasetCachingContext = datasetCachingContext;
            _isServiceMap = map is IServiceMap;
            _layer = layer;
            _cancelTracker = ((cancelTracker == null) ? new CancelTracker() : cancelTracker);
            _counter = counter;
            _counter.Counter = 0;
        }

        public bool UseLabelRenderer
        {
            get { return _useLabelRenderer; }
            set { _useLabelRenderer = value; }
        }

        async public Task Render()
        {
            if (_layer == null || _map == null)
            {
                return;
            }

            #region JSON FilterCollection

            if (_layer.FilterQuery != null && !String.IsNullOrEmpty(_layer.FilterQuery.JsonWhereClause))
            {
                try
                {
                    DisplayFilterCollection dfc = DisplayFilterCollection.FromJSON(_layer.FilterQuery.JsonWhereClause);
                    if (dfc == null)
                    {
                        return;
                    }

                    IFeatureLayer flayer = (IFeatureLayer)LayerFactory.Create(_layer.Class, _layer);
                    flayer.FilterQuery = (IQueryFilter)_layer.FilterQuery.Clone();
                    foreach (DisplayFilter df in dfc)
                    {
                        // immer neu klonen (wegen PenWidth...)!!
                        flayer.FeatureRenderer = _layer.FeatureRenderer != null ? (IFeatureRenderer)_layer.FeatureRenderer.Clone() : null;

                        flayer.FilterQuery.WhereClause = df.Filter;
                        if (flayer.FeatureRenderer != null)
                        {
                            foreach (ISymbol symbol in flayer.FeatureRenderer.Symbols)
                            {
                                if (df.Color.A > 0)
                                {
                                    if (symbol is IPenColor)
                                    {
                                        ((IPenColor)symbol).PenColor = df.Color;
                                    }

                                    if (symbol is IBrushColor)
                                    {
                                        ((IBrushColor)symbol).FillColor = df.Color;
                                    }

                                    if (symbol is IFontColor)
                                    {
                                        ((IFontColor)symbol).FontColor = df.Color;
                                    }
                                }
                                if (df.PenWidth > 0f && symbol is IPenWidth)
                                {
                                    ((IPenWidth)symbol).PenWidth = df.PenWidth;
                                }
                            }
                        }
                        await Render(flayer);
                    }
                }
                catch { }
                return;
            }

            #endregion

            await Render(_layer);
        }
        async private Task Render(IFeatureLayer layer)
        {
            IFeatureRenderer clonedFeatureRenderer = null;
            ILabelRenderer clonedLabelRenderer = null;

            GraphicsEngine.Abstraction.IBitmap compositionModeCopyBitmap = null;
            GraphicsEngine.Abstraction.ICanvas compositionModeCopyCanvas = null, originalCanvas = null;

            try
            {
                _map.FireOnUserInterface(true);
                if ((
                    layer.FeatureRenderer == null ||
                    layer.FeatureRenderer.HasEffect(layer, _map) == false)
                    &&
                    (
                    layer.LabelRenderer == null ||
                    _useLabelRenderer == false
                    ))
                {
                    return;
                }

                IFeatureClass fClass = layer.FeatureClass;
                if (fClass == null)
                {
                    return;
                }

                //IDataset dataset = (IDataset)_map[layer];
                //if (dataset == null) return;

                //if (!(dataset is IFeatureDataset)) return;

                IGeometry filterGeom = _map.Display.DisplayTransformation.TransformedBounds(_map.Display); //_map.Display.Envelope;

                if (_map.Display.GeometricTransformer != null)
                {
                    filterGeom = MapHelper.Project(fClass, _map.Display);
                }

                var filter = new SpatialFilter();
                filter.DatasetCachingContext = _datasetCachingContext;
                filter.Geometry = filterGeom;
                filter.AddField(fClass.ShapeFieldName);
                //filter.FuzzyQuery = true;
                filter.SpatialRelation = spatialRelation.SpatialRelationMapEnvelopeIntersects;
                filter.MapScale = _map.Display.MapScale;
                filter.CancelTracker = _cancelTracker;

                if (layer.FilterQuery != null)
                {
                    filter.WhereClause = layer.FilterQuery.WhereClause;
                    if (layer.FilterQuery is IBufferQueryFilter)
                    {
                        ISpatialFilter sFilter = await BufferQueryFilter.ConvertToSpatialFilter(layer.FilterQuery as IBufferQueryFilter);
                        if (sFilter == null)
                        {
                            return;
                        }
                        filter.SpatialRelation = spatialRelation.SpatialRelationIntersects;
                        filter.Geometry = sFilter.Geometry;
                    }
                    if (layer.FilterQuery is ISpatialFilter)
                    {
                        //filter.FuzzyQuery = ((ISpatialFilter)layer.FilterQuery).FuzzyQuery;
                        filter.SpatialRelation = ((ISpatialFilter)layer.FilterQuery).SpatialRelation;
                        filter.Geometry = ((ISpatialFilter)layer.FilterQuery).Geometry;
                    }
                }

                // Erst nach dem Clonen anwenden!!!
                //if (layer.FeatureRenderer != null && layer.FeatureRenderer.HasEffect(layer, _map))
                //{
                //    layer.FeatureRenderer.PrepareQueryFilter(layer, filter);
                //}
                //if (layer.LabelRenderer != null && _useLabelRenderer)
                //{
                //    layer.LabelRenderer.PrepareQueryFilter(_map.Display, layer, filter);
                //}

                IDisplay display = _map;
                double refScale = display.ReferenceScale;

                #region Layer Clonen

                IFeatureRenderer renderer = null;
                ILabelRenderer labelRenderer = null;

                GraphicsEngine.Current.Engine?.CloneObjectsLocker.InterLock(() =>
                {
                    // Beim Clonen sprerren... GDI+
                    // Da sonst bei der Servicemap bei gleichzeitigen Requests
                    // Exception "Objekt wird bereits an anderer Stelle verwendet" auftreten kann!
                    if (layer.FeatureRenderer != null && layer.FeatureRenderer.HasEffect(layer, _map))
                    {
                        if (layer.RequiresFeatureRendererClone(display))
                        {
                            renderer = clonedFeatureRenderer = (IFeatureRenderer)layer.FeatureRenderer.Clone(
                                new CloneOptions(display,
                                                 layer.UseWithRefScale(display),
                                                 maxRefScaleFactor: layer.MaxRefScaleFactor));
                        }
                        else
                        {
                            renderer = layer.FeatureRenderer;
                        }
                    }
                    if (layer.LabelRenderer != null && _useLabelRenderer)
                    {
                        if (layer.RequiresLabelRendererClone(display))
                        {
                            labelRenderer = clonedLabelRenderer =
                                (ILabelRenderer)layer.LabelRenderer.Clone(new CloneOptions(display,
                                                                                           layer.UseLabelsWithRefScale(display),
                                                                                           maxLabelRefscaleFactor: layer.MaxLabelRefScaleFactor));
                        }
                        else  // Clone with null => simple clone
                        {
                            //display.refScale = 0;
                            labelRenderer = clonedLabelRenderer = (ILabelRenderer)layer.LabelRenderer.Clone(null);
                            //display.refScale = refScale;
                        }
                    }
                });

                #endregion

                #region Prepare filter

                // Prepare erst auf geclonte renderer anwenden!! (Threadsafe)
                if (renderer != null && renderer.HasEffect(layer, _map))
                {
                    renderer.PrepareQueryFilter(layer, filter);
                }
                if (labelRenderer != null && _useLabelRenderer)
                {
                    labelRenderer.PrepareQueryFilter(_map.Display, layer, filter);
                }

                #endregion

                using (IFeatureCursor fCursor = await fClass.GetFeatures(MapHelper.MapQueryFilter(filter)))
                {
                    _map.FireOnUserInterface(false);

                    if (fCursor != null)
                    {
                        IFeature feature;

                        if (renderer != null)
                        {
                            renderer.StartDrawing(_map);

                            bool useCompostionModeCopy = layer is IFeatureLayerComposition &&
                                                         ((IFeatureLayerComposition)layer).CompositionMode == FeatureLayerCompositionMode.Copy;

                            if (useCompostionModeCopy)
                            {
                                originalCanvas = _map.Display.Canvas;
                                compositionModeCopyBitmap = GraphicsEngine.Current.Engine.CreateBitmap(_map.Display.Bitmap.Width, _map.Display.Bitmap.Height, GraphicsEngine.PixelFormat.Rgba32);
                                compositionModeCopyCanvas = compositionModeCopyBitmap.CreateCanvas();

                                compositionModeCopyBitmap.MakeTransparent();
                                compositionModeCopyBitmap.SetResolution(_map.Display.Bitmap.DpiX,
                                                                        _map.Display.Bitmap.DpiY);

                                ((Display)_map.Display).Canvas = compositionModeCopyCanvas;
                            }

                            while ((feature = await fCursor.NextFeature()) != null)
                            {
                                if (_cancelTracker != null)
                                {
                                    if (!_cancelTracker.Continue)
                                    {
                                        break;
                                    }
                                }

                                renderer.Draw(_map, feature);

                                if (labelRenderer != null)
                                {
                                    labelRenderer.Draw(_map, feature);
                                }

                                _counter.Counter++;

                                if (_isServiceMap == false)
                                {
                                    if (_counter.Counter % 100 == 0)
                                    {
                                        _map.FireRefreshMapView();
                                    }
                                }
                            }
                        }
                        else if (labelRenderer != null && _cancelTracker.Continue)
                        {
                            while ((feature = await fCursor.NextFeature()) != null)
                            {
                                if (_cancelTracker != null)
                                {
                                    if (!_cancelTracker.Continue)
                                    {
                                        break;
                                    }
                                }

                                labelRenderer.Draw(_map, feature);
                                _counter.Counter++;
                            }
                        }

                        if (labelRenderer != null)
                        {
                            labelRenderer.Release();
                        }

                        if (renderer != null)
                        {
                            renderer.FinishDrawing(_map, _cancelTracker);
                        }

                        if (compositionModeCopyCanvas != null && compositionModeCopyBitmap != null)
                        {
                            originalCanvas.DrawBitmap(compositionModeCopyBitmap,
                                new GraphicsEngine.CanvasRectangle(0, 0, compositionModeCopyBitmap.Width, compositionModeCopyBitmap.Height),
                                new GraphicsEngine.CanvasRectangle(0, 0, compositionModeCopyBitmap.Width, compositionModeCopyBitmap.Height),
                                opacity: (float)Math.Min(1, (100f - ((IFeatureLayerComposition)layer).CompositionModeCopyTransparency) / 100));
                        }

                        if(SystemVariables.UseDiagnostic)
                        {
                            if(fCursor is IDiagnostics)
                            {
                                var metrics = (_map as IServiceMap)?.Metrics;
                                if(metrics!=null)
                                {
                                    metrics[_layer.Title] = ((IDiagnostics)fCursor).DiagnosticParameters?.IdleMilliseconds ?? 0D;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (fClass is IDebugging && ((IDebugging)fClass).LastException != null)
                        {
                            throw ((IDebugging)fClass).LastException;
                        }

                        throw new Exception("Can't query feature class. Unknown error");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_map is IServiceMap && ((IServiceMap)_map).MapServer != null)
                {
                    await ((IServiceMap)_map).MapServer.LogAsync(
                        ((IServiceMap)_map).Name,
                        "RenderFeatureLayer: " + ((layer != null) ? layer.Title : String.Empty),
                        loggingMethod.error,
                        ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
                }
                if (_map != null)
                {
                    _map.AddRequestException(new Exception("RenderFeatureLayerThread: " + ((layer != null) ? layer.Title : String.Empty) + "\n" + ex.Message, ex));
                }
            }
            finally
            {
                if (clonedFeatureRenderer != null)
                {
                    clonedFeatureRenderer.Release();
                }

                if (clonedLabelRenderer != null)
                {
                    clonedLabelRenderer.Release();
                }

                if (originalCanvas != null)
                {
                    ((Display)_map.Display).Canvas = originalCanvas;
                }

                if (compositionModeCopyCanvas != null)
                {
                    compositionModeCopyCanvas.Dispose();
                    compositionModeCopyCanvas = null;
                }

                if (compositionModeCopyBitmap != null)
                {
                    compositionModeCopyBitmap.Dispose();
                    compositionModeCopyBitmap = null;
                }

                _map.FireOnUserInterface(false);
            }
        }
    }
}
