using gView.Framework.Carto;
using gView.Framework.Carto.UI;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.UI;
using gView.Framework.UI.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Xceed.Wpf.AvalonDock.Layout;

namespace gView.Desktop.Wpf.Carto
{
    internal class DataViewMenuItem : MenuItem
    {
        private DataView _dv;

        public DataViewMenuItem(DataView dv)
        {
            _dv = dv;
        }

        public new string Name
        {
            get
            {
                if (_dv == null)
                {
                    return "Null";
                }

                return _dv.Name;
            }
        }

        public Xceed.Wpf.AvalonDock.Layout.LayoutDocument TabPage
        {
            get
            {
                if (_dv == null)
                {
                    return null;
                }

                return _dv.TabPage;
            }
        }

        public DataView DataView
        {
            get { return _dv; }
        }
    }

    internal class DataView : IPersistable
    {
        public event EventHandler DataViewRenamed;

        private IMap _map = null;
        private IEnvelope _envelope;
        private MapView _mapView;
        private IToc _toc = null;
        private List<IMap> _maps = null;
        private Xceed.Wpf.AvalonDock.Layout.LayoutDocument _tabPage;
        private double _displayRotation = 0.0;

        public DataView(IMap map, MapView mapView)
        {
            _mapView = mapView;
            this.Map = map;
        }

        // Zum Laden einens DataViews
        public DataView(IEnumerable<IMap> maps)
        {
            _maps = new List<IMap>(maps);
        }

        #region IDataView Members

        public string Name
        {
            get
            {
                return _map.Name;
            }
        }

        public IMap Map
        {
            get
            {
                return _map;
            }
            private set
            {
                _map = value;
                if (_mapView != null && value is Map)
                {
                    _mapView.Map = (Map)value;
                }
                if (_map == null)
                {
                    return;
                }

                if (_map.Display != null)
                {
                    //_envelope = _map.Display.Envelope;
                }
                _map.MapRenamed += new EventHandler(_map_MapRenamed);
            }
        }

        private void _map_MapRenamed(object sender, EventArgs e)
        {
            if (DataViewRenamed != null)
            {
                DataViewRenamed(this, e);
            }
        }

        #endregion IDataView Members

        internal IEnvelope Envelope
        {
            get { return _envelope; }
            set
            {
                _envelope = value;
            }
        }

        internal MapView MapView
        {
            get { return _mapView; }
        }

        internal Xceed.Wpf.AvalonDock.Layout.LayoutDocument TabPage
        {
            set { _tabPage = value; }
            get { return _tabPage; }
        }

        internal IToc TOC
        {
            get { return _toc; }
            set
            {
                if (_toc is TOC && _toc != value)
                {
                    ((TOC)_toc).Dispose();
                }
                _toc = value;
            }
        }

        internal double DisplayRotation
        {
            get { return _displayRotation; }
            set
            {
                _displayRotation = value;
                if (_map != null)
                {
                    _map.Display.DisplayTransformation.DisplayRotation = _displayRotation;
                }
            }
        }

        internal LayoutDocument LayoutDocument { get; set; }

        #region IPersistable Members

        public string PersistID
        {
            get { return ""; }
        }

        public void Load(IPersistStream stream)
        {
            string mapName = (string)stream.Load("map", "");
            if (_maps != null)
            {
                foreach (IMap map in _maps)
                {
                    if (map.Name == mapName)
                    {
                        this.Map = (Map)map;
                        break;
                    }
                }
            }

            double? minx = (double?)stream.Load("minx");
            if (minx != null)
            {
                _envelope = new Envelope(
                    (double)minx,
                    (double)stream.Load("miny", 0.0),
                    (double)stream.Load("maxx", 0.0),
                    (double)stream.Load("maxy", 0.0));
            }

            if (this.Map != null)
            {
                TOC objInst = new TOC(this.Map);
                objInst.Modifier = TocModifier.Private;
                _toc = (TOC)stream.Load("ITOC", null, objInst);
                if (_toc == null)
                {
                    objInst.Dispose();
                }

                _displayRotation = (double)stream.Load("DisplayRotation", (double)0.0);
            }
        }

        public void Save(IPersistStream stream)
        {
            if (_map != null)
            {
                stream.Save("map", _map.Name);
            }

            if (_envelope != null)
            {
                stream.Save("minx", _envelope.minx);
                stream.Save("miny", _envelope.miny);
                stream.Save("maxx", _envelope.maxx);
                stream.Save("maxy", _envelope.maxy);
            }

            if (_toc != null)
            {
                stream.Save("ITOC", _toc);
            }

            if (_displayRotation != 0.0)
            {
                stream.Save("DisplayRotation", _displayRotation);
            }
        }

        #endregion IPersistable Members
    }
}