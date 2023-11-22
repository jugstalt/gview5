using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Proj;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using gView.Framework.UI.Dialogs;
using gView.Interoperability.Server;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace gView.Framework.Metadata.UI
{
    public partial class TileServiceMetadataControl : UserControl, IPlugInParameter, IMetadataObjectParameter
    {
        private TileServiceMetadata _metadata = null;
        private MapServerClass _mapServerClass = null;
        private int _currentEpsg = -1;
        private List<int> _startupEpsg;

        public TileServiceMetadataControl()
        {
            InitializeComponent();
        }

        #region Properties
        public IEnvelope CurrentEpsgExtent
        {
            get
            {
                return new Envelope(
                    (double)numLeft.Value, (double)numBottom.Value,
                    (double)numRight.Value, (double)numTop.Value);
            }
            set
            {
                num_ReleaseEventHandler();
                if (value == null)
                {
                    numLeft.Value = numBottom.Value = numRight.Value = numTop.Value = (decimal)0.0;
                }
                else
                {
                    numLeft.Value = (decimal)value.minx;
                    numBottom.Value = (decimal)value.miny;
                    numRight.Value = (decimal)value.maxx;
                    numTop.Value = (decimal)value.maxy;
                }
                num_SetEventHandlers();
            }
        }
        public IPoint CurrentEpsgOriginUpperLeft
        {
            get
            {
                return new gView.Framework.Geometry.Point((double)numUL_x.Value, (double)numUL_y.Value);
            }
            set
            {
                num_ReleaseEventHandler();
                if (value == null)
                {
                    numUL_x.Value = numUL_y.Value = (decimal)0.0;
                }
                else
                {
                    numUL_x.Value = (decimal)value.X;
                    numUL_y.Value = (decimal)value.Y;
                }
                num_SetEventHandlers();
            }
        }
        public IPoint CurrentEpsgOriginLowerLeft
        {
            get
            {
                return new gView.Framework.Geometry.Point((double)numLL_x.Value, (double)numLL_y.Value);
            }
            set
            {
                num_ReleaseEventHandler();
                if (value == null)
                {
                    numLL_x.Value = numLL_y.Value = (decimal)0.0;
                }
                else
                {
                    numLL_x.Value = (decimal)value.X;
                    numLL_y.Value = (decimal)value.Y;
                }
                num_SetEventHandlers();
            }
        }

        private void lstScales_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemove.Enabled = lstScales.SelectedIndex >= 0;
        }

        #region UI

        public bool ShowUseTileCacheCheckbox
        {
            get { return chkUseTiling.Visible; }
            set { chkUseTiling.Visible = value; }
        }

        public bool ShowTileImageFormat
        {
            get { return gbTileFormat.Visible; }
            set { gbTileFormat.Visible = value; }
        }

        public bool ShowCacheTilesCheckBoxes
        {
            get { return chkUpperLeftCacheTiles.Visible || chkLowerLeftCacheTiles.Visible; }
            set
            {
                chkUpperLeftCacheTiles.Visible = chkLowerLeftCacheTiles.Visible = value;
            }
        }

        public bool ShowOrigionGroupBox
        {
            get { return gbOrigin.Visible; }
            set { gbOrigin.Visible = value; }
        }

        #endregion

        #endregion

        #region IPlugInParameter Member

        public object Parameter
        {
            get
            {
                return _metadata;
            }
            set
            {
                lstScales.Items.Clear();
                _metadata = value as TileServiceMetadata;

                if (_metadata != null)
                {
                    chkUseTiling.Checked = _metadata.Use;
                    chkRenderOnTheFly.Checked = _metadata.RenderTilesOnTheFly;

                    numTileWidth.Value = _metadata.TileWidth;
                    numTileHeight.Value = _metadata.TileHeight;

                    //gpExtent.Enabled = gpTileSize.Enabled = !(_metadata.Extent.Width > 0 && _metadata.Extent.Height > 0);
                    _startupEpsg = ListOperations<int>.Clone(_metadata.EPSGCodes);

                    FillScaleList();

                    chkUpperLeft.Checked = _metadata.UpperLeft;
                    chkLowerLeft.Checked = _metadata.LowerLeft;
                    chkUpperLeftCacheTiles.Checked = _metadata.UpperLeftCacheTiles;
                    chkLowerLeftCacheTiles.Checked = _metadata.LowerLeftCacheTiles;

                    chkPng.Checked = _metadata.FormatPng;
                    chkJpg.Checked = _metadata.FormatJpg;
                    chkTransparentPng.Checked = _metadata.MakeTransparentPng;

                    FillEpsgList();

                    lblResolutinDpi.Text = "Resoultion DPI: " + _metadata.Dpi.ToString();
                }
            }
        }

        #endregion

        #region Events
        async private void btnImport_Click(object sender, EventArgs e)
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
            filters.Add(new OpenDataFilter());

            ExplorerDialog dlg = new ExplorerDialog("Import Extent", filters, true);
            dlg.MulitSelection = true;

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + cmbEpsgs.SelectedItem.ToString());

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                IEnvelope bounds = null;
                foreach (IExplorerObject exObject in dlg.ExplorerObjects)
                {
                    var objectInstance = await exObject?.GetInstanceAsync();
                    if (objectInstance == null)
                    {
                        continue;
                    }

                    IEnvelope objEnvelope = null;

                    if (objectInstance is IDataset)
                    {
                        foreach (IDatasetElement element in await ((IDataset)objectInstance).Elements())
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            objEnvelope = ClassEnvelope(element.Class, sRef);
                        }
                    }
                    else
                    {
                        objEnvelope = ClassEnvelope(objectInstance as IClass, sRef);
                    }

                    if (objEnvelope != null)
                    {
                        if (bounds == null)
                        {
                            bounds = new Envelope(objEnvelope);
                        }
                        else
                        {
                            bounds.Union(objEnvelope);
                        }
                    }
                }

                if (bounds != null)
                {
                    this.CurrentEpsgExtent = bounds;
                }
            }

            if (_metadata != null && cmbEpsgs.SelectedIndex >= 0)
            {
                _metadata.SetEPSGEnvelope(_currentEpsg, this.CurrentEpsgExtent);
            }
        }

        private void numTileWidth_ValueChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.TileWidth = (int)numTileWidth.Value;
            }
        }

        private void numTileHeight_ValueChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.TileHeight = (int)numTileHeight.Value;
            }
        }

        private void chkUseTiling_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.Use = chkUseTiling.Checked;
            }
        }

        private void num_ReleaseEventHandler()
        {
            numBottom.ValueChanged -= new EventHandler(num_ValueChanged);
            numTop.ValueChanged -= new EventHandler(num_ValueChanged);
            numLeft.ValueChanged -= new EventHandler(num_ValueChanged);
            numRight.ValueChanged -= new EventHandler(num_ValueChanged);

            numUL_x.ValueChanged -= new EventHandler(num_ULValueChanged);
            numUL_y.ValueChanged -= new EventHandler(num_ULValueChanged);

            numLL_x.ValueChanged -= new EventHandler(num_LLValueChanged);
            numLL_y.ValueChanged -= new EventHandler(num_LLValueChanged);
        }
        private void num_SetEventHandlers()
        {
            numBottom.ValueChanged += new EventHandler(num_ValueChanged);
            numTop.ValueChanged += new EventHandler(num_ValueChanged);
            numLeft.ValueChanged += new EventHandler(num_ValueChanged);
            numRight.ValueChanged += new EventHandler(num_ValueChanged);

            numUL_x.ValueChanged += new EventHandler(num_ULValueChanged);
            numUL_y.ValueChanged += new EventHandler(num_ULValueChanged);

            numLL_x.ValueChanged += new EventHandler(num_LLValueChanged);
            numLL_y.ValueChanged += new EventHandler(num_LLValueChanged);
        }
        private void num_ValueChanged(object sender, EventArgs e)
        {
            if (_metadata != null && cmbEpsgs.SelectedIndex >= 0)
            {
                _metadata.SetEPSGEnvelope(_currentEpsg, this.CurrentEpsgExtent);
            }
        }
        private void num_ULValueChanged(object sender, EventArgs e)
        {
            if (_metadata != null && cmbEpsgs.SelectedIndex >= 0)
            {
                _metadata.SetOriginUpperLeft(_currentEpsg, this.CurrentEpsgOriginUpperLeft);
            }
        }
        private void num_LLValueChanged(object sender, EventArgs e)
        {
            if (_metadata != null && cmbEpsgs.SelectedIndex >= 0)
            {
                _metadata.SetOriginLowerLeft(_currentEpsg, this.CurrentEpsgOriginLowerLeft);
            }
        }

        private void cmbEpsgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnImport.Enabled = false;
            if (_metadata != null)
            {
                if (_currentEpsg != -1)
                {
                    _metadata.SetEPSGEnvelope(_currentEpsg, this.CurrentEpsgExtent);
                    _metadata.SetOriginUpperLeft(_currentEpsg, this.CurrentEpsgOriginUpperLeft);
                    _metadata.SetOriginLowerLeft(_currentEpsg, this.CurrentEpsgOriginLowerLeft);
                }

                _currentEpsg = (int)cmbEpsgs.SelectedItem;
                this.CurrentEpsgExtent = _metadata.GetEPSGEnvelope((int)cmbEpsgs.SelectedItem);
                this.CurrentEpsgOriginUpperLeft = _metadata.GetOriginUpperLeft((int)cmbEpsgs.SelectedItem);
                this.CurrentEpsgOriginLowerLeft = _metadata.GetOriginLowerLeft((int)cmbEpsgs.SelectedItem);

                panelExtent.Enabled = (_startupEpsg != null ?
                    !(_startupEpsg.Contains(_currentEpsg) && this.CurrentEpsgExtent.Width > 0 && this.CurrentEpsgExtent.Height > 0)
                    : false);

                //numUL_x.Enabled = numUL_y.Enabled = numLL_x.Enabled = numLL_y.Enabled = panelExtent.Enabled;

                btnImport.Enabled = cmbEpsgs.SelectedIndex >= 0;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if ((int)numScale.Value > 0.0)
            {
                TileServiceMetadata.DoubleScales scales = new TileServiceMetadata.DoubleScales(new List<double>());
                foreach (double s in lstScales.Items)
                {
                    scales.InnerList.Add(s);
                }
                if (scales.Contains((double)numScale.Value))
                {
                    return;
                }

                scales.InnerList.Add((double)numScale.Value);

                scales.Order();
                if (_metadata != null)
                {
                    _metadata.Scales = scales;
                }

                FillScaleList();
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.Scales.InnerList.Remove(Convert.ToInt32(lstScales.SelectedItem));
            }

            FillScaleList();
        }

        private void chkUpperLeftCacheTiles_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.UpperLeftCacheTiles = chkUpperLeftCacheTiles.Checked;
            }
        }

        private void chkLowerLeftCacheTiles_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.LowerLeftCacheTiles = chkLowerLeftCacheTiles.Checked;
            }
        }

        private void chkUpperLeft_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.UpperLeft = chkUpperLeft.Checked;
            }
        }

        private void chkLowerLeft_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.LowerLeft = chkLowerLeft.Checked;
            }
        }

        private void chkPng_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.FormatPng = chkPng.Checked;
            }
        }

        private void chkTransparentPng_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.MakeTransparentPng = chkTransparentPng.Checked;
            }
        }

        private void chkRenderOnTheFly_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.RenderTilesOnTheFly = chkRenderOnTheFly.Checked;
            }
        }

        private void chkJpg_CheckedChanged(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                _metadata.FormatJpg = chkJpg.Checked;
            }
        }

        private void btnAddEpsg_Click(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                ProjDBTables tables = new ProjDBTables();
                FormSpatialReferenceSystems dlg = new FormSpatialReferenceSystems(tables);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ISpatialReference sRef = dlg.SpatialRefererence;
                        if (!sRef.Name.ToLower().StartsWith("epsg:"))
                        {
                            MessageBox.Show("Only spatialreference system with EPSG-Code allowed");
                            return;
                        }
                        int epsg = int.Parse(sRef.Name.Split(':')[1]);
                        if (!_metadata.EPSGCodes.Contains(epsg))
                        {
                            _metadata.EPSGCodes.Add(epsg);
                        }

                        FillEpsgList();
                        cmbEpsgs.SelectedItem = epsg;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }
            }
        }

        private void btnRemoveEpsg_Click(object sender, EventArgs e)
        {
            if (_metadata != null && cmbEpsgs.SelectedItem != null)
            {
                if (_metadata.EPSGCodes.Contains((int)cmbEpsgs.SelectedItem))
                {
                    _metadata.EPSGCodes.Remove((int)cmbEpsgs.SelectedItem);

                    _startupEpsg.Remove((int)cmbEpsgs.SelectedItem);
                    cmbEpsgs.Items.Remove(cmbEpsgs.SelectedItem);

                    if (cmbEpsgs.Items.Count > 0)
                    {
                        cmbEpsgs.SelectedIndex = 0;
                    }
                }
            }
        }
        #endregion

        #region Helper
        private IEnvelope ClassEnvelope(IClass Class, ISpatialReference sRef)
        {
            IEnvelope envelope = null;
            ISpatialReference classSRef = null;

            if (Class is IFeatureClass)
            {
                envelope = ((IFeatureClass)Class).Envelope;
                classSRef = ((IFeatureClass)Class).SpatialReference;
            }
            else if (Class is IRasterClass && ((IRasterClass)Class).Polygon != null)
            {
                envelope = ((IRasterClass)Class).Polygon.Envelope;
                classSRef = ((IRasterClass)Class).SpatialReference;
            }

            if (envelope != null && classSRef != null && sRef != null && !sRef.Equals(classSRef))
            {
                using (var geometricTransformer = GeometricTransformerFactory.Create())
                {
                    geometricTransformer.SetSpatialReferences(classSRef, sRef);
                    IGeometry geom = geometricTransformer.Transform2D(envelope) as IGeometry;
                    if (geom == null)
                    {
                        return null;
                    }

                    envelope = geom.Envelope;
                }
            }
            return envelope;
        }

        private void FillScaleList()
        {
            lstScales.Items.Clear();
            if (_metadata != null)
            {
                foreach (double scale in _metadata.Scales.InnerList)
                {
                    lstScales.Items.Add(scale);
                }
            }
        }

        private void FillEpsgList()
        {
            cmbEpsgs.Items.Clear();
            if (_metadata != null)
            {
                foreach (int epsg in _metadata.EPSGCodes)
                {
                    cmbEpsgs.Items.Add(epsg);
                }
            }
            if (cmbEpsgs.Items.Count > 0)
            {
                cmbEpsgs.SelectedIndex = 0;
            }
        }
        #endregion

        #region IMetadataObjectParameter Member

        public object MetadataObject
        {
            get
            {
                return _mapServerClass;
            }
            set
            {
                _mapServerClass = value as MapServerClass;
            }
        }

        #endregion

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Xml Datei|*.xml";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    XmlStream stream = new XmlStream("TileService");
                    _metadata.Save(stream);
                    stream.WriteStream(dlg.FileName);
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (_metadata != null)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Xml Datei|*.xml";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    XmlStream stream = new XmlStream("TileService");
                    stream.ReadStream(dlg.FileName);
                    _metadata.Load(stream);

                    this.Parameter = _metadata;
                }
            }
        }

    }
}
