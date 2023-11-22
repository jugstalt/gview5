using gView.Framework.Data.Metadata;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using System;

namespace gView.Framework.Carto
{
    public class Display : MapMetadata, gView.Framework.Carto.IDisplay
    {
        public event MapScaleChangedEvent MapScaleChanged;
        public event RenderOverlayImageEvent RenderOverlayImage;

        #region Variablen

        protected double m_maxX, m_maxY, m_minX, m_minY;
        protected double m_actMinX = -10, m_actMinY = -10, m_actMaxX = 10, m_actMaxY = 10;
        protected int m_iWidth, m_iHeight, m_fixScaleIndex;
        protected double m_dpm = (96.0 / 0.0254);  // dots per [m]... 96 dpi werden angenommen
        protected double m_scale;
        protected bool m_extentChanged;
        protected double m_fontsizeFactor, m_widthFactor, m_refScale;
        //protected ArrayList m_fixScales;
        public gView.GraphicsEngine.Abstraction.IBitmap _bitmap = null;
        public gView.GraphicsEngine.Abstraction.ICanvas _canvas = null;
        protected ISpatialReference _spatialReference = null;
        protected IGeometricTransformer _geoTransformer = null;
        protected float _OX = 0, _OY = 0;
        protected GraphicsEngine.ArgbColor _backgroundColor = GraphicsEngine.ArgbColor.White;
        private GraphicsEngine.ArgbColor _transColor = GraphicsEngine.ArgbColor.White;
        private bool _makeTransparent = false;
        protected ILabelEngine _labelEngine;
        protected GeoUnits _mapUnits = GeoUnits.Unknown, _displayUnits = GeoUnits.Unknown;
        private object lockThis = new object();
        private DisplayScreen _screen;
        private DisplayTransformation _displayTransformation = new DisplayTransformation();

        #endregion

        public Display(IMap map)
        {
            this.Map = map;

            m_extentChanged = true;
            m_fontsizeFactor = m_widthFactor = m_refScale = -1.0;
            //m_fixScales=new ArrayList();

            _labelEngine = new LabelEngine2();

            _screen = new DisplayScreen();
            _screen.RefreshSettings();

            this.Dpi = GraphicsEngine.Current.Engine.ScreenDpi;
        }

        internal Display(IMap map, bool createLabelEngine)
            : this(map)
        {
            m_extentChanged = true;
            m_fontsizeFactor = m_widthFactor = m_refScale = -1.0;
            //m_fixScales = new ArrayList();

            _labelEngine = (createLabelEngine) ? new LabelEngine2() : null;
        }

        public void setLimit(double minx, double miny, double maxx, double maxy)
        {
            m_minX = m_actMinX = minx;
            m_maxX = m_actMaxX = maxx;
            m_minY = m_actMinY = miny;
            m_maxY = m_actMaxY = maxy;
            this.ZoomTo(m_actMinX, m_actMinY, m_actMaxX, m_actMaxY);
        }

        public void World2Image(ref double x, ref double y)
        {
            //x=(x-m_actMinX)*m_iWidth/_wWidth;
            //y=m_iHeight-(y-m_actMinY)*m_iHeight/_wHeight;

            x = (x - m_actMinX) * m_iWidth / (m_actMaxX - m_actMinX);
            y = m_iHeight - (y - m_actMinY) * m_iHeight / (m_actMaxY - m_actMinY);

            x += _OX;
            //y -= _OX;
            y += _OY;

            if (_displayTransformation.UseTransformation)
            {
                _displayTransformation.Transform(this, ref x, ref y);
            }
        }

        public void Image2World(ref double x, ref double y)
        {
            if (_displayTransformation.UseTransformation)
            {
                _displayTransformation.InvTransform(this, ref x, ref y);
            }

            x -= _OX;
            y -= _OY;

            x = x * (m_actMaxX - m_actMinX) / m_iWidth + m_actMinX;
            y = (m_iHeight - y) * (m_actMaxY - m_actMinY) / m_iHeight + m_actMinY;
        }

        public gView.GraphicsEngine.Abstraction.ICanvas Canvas
        {
            get { return _canvas; }
            set { _canvas = value; }
        }

