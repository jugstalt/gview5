using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs
{
    public partial class FormMapProperties : Form
    {
        private IMap _map;
        private IDisplay _display;
        private FormSpatialReference _sr = null, _sr2 = null;
        private IMapApplication _app;

        public FormMapProperties(IMapApplication app, IMap map, IDisplay display)
        {
            _app = app;
            _map = map;
            _display = display;

            InitializeComponent();
        }

        private void FormMapProperties_Load(object sender, EventArgs e)
        {
            if (_map == null || _display == null)
            {
                return;
            }

            txtName.Text = _map.Name;
            numRefScale.Value = (int)_display.ReferenceScale;

            int index = 0;
            foreach (GeoUnits unit in Enum.GetValues(typeof(GeoUnits)))
            {
                // MapUnit darf nie diese Einheit sein... Immer double Wert!
                if (unit != GeoUnits.DegreesMinutesSeconds)
                {
                    bool append = false;
                    if (_display.SpatialReference != null)
                    {
                        if (_display.SpatialReference.SpatialParameters.IsGeographic && (int)unit < 0)
                        {
                            append = true;
                        }
                        else if (!_display.SpatialReference.SpatialParameters.IsGeographic && (int)unit > 0)
                        {
                            append = true;
                        }
                        else if (unit == 0)
                        {
                            append = true;
                        }
                    }
                    else
                    {
                        append = true;
                    }

                    if (append)
                    {
                        cmbMapUnits.Items.Add(new GeoUnitsItem(unit));
                        if (_display.MapUnits == unit)
                        {
                            cmbMapUnits.SelectedIndex = cmbMapUnits.Items.Count - 1;
                        }
                    }
                }

                cmbDisplayUnits.Items.Add(new GeoUnitsItem(unit));
                if (_display.DisplayUnits == unit)
                {
                    cmbDisplayUnits.SelectedIndex = index;
                }

                index++;
            }

            //if (_display.SpatialReference != null && _display.SpatialReference.SpatialParameters.Unit != GeoUnits.Unknown)
            //    cmbMapUnits.Enabled = false;

            _sr = new FormSpatialReference(_map.Display.SpatialReference);
            _sr.canModify = true;
            tabSR.Controls.Add(_sr.panelReferenceSystem);

            _sr2 = new FormSpatialReference(_map.LayerDefaultSpatialReference);
            _sr2.canModify = true;
            panelDefaultLayerSR.Controls.Add(_sr2.panelReferenceSystem);

            btnBackgroundColor.BackColor = _display.BackgroundColor.ToGdiColor();

            txtTitle.Text = _map.Title;
            txtDescription.Text = _map.GetLayerDescription(Map.MapDescriptionId);
            txtCopyright.Text = _map.GetLayerCopyrightText(Map.MapCopyrightTextId);

            numFontScaleFactor.Value = (decimal)(SystemVariables.SystemFontsScaleFactor * 100f);

            #region Graphics Engine

            numEngineDpi.Value = (decimal)GraphicsEngine.Current.Engine.ScreenDpi;

            foreach (var engineName in Engines.RegisteredGraphicsEngineNames())
            {
                cmbGraphicsEngine.Items.Add(engineName);
            }
            cmbGraphicsEngine.SelectedItem = GraphicsEngine.Current.Engine.EngineName;

            #endregion Graphics Engine

            #region Current Display Values

            txtCurrentBBox.Text = _display.Envelope.ToBBoxString();

            int iWidth = (int)((float)_display.ImageWidth * 96f / (float)_display.Dpi);
            int iHeight = (int)((float)_display.ImageHeight * 96f / (float)_display.Dpi);
            txtCurrentSize.Text = $"{iWidth},{iHeight}";

            #endregion

            BuildResourcesList();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (_map == null || _display == null)
            {
                return;
            }

            try
            {
                bool refresh = false;

                _map.Name = txtName.Text;
                _display.ReferenceScale = Convert.ToDouble(numRefScale.Value);

                if (cmbMapUnits.Enabled)
                {
                    _display.MapUnits = ((GeoUnitsItem)cmbMapUnits.SelectedItem).Unit;
                }

                _display.DisplayUnits = ((GeoUnitsItem)cmbDisplayUnits.SelectedItem).Unit;

                _display.BackgroundColor = btnBackgroundColor.BackColor.ToArgbColor();

                ISpatialReference oldSRef = _display.SpatialReference;
                _display.SpatialReference = _sr.SpatialReference;

                if (oldSRef != null &&
                    !oldSRef.Equals(_display.SpatialReference))
                {
                    IEnvelope limit = _display.Limit;
                    IEnvelope env = _display.Envelope;

                    _display.Limit = GeometricTransformerFactory.Transform2D(
                        limit,
                        oldSRef,
                        _display.SpatialReference).Envelope;

                    _display.ZoomTo(
                        GeometricTransformerFactory.Transform2D(
                            env,
                            oldSRef,
                            _display.SpatialReference).Envelope
                        );
                }

                _map.LayerDefaultSpatialReference = _sr2.SpatialReference;

                _map.Title = txtTitle.Text;
                _map.SetLayerDescription(Map.MapDescriptionId, txtDescription.Text);
                _map.SetLayerCopyrightText(Map.MapCopyrightTextId, txtCopyright.Text);

                if (SystemVariables.SystemFontsScaleFactor != (float)numFontScaleFactor.Value / 100f)
                {
                    SystemVariables.SystemFontsScaleFactor = (float)numFontScaleFactor.Value / 100f;
                    _display.Screen?.RefreshSettings();

                    refresh = true;
                }

                #region Graphics Engine

                if (cmbGraphicsEngine.SelectedItem.ToString() != GraphicsEngine.Current.Engine.EngineName)
                {
                    var engine = Engines.RegisteredGraphcisEngines().Where(ge => ge.EngineName == cmbGraphicsEngine.SelectedItem.ToString()).FirstOrDefault();
                    if (engine != null)
                    {
                        GraphicsEngine.Current.Engine = engine;
                        RefreshFeatureRendererSymbolsGraphcisEngine();

                        refresh = true;
                    }
                }

                #endregion Graphics Engine

                if (refresh)
                {
                    if (_app != null)
                    {
                        _app.RefreshTOC();
                        _app.RefreshActiveMap(DrawPhase.All);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnBackgroundColor_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            dlg.Color = btnBackgroundColor.BackColor;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                btnBackgroundColor.BackColor = dlg.Color;
            }
        }

        private void btnTransparent_Click(object sender, EventArgs e)
        {
            btnBackgroundColor.BackColor = Color.FromArgb(0, 0, 0, 0);
        }

        private void btnAntialiasLabels_Click(object sender, EventArgs e)
        {
            SetLabelSmoothing(SymbolSmoothing.AntiAlias);
        }

        private void btnNoAntialiasLabels_Click(object sender, EventArgs e)
        {
            SetLabelSmoothing(SymbolSmoothing.None);
        }

        private void btnAntialiasFeatures_Click(object sender, EventArgs e)
        {
            SetFeatureSmooting(SymbolSmoothing.AntiAlias);
        }

        private void btnNoAntialiasFeatures_Click(object sender, EventArgs e)
        {
            SetFeatureSmooting(SymbolSmoothing.None);
        }

        private void SetLabelSmoothing(SymbolSmoothing smooting)
        {
            if (_map == null || _map.MapElements == null)
            {
                return;
            }

            foreach (IDatasetElement dsElement in _map.MapElements)
            {
                IFeatureLayer fLayer = dsElement as IFeatureLayer;
                if (fLayer == null || fLayer.LabelRenderer == null)
                {
                    continue;
                }

                ILabelRenderer lRenderer = fLayer.LabelRenderer;
                foreach (ISymbol symbol in lRenderer.Symbols)
                {
                    if (symbol == null)
                    {
                        continue;
                    }

                    symbol.SymbolSmothingMode = smooting;
                }
            }
            if (_app != null)
            {
                _app.RefreshActiveMap(DrawPhase.All);
            }
        }

        private void SetFeatureSmooting(SymbolSmoothing smooting)
        {
            if (_map == null || _map.MapElements == null)
            {
                return;
            }

            foreach (IDatasetElement dsElement in _map.MapElements)
            {
                IFeatureLayer fLayer = dsElement as IFeatureLayer;
                if (fLayer == null || fLayer.FeatureRenderer == null)
                {
                    continue;
                }

                IFeatureRenderer fRenderer = fLayer.FeatureRenderer;
                foreach (ISymbol symbol in fRenderer.Symbols)
                {
                    if (symbol == null)
                    {
                        continue;
                    }

                    symbol.SymbolSmothingMode = smooting;
                }
            }
            if (_app != null)
            {
                _app.RefreshActiveMap(DrawPhase.All);
            }
        }

        private void RefreshFeatureRendererSymbolsGraphcisEngine()
        {
            if (_map == null || _map.MapElements == null)
            {
                return;
            }

            foreach (IDatasetElement dsElement in _map.MapElements)
            {
                IFeatureLayer fLayer = dsElement as IFeatureLayer;
                if (fLayer == null || fLayer.FeatureRenderer == null)
                {
                    continue;
                }

                IFeatureRenderer fRenderer = fLayer.FeatureRenderer;
                foreach (ISymbol symbol in fRenderer.Symbols)
                {
                    if (symbol is ISymbolCurrentGraphicsEngineDependent)
                    {
                        ((ISymbolCurrentGraphicsEngineDependent)symbol).CurrentGraphicsEngineChanged();
                    }
                }
            }
        }

        #region MapResources

        private void btnAddResources_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                Multiselect = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (var filename in dlg.FileNames)
                {
                    FileInfo fi = new FileInfo(filename);

                    _map.ResourceContainer[fi.Name] = File.ReadAllBytes(fi.FullName);
                }

                BuildResourcesList();
            }
        }

        private void BuildResourcesList()
        {
            gridResources.Rows.Clear();

            if (_map?.ResourceContainer?.Names != null)
            {
                foreach (string name in _map.ResourceContainer.Names)
                {
                    var data = _map.ResourceContainer[name];
                    if (data == null || data.Length == 0)
                    {
                        continue;
                    }

                    var gridRow = new DataGridViewRow();
                    gridRow.Cells.Add(new DataGridViewTextBoxCell()
                    {
                        Value = name
                    });
                    gridRow.Cells.Add(new DataGridViewTextBoxCell()
                    {
                        Value = $"{Math.Round(data.Length / 1024.0, 2).ToString()}kb"
                    });
                    gridRow.Cells.Add(new DataGridViewButtonCell()
                    {
                        Value = "Remove"
                    });
                    gridResources.Rows.Add(gridRow);
                }
            }
        }

        private void gridResources_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_map?.ResourceContainer != null)
            {
                if (e.ColumnIndex == 2)  // remove
                {
                    var row = gridResources.Rows[e.RowIndex];

                    string resourceName = row.Cells[0].Value.ToString();

                    if (MessageBox.Show($"Remove resouorce {resourceName} from map?", "Remove", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _map.ResourceContainer[resourceName] = null;
                        gridResources.Rows.Remove(row);
                    }
                }
            }
        }

        #endregion MapResources

        async private void btnMapServiceMetadata_Click(object sender, EventArgs e)
        {
            XmlStream xmlStream = new XmlStream(String.Empty);
            this._map.ReadMetadata(xmlStream);

            FormMetadata dlg = new FormMetadata(xmlStream, this._map);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                await (this._map).WriteMetadata(await dlg.GetStream());
            }
        }

        private void btnDefaultLayerSRFromSpatialReference_Click(object sender, EventArgs e)
        {
            _sr2.SpatialReference = _sr.SpatialReference;
            _sr2.RefreshGUI();
        }
    }

    internal class GeoUnitsItem
    {
        private GeoUnits _unit;

        public GeoUnitsItem(GeoUnits unit)
        {
            _unit = unit;
        }

        public GeoUnits Unit
        {
            get { return _unit; }
        }

        public override string ToString()
        {
            return Unit2String(_unit);
        }

        private string Unit2String(GeoUnits unit)
        {
            switch (unit)
            {
                case GeoUnits.Unknown:
                    return unit.ToString();

                case GeoUnits.Inches:
                    return unit.ToString();

                case GeoUnits.Feet:
                    return unit.ToString();

                case GeoUnits.Yards:
                    return unit.ToString();

                case GeoUnits.Miles:
                    return unit.ToString();

                case GeoUnits.NauticalMiles:
                    return "Nautic Miles";

                case GeoUnits.Millimeters:
                    return unit.ToString();

                case GeoUnits.Centimeters:
                    return unit.ToString();

                case GeoUnits.Decimeters:
                    return unit.ToString();

                case GeoUnits.Meters:
                    return unit.ToString();

                case GeoUnits.Kilometers:
                    return unit.ToString();

                case GeoUnits.DecimalDegrees:
                    return "Decimal Degrees";

                case GeoUnits.DegreesMinutesSeconds:
                    return "Degrees Minutes Seconds";
            }
            return "???";
        }
    }
}