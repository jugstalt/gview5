using gView.Framework.Carto.Rendering.UI;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.IO;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace gView.Framework.Carto.Rendering
{
    [gView.Framework.system.RegisterPlugIn("CD41C987-9415-4c7c-AF5F-6385622AB768")]
    public class ScaleDependentRenderer : IGroupRenderer, IFeatureRenderer, IPropertyPage, ILegendGroup, ISimplify
    {
        private RendererList _renderers;
        private bool _useRefScale = true;

        public ScaleDependentRenderer()
        {
            _renderers = new RendererList();
        }

        #region IGroupRenderer Member

        public IRendererGroup Renderers
        {
            get { return _renderers; }
        }

        #endregion

        #region IFeatureRenderer Member
        public void Draw(IDisplay disp, gView.Framework.Data.IFeature feature)
        {
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.Draw(disp, feature);
            }
        }

        public void StartDrawing(IDisplay display) { }

        public void FinishDrawing(IDisplay disp, ICancelTracker cancelTracker)
        {
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.FinishDrawing(disp, cancelTracker);
                }
            }
        }

        public void PrepareQueryFilter(IFeatureLayer layer, IQueryFilter filter)
        {
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.PrepareQueryFilter(layer, filter);
            }
        }

        public bool CanRender(IFeatureLayer layer, IMap map)
        {
            return true;
        }

        public bool HasEffect(IFeatureLayer layer, IMap map)
        {
            if (_renderers == null)
            {
                return false;
            }

            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.HasEffect(layer, map))
                {
                    return true;
                }
            }
            return false;
        }

        public bool UseReferenceScale
        {
            get
            {
                return _useRefScale;
            }
            set
            {
                _useRefScale = value;
                foreach (IFeatureRenderer renderer in _renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.UseReferenceScale = _useRefScale;
                }
            }
        }

        public string Name
        {
            get { return "Scale Dependent Renderer"; }
        }

        public string Category
        {
            get { return "Group"; }
        }

        public bool RequireClone()
        {
            return _renderers?.Where(r => r != null && r.RequireClone()).FirstOrDefault() != null;
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _useRefScale = (bool)stream.Load("useRefScale", true);

            ScaleRendererPersist persist;
            while ((persist = stream.Load("ScaleRenderer", null, new ScaleRendererPersist(new ScaleRenderer(null))) as ScaleRendererPersist) != null)
            {
                _renderers.Add(persist.ScaleRenderer);
            }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("useRefScale", _useRefScale);

            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                stream.Save("ScaleRenderer", new ScaleRendererPersist(renderer as ScaleRenderer));
            }
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            if (!(initObject is IFeatureLayer))
            {
                return null;
            }

            try
            {
                string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Carto.Rendering.UI.dll");

                IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Carto.Rendering.UI.PropertyForm_FeatureGroupRenderer") as IPropertyPanel;
                if (p != null)
                {
                    return p.PropertyPanel(this, (IFeatureLayer)initObject);
                }
            }
            catch (Exception /*ex*/)
            {

            }
            return null;
        }

        public object PropertyPageObject()
        {
            return this;
        }

        #endregion

        #region IClone2 Member

        public object Clone(CloneOptions options)
        {
            ScaleDependentRenderer scaledependentRenderer = new ScaleDependentRenderer();
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                scaledependentRenderer._renderers.Add(renderer.Clone(options) as IFeatureRenderer);
            }

            scaledependentRenderer.UseReferenceScale = _useRefScale;
            return scaledependentRenderer;
        }

        public void Release()
        {
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.Release();
            }
            _renderers.Clear();
        }

        #endregion

        #region ILegendGroup Member

        public int LegendItemCount
        {
            get
            {
                int count = 0;
                foreach (IFeatureRenderer renderer in _renderers)
                {
                    if (!(renderer is ILegendGroup))
                    {
                        continue;
                    }

                    count += ((ILegendGroup)renderer).LegendItemCount;
                }
                return count;
            }
        }

        public ILegendItem LegendItem(int index)
        {
            int count = 0;
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (!(renderer is ILegendGroup))
                {
                    continue;
                }

                if (count + ((ILegendGroup)renderer).LegendItemCount > index)
                {
                    return ((ILegendGroup)renderer).LegendItem(index - count);
                }
                count += ((ILegendGroup)renderer).LegendItemCount;
            }
            return null;
        }

        public void SetSymbol(ILegendItem item, ISymbol symbol)
        {
            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (!(renderer is ILegendGroup))
                {
                    continue;
                }

                int count = ((ILegendGroup)renderer).LegendItemCount;
                for (int i = 0; i < count; i++)
                {
                    if (((ILegendGroup)renderer).LegendItem(i) == item)
                    {
                        ((ILegendGroup)renderer).SetSymbol(item, symbol);
                        return;
                    }
                }
            }
        }

        #endregion

        #region IClone Member

        public object Clone()
        {
            ScaleDependentRenderer scaleDependentRenderer = new ScaleDependentRenderer();

            scaleDependentRenderer._useRefScale = _useRefScale;
            foreach (IFeatureRenderer renderer in this.Renderers)
            {
                scaleDependentRenderer.Renderers.Add(renderer.Clone() as IFeatureRenderer);
            }

            return scaleDependentRenderer;
        }

        #endregion

        private class ScaleRenderer : IFeatureRenderer, IScaledependent, IPropertyPage, ILegendGroup, ISimplify
        {
            private IFeatureRenderer _renderer = null;
            private double _minScale = 0, _maxScale = 0;

            public ScaleRenderer(IFeatureRenderer renderer)
            {
                _renderer = renderer;
            }
            public ScaleRenderer(IFeatureRenderer renderer, double minScale, double maxScale)
                : this(renderer)
            {
                _minScale = minScale;
                _maxScale = maxScale;
            }

            internal IFeatureRenderer Renderer
            {
                get { return _renderer; }
                set { _renderer = value; }
            }

            #region IScaledependent
            public double MinimumScale
            {
                get { return _minScale; }
                set { _minScale = value; }
            }

            public double MaximumScale
            {
                get { return _maxScale; }
                set { _maxScale = value; }
            }
            #endregion

            #region IFeatureRenderer Member

            public void Draw(IDisplay disp, gView.Framework.Data.IFeature feature)
            {
                if (_renderer == null)
                {
                    return;
                }

                if (this.MinimumScale > 1 && this.MinimumScale > disp.MapScale)
                {
                    return;
                }

                if (this.MaximumScale > 1 && this.MaximumScale < disp.MapScale)
                {
                    return;
                }

                _renderer.Draw(disp, feature);
            }

            public void StartDrawing(IDisplay display) { }

            public void FinishDrawing(IDisplay disp, ICancelTracker cancelTracker)
            {
                if (_renderer != null)
                {
                    _renderer.FinishDrawing(disp, cancelTracker);
                }
            }

            public void PrepareQueryFilter(IFeatureLayer layer, IQueryFilter filter)
            {
                if (_renderer == null)
                {
                    return;
                }

                _renderer.PrepareQueryFilter(layer, filter);
            }

            public bool CanRender(gView.Framework.Data.IFeatureLayer layer, IMap map)
            {
                if (_renderer == null)
                {
                    return false;
                }

                return _renderer.CanRender(layer, map);
            }

            public bool HasEffect(gView.Framework.Data.IFeatureLayer layer, IMap map)
            {
                if (_renderer == null || map == null || map.Display == null)
                {
                    return false;
                }

                if (this.MinimumScale > 1 && this.MinimumScale > map.Display.MapScale)
                {
                    return false;
                }

                if (this.MaximumScale > 1 && this.MaximumScale < map.Display.MapScale)
                {
                    return false;
                }

                return _renderer.HasEffect(layer, map);
            }

            public bool UseReferenceScale
            {
                get
                {
                    if (_renderer == null)
                    {
                        return false;
                    }

                    return _renderer.UseReferenceScale;
                }
                set
                {
                    if (_renderer == null)
                    {
                        return;
                    }

                    _renderer.UseReferenceScale = value;
                }
            }

            public string Name
            {
                get
                {
                    if (_renderer == null)
                    {
                        return "";
                    }

                    return _renderer.Name;
                }
            }

            public string Category
            {
                get
                {
                    if (_renderer == null)
                    {
                        return "";
                    }

                    return _renderer.Category;
                }
            }

            public bool RequireClone()
            {
                return _renderer != null && _renderer.RequireClone();
            }

            #endregion

            #region IgViewExtension Member

            public Guid GUID
            {
                get
                {
                    return PlugInManager.PlugInID(_renderer);
                }
            }

            #endregion

            #region IPersistable Member

            public void Load(gView.Framework.IO.IPersistStream stream)
            {
                ScaleRendererPersist persist = new ScaleRendererPersist(this);

                persist.Load(stream);
            }

            public void Save(gView.Framework.IO.IPersistStream stream)
            {
                ScaleRendererPersist persist = new ScaleRendererPersist(this);

                persist.Save(stream);
            }

            #endregion

            #region IClone2 Member

            public object Clone(CloneOptions options)
            {
                if (_renderer == null)
                {
                    return null;
                }

                IFeatureRenderer renderer = _renderer.Clone(options) as IFeatureRenderer;
                if (renderer == null)
                {
                    return null;
                }

                ScaleRenderer scaleRenderer = new ScaleRenderer(renderer);
                scaleRenderer._minScale = _minScale;
                scaleRenderer._maxScale = _maxScale;

                return scaleRenderer;
            }

            public void Release()
            {
                if (_renderer != null)
                {
                    _renderer.Release();
                }
            }

            #endregion

            #region IPropertyPage Member

            public object PropertyPage(object initObject)
            {
                if (_renderer is IPropertyPage)
                {
                    return ((IPropertyPage)_renderer).PropertyPage(initObject);
                }

                return null;
            }

            public object PropertyPageObject()
            {
                if (_renderer is IPropertyPage)
                {
                    return ((IPropertyPage)_renderer).PropertyPageObject();
                }

                return null;
            }

            #endregion

            #region ILegendGroup Member

            public int LegendItemCount
            {
                get
                {
                    if (_renderer is ILegendGroup)
                    {
                        return ((ILegendGroup)_renderer).LegendItemCount;
                    }

                    return 0;
                }
            }

            public ILegendItem LegendItem(int index)
            {
                if (_renderer is ILegendGroup)
                {
                    return ((ILegendGroup)_renderer).LegendItem(index);
                }

                return null;
            }

            public void SetSymbol(ILegendItem item, ISymbol symbol)
            {
                if (_renderer is ILegendGroup)
                {
                    ((ILegendGroup)_renderer).SetSymbol(item, symbol);
                }
            }

            #endregion

            #region IClone Member

            public object Clone()
            {
                IFeatureRenderer renderer = _renderer.Clone() as IFeatureRenderer;

                ScaleRenderer scaleRenderer = new ScaleRenderer(renderer);
                scaleRenderer._minScale = _minScale;
                scaleRenderer._maxScale = _maxScale;
                return scaleRenderer;
            }

            #endregion

            #region IRenderer Member

            public List<ISymbol> Symbols
            {
                get
                {
                    if (_renderer != null)
                    {
                        return _renderer.Symbols;
                    }

                    return new List<ISymbol>();
                }
            }

            #endregion

            #region ISimplify Member

            public void Simplify()
            {
                if (_renderer is ISimplify)
                {
                    ((ISimplify)_renderer).Simplify();
                }
            }

            #endregion

            public bool Combine(IRenderer cand)
            {
                if (_renderer == null)
                {
                    return false;
                }

                if (cand is ScaleRenderer && cand != this &&
                    ((ScaleRenderer)cand).MinimumScale == this.MinimumScale &&
                    ((ScaleRenderer)cand).MaximumScale == this.MaximumScale)
                {
                    return _renderer.Combine(((ScaleRenderer)cand).Renderer);
                }

                return false;
            }
        }

        private class ScaleRendererPersist : IPersistable
        {
            ScaleRenderer _renderer;
            public ScaleRendererPersist(ScaleRenderer scaleRenderer)
            {
                _renderer = scaleRenderer;
            }

            internal ScaleRenderer ScaleRenderer
            {
                get { return _renderer; }
            }

            #region IPersistable Member

            public void Load(IPersistStream stream)
            {
                if (_renderer == null)
                {
                    return;
                }

                _renderer.MinimumScale = (double)stream.Load("MinScale", 0.0);
                _renderer.MaximumScale = (double)stream.Load("MaxScale", 0.0);

                _renderer.Renderer = stream.Load("Renderer", null) as IFeatureRenderer;
            }

            public void Save(IPersistStream stream)
            {
                if (_renderer == null)
                {
                    return;
                }

                stream.Save("MinScale", _renderer.MinimumScale);
                stream.Save("MaxScale", _renderer.MaximumScale);

                stream.Save("Renderer", _renderer.Renderer);
            }

            #endregion
        }

        private class RendererList : List<IFeatureRenderer>, IRendererGroup
        {
            public new void Add(IFeatureRenderer renderer)
            {
                if (renderer == null)
                {
                    return;
                }

                if (renderer is ScaleRenderer)
                {
                    base.Add(renderer);
                }
                else
                {
                    base.Add(new ScaleRenderer(renderer));
                }
            }
        }

        #region IRenderer Member

        public List<ISymbol> Symbols
        {
            get
            {
                List<ISymbol> symbols = new List<ISymbol>();

                if (_renderers != null)
                {
                    foreach (IRenderer renderer in _renderers)
                    {
                        if (renderer == null)
                        {
                            continue;
                        }

                        symbols.AddRange(renderer.Symbols);
                    }
                }

                return symbols;
            }
        }

        public bool Combine(IRenderer renderer)
        {
            if (this == renderer)
            {
                return false;
            }

            if (renderer is ScaleDependentRenderer)
            {
                ScaleDependentRenderer cand = (ScaleDependentRenderer)renderer;

                foreach (ScaleRenderer sRenderer in this.Renderers)
                {
                    for (int i = 0; i < cand.Renderers.Count; i++)
                    {
                        if (sRenderer.Combine(cand.Renderers[i]))
                        {
                            cand.Renderers.RemoveAt(i);
                            i--;
                        }
                    }
                }

                return cand.Renderers.Count == 0;
            }

            return false;
        }
        #endregion

        #region ISimplify Member

        public void Simplify()
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (IFeatureRenderer renderer in _renderers)
            {
                if (renderer is ISimplify)
                {
                    ((ISimplify)renderer).Simplify();
                }
            }

            #region SimpleRenderer zusammenfassen
            bool allSimpleRenderers = true;
            foreach (IRenderer renderer in _renderers)
            {
                if (!(renderer is ScaleRenderer) && (((ScaleRenderer)renderer).Renderer is SimpleRenderer))
                {
                    allSimpleRenderers = false;
                    break;
                }
            }

            if (allSimpleRenderers && _renderers.Count > 1)
            {
                ScaleRenderer renderer = (ScaleRenderer)_renderers[0];
                if (_renderers.Count > 1)
                {
                    ISymbolCollection symCol = PlugInManager.Create(new Guid("062AD1EA-A93C-4c3c-8690-830E65DC6D91")) as ISymbolCollection;
                    foreach (IRenderer sRenderer in _renderers)
                    {
                        if (((SimpleRenderer)((ScaleRenderer)sRenderer).Renderer).Symbol != null)
                        {
                            symCol.AddSymbol(((SimpleRenderer)((ScaleRenderer)sRenderer).Renderer).Symbol);
                        }
                    }
                    ((SimpleRenderer)((ScaleRenderer)renderer).Renderer).Symbol = (ISymbol)symCol;
                    _renderers.Clear();
                    _renderers.Add(renderer);
                }
            }
            #endregion

            ShrinkScaleRenderes();
        }

        #endregion

        public void ShrinkScaleRenderes()
        {
            if (_renderers == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Count; i++)
            {
                ScaleRenderer sRenderer = _renderers[i] as ScaleRenderer;
                for (int j = i + 1; j < _renderers.Count; j++)
                {
                    ScaleRenderer sRenderCand = _renderers[j] as ScaleRenderer;

                    if (sRenderer.Combine(sRenderCand))
                    {
                        _renderers.RemoveAt(j);
                        j--;
                    }
                }
            }
        }
    }
}