        public ISpatialReference SpatialReference
        {
            get { return _spatialReference; }
            set
            {
                _spatialReference = value;
                if (value != null)
                {
                    if (_spatialReference.SpatialParameters.IsGeographic && _mapUnits >= 0)
                    {
                        _displayUnits = _mapUnits = GeoUnits.DecimalDegrees;
                    }
                    else if (!_spatialReference.SpatialParameters.IsGeographic && _mapUnits < 0)
                    {
                        _displayUnits = _mapUnits = GeoUnits.Unknown;
                    }
                }
            }
        }

        public IGeometricTransformer GeometricTransformer
        {
            get { return _geoTransformer; }
            set
            {
                _geoTransformer = value;
            }
        }

        public void Draw(ISymbol symbol, IGeometry geometry)
        {
            try
            {
                IGeometry geom;
                if (_geoTransformer != null)
                {
                    geom = (IGeometry)_geoTransformer.Transform2D(geometry);
                }
                else
                {
                    geom = geometry;
                }

                symbol.Draw(this, geom);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        public void DrawOverlay(IGraphicsContainer container, bool clearOld)
        {
            if (RenderOverlayImage == null || _canvas != null)
            {
                return;
            }

            if (container == null)
            {
                ClearOverlay();
            }

            GraphicsEngine.Abstraction.IBitmap bm = null;
            try
            {
                bm = GraphicsEngine.Current.Engine.CreateBitmap(ImageWidth, ImageHeight);
                bm.MakeTransparent();

                _canvas = bm.CreateCanvas();
                foreach (IGraphicElement element in container.Elements)
                {
                    element.Draw(this);
                }

                RenderOverlayImage(bm, clearOld);
            }
            catch
            {
            }
            finally
            {
                if (_canvas != null)
                {
                    _canvas.Dispose();
                }

                _canvas = null;
                if (bm != null)
                {
                    bm.Dispose();
                }
            }
        }

        public void ClearOverlay()
        {
            RenderOverlayImage(null, true);
        }

        public void ZoomTo(IEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            this.ZoomTo(envelope.minx, envelope.miny, envelope.maxx, envelope.maxy);
        }

        private IGraphicsContainer _graphicsContainer = new GraphicsContainer();
        public IGraphicsContainer GraphicsContainer
        {
            get { return _graphicsContainer; }
        }

        #region Eigenschaften
        public int ImageWidth
        {
            get { return m_iWidth; }
            set { m_iWidth = value; }
        }
        public int ImageHeight
        {
            get { return m_iHeight; }
            set { m_iHeight = value; }
        }

        public double left
        {
            get { return m_actMinX; }
        }
        public double right
        {
            get { return m_actMaxX; }
        }
        public double bottom
        {
            get { return m_actMinY; }
        }
        public double top
        {
            get { return m_actMaxY; }
        }
        public IEnvelope Limit
        {
            get
            {
                return new Envelope(m_minX, m_minY, m_maxX, m_maxY);
            }
            set
            {
                if (value != null)
                {
                    m_minX = value.minx;
                    m_minY = value.miny;
                    m_maxX = value.maxx;
                    m_maxY = value.maxy;
                }
            }
        }
        public double ReferenceScale
        {
            get
            {
                return (_mapUnits == GeoUnits.Unknown) ? 0.0 : m_refScale;
            }
            set
            {
                m_refScale = value;
                if (m_refScale > 0.0)
                {
                    m_widthFactor = m_fontsizeFactor = m_refScale / Math.Max(m_scale, 1.0);
                }
                else
                {
                    m_widthFactor = m_fontsizeFactor = -1;
                }
            }
        }

        public double Dpm
        {
            get { return m_dpm; }
        }
        public double Dpi
        {
            get { return Dpm * 0.0254; }
            set { m_dpm = value / 0.0254; }
        }
        public IEnvelope Envelope
        {
            get
            {
                return new Envelope(m_actMinX, m_actMinY, m_actMaxX, m_actMaxY);
            }
            set
            {
                m_actMinX = value.minx;
                m_actMinY = value.miny;
                m_actMaxX = value.maxx;
                m_actMaxY = value.maxy;
            }
        }
        public GraphicsEngine.Abstraction.IBitmap Bitmap
        {
            get { return _bitmap; }
        }
        public GraphicsEngine.ArgbColor BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }

        public GraphicsEngine.ArgbColor TransparentColor
        {
            get { return _transColor; }
            set { _transColor = value; }
        }

        public bool MakeTransparent
        {
            get { return _makeTransparent; }
            set { _makeTransparent = value; }
        }
        #endregion

        #region Scale

        private double CalcScale()
        {
            double phi = 0.0;
            if (_mapUnits == GeoUnits.DecimalDegrees)
            {
                phi = Math.Min(90.0, m_actMaxY) * 0.5 + Math.Max(-90.0, m_actMinY) * 0.5;
            }

            double w = Math.Abs(m_actMaxX - m_actMinX);
            double h = Math.Abs(m_actMaxY - m_actMinY);

            GeoUnitConverter converter = new GeoUnitConverter();
            double dpu = converter.Convert(1.0, GeoUnits.Meters, _mapUnits, 1, phi);

            double s1 = ImageWidth > 0 ? Math.Abs(w) / ImageWidth * Dpm : 1; s1 /= dpu;
            double s2 = ImageHeight > 0 ? Math.Abs(h) / ImageHeight * Dpm : 1; s2 /= dpu;
            double scale = ImageWidth > 0 && ImageHeight > 0 ? Math.Max(s1, s2) : Math.Max(MapScale, 1);

            return scale;

            #region Old
            /*
            double phi1 = 0.0, phi2 = 0.0;
            if (_mapUnits == GeoUnits.DecimalDegrees)
            {
                phi1 = Math.Abs(m_actMinY);
                phi2 = Math.Abs(m_actMaxY);
            }

            double w = Math.Abs(m_actMaxX - m_actMinX);
            double h = Math.Abs(m_actMaxY - m_actMinY);

            GeoUnitConverter converter = new GeoUnitConverter();
            double Wm = converter.Convert(w, _mapUnits, GeoUnits.Meters, 1, Math.Min(phi1, phi2));
            double Hm = converter.Convert(h, _mapUnits, GeoUnits.Meters);

            double s1 = Math.Abs(Wm) / iWidth * dpm;
            double s2 = Math.Abs(Hm) / iHeight * dpm;
            double scale = Math.Max(s1, s2);

            return scale;
            */
            #endregion
        }

        private void CalcExtent(double scale, double cx, double cy)
        {
            double phi = 0.0;
            if (_mapUnits == GeoUnits.DecimalDegrees)
            {
                phi = m_actMaxY * 0.5 + m_actMinY * 0.5;
            }

            GeoUnitConverter converter = new GeoUnitConverter();
            double dpu = converter.Convert(1.0, GeoUnits.Meters, _mapUnits, 1, phi);

            double w = (ImageWidth / Dpm) * scale; w *= dpu;
            double h = (ImageHeight / Dpm) * scale; h *= dpu;

            m_actMinX = cx - w * 0.5;
            m_actMaxX = cx + w * 0.5;
            m_actMinY = cy - h * 0.5;
            m_actMaxY = cy + h * 0.5;

            #region Old
            /*
            double Wm = (iWidth / dpm) * scale;
            double Hm = (iHeight / dpm) * scale;

            double phi = 0.0;
            if (_mapUnits == GeoUnits.DecimalDegrees)
            {
                phi = m_actMaxY * 0.5 + m_actMinY * 0.5;
            }
            GeoUnitConverter converter = new GeoUnitConverter();
            double w = converter.Convert(Wm, GeoUnits.Meters, _mapUnits, 1, phi);
            double h = converter.Convert(Hm, GeoUnits.Meters, _mapUnits);

            m_actMinX = cx - w * 0.5;
            m_actMaxX = cx + w * 0.5;
            m_actMinY = cy - h * 0.5;
            m_actMaxY = cy + h * 0.5;
             * */
            #endregion
        }

        #endregion

        #region Zoom
        public void setScale(double scale, double cx, double cy)
        {
            if (scale == 0.0)
            {
                return;
            }

            CalcExtent(scale, cx, cy);
            m_scale = scale;

            #region old
            /*
			m_scale=scale;
			double w=(iWidth/dpm)*scale;
			double h=(iHeight/dpm)*scale;
			double maxW=m_maxX-m_minX,
				maxH=m_maxY-m_minY;
			double m_old_actMinX=m_actMinX,
				m_old_actMaxX=m_actMaxX,
				m_old_actMinY=m_actMinY,
				m_old_actMaxY=m_actMaxY;

			if(maxW<w && (int)maxW>0) 
			{
				double CX=m_maxX*0.5+m_minX*0.5;
				m_actMinX=CX-w*0.5;
				m_actMaxX=CX+w*0.5;
			} 
			else 
			{
				m_actMinX=cx-w*0.5;
				m_actMaxX=cx+w*0.5;
				if(m_actMinX<m_minX && (int)maxW!=0) { m_actMinX=m_minX; m_actMaxX=m_actMinX+w; }
				if(m_actMaxX>m_maxX && (int)maxW!=0) { m_actMaxX=m_maxX; m_actMinX=m_actMaxX-w; }
			}
			if(maxH<h && (int)maxH>0) 
			{
				double CY=m_maxY*0.5+m_minY*0.5;
				m_actMinY=CY-h*0.5;
				m_actMaxY=CY+h*0.5;
			}
			else
			{
				m_actMinY=cy-h*0.5;
				m_actMaxY=cy+h*0.5;
				if(m_actMinY<m_minY && (int)maxH>0) { m_actMinY=m_minY; m_actMaxY=m_actMinY+h; }
				if(m_actMaxY>m_maxY && (int)maxH>0) { m_actMaxY=m_maxY; m_actMinY=m_actMaxY-h; }
			}
			if( Math.Abs(m_actMinX-m_old_actMinX)>1e-7 ||
				Math.Abs(m_actMinY-m_old_actMinY)>1e-7 ||
				Math.Abs(m_actMaxX-m_old_actMaxX)>1e-7 ||
				Math.Abs(m_actMaxY-m_old_actMaxY)>1e-7)
			{  
				m_extentChanged=true;
			}
			if(refScale>0.0) 
			{
				m_widthFactor=m_fontsizeFactor=m_refScale/Math.Max(m_scale,1.0);
			}
            */
            #endregion

            if (MapScaleChanged != null)
            {
                MapScaleChanged(this);
            }
        }
        public void setScale(double scale)
        {
            double cx = m_actMaxX * 0.5 + m_actMinX * 0.5;
            double cy = m_actMaxY * 0.5 + m_actMinY * 0.5;
            setScale(scale, cx, cy);
        }

        public double MapScale
        {
            get { return m_scale; }
            set
            {
                setScale(value);
            }
        }

        public void ZoomTo(double minx, double miny, double maxx, double maxy)
        {
            #region AutoResize

            double dx = Math.Abs(maxx - minx), mx = (maxx + minx) / 2.0;
            double dy = Math.Abs(maxy - miny), my = (maxy + miny) / 2.0;

            if (dx < dy)
            {
                double W = Math.Max((double)ImageWidth, 1) * dy / Math.Max((double)ImageHeight, 1);
                minx = mx - W / 2.0;
                maxx = mx + W / 2.0;
            }
            else
            {
                double H = Math.Max((double)ImageHeight, 1) * dx / Math.Max((double)ImageWidth, 1);
                miny = my - H / 2.0;
                maxy = my + H / 2.0;
            }
            #endregion

            double epsi = double.Epsilon;
            if (Math.Abs(maxx - minx) < epsi)
            {
                minx -= epsi; maxx += epsi;
            }
            if (Math.Abs(maxy - miny) < epsi)
            {
                miny -= epsi; maxy += epsi;
            }
            double cx = maxx * 0.5 + minx * 0.5,
                cy = maxy * 0.5 + miny * 0.5;

            dx = Math.Abs(maxx - minx);
            dy = Math.Abs(maxy - miny);

            m_actMinX = cx - dx / 2.0; m_actMaxX = cx + dx / 2.0;
            m_actMinY = cy - dy / 2.0; m_actMaxY = cy + dy / 2.0;
            m_scale = CalcScale();

            if (m_scale < 1.0)
            {
                this.setScale(1.0);
            }
            else
            {
                if (MapScaleChanged != null)
                {
                    MapScaleChanged(this);
                }
            }


            #region AutoResize old Method
            /*
            double dx = Math.Abs(maxx - minx), mx = (maxx + minx) / 2.0;
            double dy = Math.Abs(maxy - miny), my = (maxy + miny) / 2.0;

            double phi = 0.0;
            if (_mapUnits == GeoUnits.DecimalDegrees)
            {
                phi = Math.Min(90.0, maxy) * 0.5 + Math.Max(-90.0, miny) * 0.5;
            }
            GeoUnitConverter converter = new GeoUnitConverter();
            double dx_ = converter.Convert(dx, _mapUnits, GeoUnits.Meters, 1, phi);
            double dy_ = converter.Convert(dy, _mapUnits, GeoUnits.Meters);

            if (dx_ < dy_)
            {
                double W = (double)iWidth * dy_ / (double)iHeight;
                W = converter.Convert(W, GeoUnits.Meters, _mapUnits, 1, phi);
                minx = mx - W / 2.0;
                maxx = mx + W / 2.0;
            }
            else
            {
                double H = (double)iHeight * dx_ / (double)iWidth;
                H = converter.Convert(H, GeoUnits.Meters, _mapUnits);
                miny = my - H / 2.0;
                maxy = my + H / 2.0;
            }

            ////////////////////////////

            
             * */
            #endregion
        }
        #endregion

        #region Pan
        public void Pan(double px, double py)
        {
            double cx = m_actMaxX * 0.5 + m_actMinX * 0.5;
            double cy = m_actMaxY * 0.5 + m_actMinY * 0.5;
            cx += px;
            cy += py;
            setScale(m_scale, cx, cy);
        }
        public void PanW()
        {
            Pan(-Math.Abs(m_actMaxX - m_actMinX) * 0.5, 0.0);
        }
        public void PanE()
        {
            Pan(Math.Abs(m_actMaxX - m_actMinX) * 0.5, 0.0);
        }
        public void PanN()
        {
            Pan(0.0, Math.Abs(m_actMaxY - m_actMinY) * 0.5);
        }
        public void PanS()
        {
            Pan(0.0, -Math.Abs(m_actMaxY - m_actMinY) * 0.5);
        }
        #endregion

        public ILabelEngine LabelEngine { get { return _labelEngine; } }

        public IGeometry Transform(IGeometry geometry, ISpatialReference geometrySpatialReference)
        {
            if (geometry == null)
            {
                return null;
            }

            if (geometrySpatialReference == null || _spatialReference == null || geometrySpatialReference.Equals(_spatialReference))
            {
                return geometry;
            }

            IGeometricTransformer transformer = GeometricTransformerFactory.Create();

            //transformer.FromSpatialReference = geometrySpatialReference;
            //transformer.ToSpatialReference = _spatialReference;
            transformer.SetSpatialReferences(geometrySpatialReference, _spatialReference);

            IGeometry geom = transformer.Transform2D(geometry) as IGeometry;
            transformer.Release();

            return geom;
        }


        public GeoUnits MapUnits
        {
            get
            {
                return _mapUnits;
            }
            set
            {
                _mapUnits = value;
            }
        }

        public GeoUnits DisplayUnits
        {
            get
            {
                return _displayUnits;
            }
            set
            {
                _displayUnits = value;
            }
        }

        virtual public IScreen Screen
        {
            get
            {
                return _screen;
            }
        }

        private IMap _map = null;
        virtual public IMap Map
        {
            get
            {
                return _map ?? this as IMap;
            }
            set { _map = value; }
        }

        private class DisplayScreen : IScreen
        {
            private float _fac = 1f;

            public DisplayScreen()
            {
            }

            #region IScreen Member

            public float LargeFontsFactor
            {
                get { return _fac; }
            }

            public void RefreshSettings(bool forceReloadAll = true)
            {
                _fac = SystemVariables.SystemFontsScaleFactor; //SystemVariables.PrimaryScreenDPI(forceReloadAll) / 96f;
            }

            #endregion
        }

        public IDisplayTransformation DisplayTransformation
        {
            get { return _displayTransformation; }
        }
    }
}
