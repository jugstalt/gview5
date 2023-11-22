using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Events;
using System;
using System.Threading.Tasks;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("F3DF8F45-4BAC-49ee-82E6-E10711029648")]
    public class Zoom2Selection : gView.Framework.UI.ITool
    {
        IMapDocument _doc = null;
        #region ITool Member

        public object Image
        {
            get
            {
                Buttons b = new Buttons();
                return b.imageList1.Images[8];
            }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public string Name
        {
            get
            {
                return LocalizedResources.GetResString("Tools.ZoomToSelection", "Zoom To Selection");
            }
        }

        public bool Enabled
        {
            get
            {
                if (_doc == null)
                {
                    return false;
                }

                if (_doc.FocusMap == null)
                {
                    return false;
                }

                foreach (IDatasetElement layer in _doc.FocusMap.MapElements)
                {
                    if (layer is IWebServiceLayer && ((IWebServiceLayer)layer).WebServiceClass != null && ((IWebServiceLayer)layer).WebServiceClass.Themes != null)
                    {
                        foreach (IWebServiceTheme theme in ((IWebServiceLayer)layer).WebServiceClass.Themes)
                        {
                            if (!(theme is IFeatureSelection))
                            {
                                continue;
                            }

                            ISelectionSet themeSelSet = ((IFeatureSelection)theme).SelectionSet;
                            if (themeSelSet == null)
                            {
                                continue;
                            }

                            if (themeSelSet.Count > 0)
                            {
                                return true;
                            }
                        }
                    }

                    if (!(layer is IFeatureSelection))
                    {
                        continue;
                    }

                    ISelectionSet selSet = ((IFeatureSelection)layer).SelectionSet;
                    if (selSet == null)
                    {
                        continue;
                    }

                    if (selSet.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (!(MapEvent is MapEvent))
            {
                return true;
            }

            IMap map = ((MapEvent)MapEvent).Map;
            if (map == null)
            {
                return true;
            }

            Envelope env = null;
            IEnvelope envelope = null;

            double maximumScale = 1.0;

            foreach (IDatasetElement layer in map.MapElements)
            {
                if (layer is IWebServiceLayer && ((IWebServiceLayer)layer).WebServiceClass != null && ((IWebServiceLayer)layer).WebServiceClass.Themes != null)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceLayer)layer).WebServiceClass.Themes)
                    {
                        envelope = await SelectionEnvelope(map, theme as IFeatureLayer);
                        if (envelope == null)
                        {
                            continue;
                        }

                        if (env == null)
                        {
                            env = new Envelope(envelope);
                        }
                        else
                        {
                            env.Union(envelope);
                        }

                        if (theme is ILayer)
                        {
                            maximumScale = Math.Max(maximumScale, theme.MaximumZoomToFeatureScale);
                        }
                    }
                }

                envelope = await SelectionEnvelope(map, layer as IFeatureLayer);
                if (envelope == null)
                {
                    continue;
                }

                if (env == null)
                {
                    env = new Envelope(envelope);
                }
                else
                {
                    env.Union(envelope);
                }

                if (layer is ILayer)
                {
                    maximumScale = Math.Max(maximumScale, ((ILayer)layer).MaximumZoomToFeatureScale);
                }
            }

            if (env != null)
            {
                env.Raise(110.0);
                map.Display.ZoomTo(env);
                if (maximumScale > _doc.FocusMap.Display.MapScale)
                {
                    _doc.FocusMap.Display.MapScale = maximumScale;
                }
                //((Map)map).setScale(env.minx, env.miny, env.maxx, env.maxy, true);
                ((MapEvent)MapEvent).refreshMap = true;
            }

            return true;
        }

        public gView.Framework.UI.ToolType toolType
        {
            get
            {
                return ToolType.command;
            }
        }

        public string ToolTip
        {
            get
            {
                return "";
            }
        }

        #endregion

        #region Helper
        async private Task<IEnvelope> SelectionEnvelope(IMap map, IFeatureLayer fLayer)
        {
            if (!(fLayer is IFeatureSelection) ||
                fLayer.FeatureClass == null ||
                map == null || map.Display == null ||
                ((IFeatureSelection)fLayer).SelectionSet == null ||
                ((IFeatureSelection)fLayer).SelectionSet.Count == 0)
            {
                return null;
            }

            ISelectionSet selSet = ((IFeatureSelection)fLayer).SelectionSet;
            IFeatureClass fc = fLayer.FeatureClass;

            bool project = false;
            if (fc.SpatialReference != null && !fc.SpatialReference.Equals(map.Display.SpatialReference))
            {
                project = true;
            }

            IQueryFilter filter = null;
            if (selSet is IIDSelectionSet)
            {
                filter = new RowIDFilter(fc.IDFieldName, ((IIDSelectionSet)selSet).IDs);
            }
            else if (selSet is IQueryFilteredSelectionSet)
            {
                filter = ((IQueryFilteredSelectionSet)selSet).QueryFilter.Clone() as IQueryFilter;
            }
            if (filter == null)
            {
                return null;
            }

            filter.AddField(fc.ShapeFieldName);
            Envelope env = null;
            using (IFeatureCursor cursor = (fc is ISelectionCache) ?
                ((ISelectionCache)fc).GetSelectedFeatures() :
                await fc.GetFeatures(filter))
            {
                if (cursor == null)
                {
                    return null;
                }

                IFeature feat;
                while ((feat = await cursor.NextFeature()) != null)
                {
                    if (feat.Shape == null)
                    {
                        continue;
                    }

                    IEnvelope envelope = feat.Shape.Envelope;
                    if (project)
                    {
                        IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, fc.SpatialReference, map.Display.SpatialReference);
                        if (geom == null)
                        {
                            continue;
                        }

                        envelope = geom.Envelope;
                    }
                    if (env == null)
                    {
                        env = new gView.Framework.Geometry.Envelope(envelope);
                    }
                    else
                    {
                        env.Union(envelope);
                    }
                }
                cursor.Dispose();
            }

            return env;
        }
        #endregion
    }
}
