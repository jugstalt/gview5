using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Symbology.UI;
using gView.Win.Carto.Rendering.UI.Framework.Carto.Rendering.Extensions;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace gView.Framework.Carto.Rendering.UI
{
    public partial class PropertyForm_UniversalGeometryRenderer : Form, IPropertyPanel
    {
        private IFeatureLayer _layer = null;
        private UniversalGeometryRenderer _renderer = null;
        public PropertyForm_UniversalGeometryRenderer()
        {

        }

        private void btnChooseSymbol_Paint(object sender, PaintEventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            e.Graphics.DrawSymbol(_renderer[GeometryType.Point], new Rectangle(5, 5, btnChooseSymbol.Width - 10, btnChooseSymbol.Height - 10));
        }

        private void btnChoosePolygonSymbol_Paint(object sender, PaintEventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            e.Graphics.DrawSymbol(_renderer[GeometryType.Polygon], new Rectangle(5, 5, btnChooseSymbol.Width - 10, btnChooseSymbol.Height - 10));
        }

        private void btnChooseLineSymbol_Paint(object sender, PaintEventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            e.Graphics.DrawSymbol(_renderer[GeometryType.Polyline], new Rectangle(5, 5, btnChooseSymbol.Width - 10, btnChooseSymbol.Height - 10));
        }

        private void btnChooseSymbol_Click(object sender, EventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            FormSymbol dlg = new FormSymbol(_renderer[GeometryType.Point]);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer[GeometryType.Point] = dlg.Symbol;
            }
        }

        private void btnChooseLineSymbol_Click(object sender, EventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            FormSymbol dlg = new FormSymbol(_renderer[GeometryType.Polyline]);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer[GeometryType.Polyline] = dlg.Symbol;
            }
        }

        private void btnChoosePolygonSymbol_Click(object sender, EventArgs e)
        {
            if (_renderer == null)
            {
                return;
            }

            FormSymbol dlg = new FormSymbol(_renderer[GeometryType.Polygon]);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _renderer[GeometryType.Polygon] = dlg.Symbol;
            }
        }

        private void btnRotation_Click(object sender, EventArgs e)
        {
            if (_renderer == null || _layer == null || _layer.FeatureClass == null)
            {
                return;
            }

            FormRotationType dlg = new FormRotationType(_renderer.SymbolRotation, _layer.FeatureClass);
            if (dlg.ShowDialog() == DialogResult.OK)
            {

            }
        }

        private void chkUsePointSymbol_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer != null)
            {
                _renderer.UsePointSymbol = chkUsePointSymbol.Checked;
            }
        }

        private void chkUseLineSymbol_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer != null)
            {
                _renderer.UseLineSymbol = chkUseLineSymbol.Checked;
            }
        }

        private void chkUsePolyonSymbol_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer != null)
            {
                _renderer.UsePolygonSymbol = chkUsePolyonSymbol.Checked;
            }
        }

        #region IPropertyPanel Member

        public object PropertyPanel(IFeatureRenderer renderer, IFeatureLayer layer)
        {
            InitializeComponent();

            _renderer = renderer as UniversalGeometryRenderer;
            _layer = layer;

            if (_renderer != null)
            {
                chkUsePointSymbol.Checked = _renderer.UsePointSymbol;
                chkUseLineSymbol.Checked = _renderer.UseLineSymbol;
                chkUsePolyonSymbol.Checked = _renderer.UsePolygonSymbol;
            }

            return panel1;
        }

        #endregion
    }
}