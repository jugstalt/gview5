﻿using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System.ComponentModel;
using System.Reflection;

namespace gView.Framework.Symbology
{
    [gView.Framework.system.RegisterPlugIn("48177A8B-1B3F-480a-87DF-9F7E1DE57D7B")]
    public sealed class PolygonMaskSymbol : LegendItem, IFillSymbol, IPropertyPage
    {
        private IBrush _brush;
        private ArgbColor _color;

        public PolygonMaskSymbol()
        {
            _color = ArgbColor.White;
            _brush = Current.Engine.CreateSolidBrush(_color);
        }

        private PolygonMaskSymbol(ArgbColor color)
        {
            _color = color;
            _brush = Current.Engine.CreateSolidBrush(_color);
        }

        ~PolygonMaskSymbol()
        {
            this.Release();
        }

        [Browsable(true)]
        [Category("Fill Symbol")]
        [UseColorPicker()]
        public ArgbColor Color
        {
            get
            {
                return _color;
            }
            set
            {
                _brush.Color = value;
                _color = value;
            }
        }

        #region IFillSymbol Member

        public void FillPath(IDisplay display, IGraphicsPath path)
        {
            display.Canvas.FillPath(_brush, path);
        }

        #endregion

        #region ISymbol Member

        public bool SupportsGeometryType(GeometryType geomType) => geomType == GeometryType.Polygon || geomType == GeometryType.Aggregate;

        public void Draw(IDisplay display, IGeometry geometry)
        {
            Polygon p = new Polygon();
            p.AddRing(display.Envelope.ToPolygon(0)[0]);

            if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    var ring = ((IPolygon)geometry)[i];
                    if (SpatialAlgorithms.Algorithm.Intersects(display.Envelope, new Polygon(ring)))
                    {
                        p.AddRing(ring);
                    }
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int g = 0; g < ((IAggregateGeometry)geometry).GeometryCount; g++)
                {
                    if (((IAggregateGeometry)geometry)[g] is IPolygon)
                    {
                        IPolygon poly = (IPolygon)((IAggregateGeometry)geometry)[g];
                        for (int i = 0; i < poly.RingCount; i++)
                        {
                            var ring = poly[i];
                            if (SpatialAlgorithms.Algorithm.Intersects(display.Envelope, new Polygon(ring)))
                            {
                                p.AddRing(ring);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var gp = DisplayOperations.Geometry2GraphicsPath(display, p);
            if (gp != null)
            {
                this.FillPath(display, gp);
                gp.Dispose(); gp = null;
            }
        }

        public void Release()
        {
            if (_brush != null)
            {
                _brush.Dispose();
                _brush = null;
            }
        }

        [Browsable(false)]
        public string Name
        {
            get { return "Polygon Mask"; }
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPageObject()
        {
            return null;
        }

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Symbology.UI.dll");

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SimpleFillSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        #endregion

        #region IClone2 Member

        public object Clone(CloneOptions options)
        {
            var maskSymbol = new PolygonMaskSymbol(_brush.Color);

            maskSymbol.LegendLabel = _legendLabel;
            return maskSymbol;
        }

        #endregion

        #region IPersistable Member

        new public void Load(IPersistStream stream)
        {
            base.Load(stream);

            this.Color = ArgbColor.FromArgb((int)stream.Load("color", ArgbColor.White.ToArgb()));
        }

        new public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("color", this.Color.ToArgb());
        }

        #endregion

        #region ISymbol Member

        [Browsable(false)]
        public SymbolSmoothing SymbolSmothingMode
        {
            set { }
        }

        public bool RequireClone()
        {
            return false;
        }

        #endregion
    }
}
