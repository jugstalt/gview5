using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Metadata;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace gView.DataSources.TileCache.UI
{
    public partial class FormTileCacheConnection : Form
    {
        public FormTileCacheConnection()
        {
            InitializeComponent();

            cmbOrigin.SelectedIndex = 1;
            this.ConnectionString = String.Empty;

        }

        #region Properties

        public ISpatialReference SpatialReference { get; set; }

        public string ConnectionString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("name=" + txtName.Text);
                sb.Append(";extent=" + ((double)numLeft.Value).ToString(Numbers.Nhi) + "," + ((double)numBottom.Value).ToString(Numbers.Nhi) + "," + ((double)numRight.Value).ToString(Numbers.Nhi) + "," + ((double)numTop.Value).ToString(Numbers.Nhi));
                sb.Append(";origin=" + cmbOrigin.SelectedIndex);
                if (this.SpatialReference != null)
                {
                    sb.Append(";sref64=" + this.SpatialReference.ToBase64String());
                }

                sb.Append(";scales=");
                for (int i = 0; i < lstScales.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(((double)lstScales.Items[i]).ToString(Numbers.Nhi));
                }
                sb.Append(";tilewidth=" + (int)numTileWidth.Value);
                sb.Append(";tileheight=" + (int)numTileHeight.Value);
                sb.Append(";tileurl=" + txtTileUrl.Text);
                sb.Append(";copyright=" + txtCopyright.Text);

                return sb.ToString();
            }
            set
            {
                txtName.Text = ConfigTextStream.ExtractValue(value, "name");
                string extent = ConfigTextStream.ExtractValue(value, "extent");
                if (!String.IsNullOrEmpty(extent))
                {
                    string[] bbox = extent.Split(',');
                    numLeft.Value = (decimal)double.Parse(bbox[0], Numbers.Nhi);
                    numBottom.Value = (decimal)double.Parse(bbox[1], Numbers.Nhi);
                    numRight.Value = (decimal)double.Parse(bbox[2], Numbers.Nhi);
                    numTop.Value = (decimal)double.Parse(bbox[3], Numbers.Nhi);
                }
                string origin = ConfigTextStream.ExtractValue(value, "origin");
                if (!String.IsNullOrEmpty(origin))
                {
                    cmbOrigin.SelectedIndex = int.Parse(origin);
                }

                string sref64 = ConfigTextStream.ExtractValue(value, "sref64");
                if (!String.IsNullOrEmpty(sref64))
                {
                    this.SpatialReference = new SpatialReference();
                    this.SpatialReference.FromBase64String(sref64);
                    txtSpatialReference.Text = this.SpatialReference.Name + "/" + this.SpatialReference.Description;
                }
                string scales = ConfigTextStream.ExtractValue(value, "scales");
                lstScales.Items.Clear();
                foreach (string scale in scales.Split(','))
                {
                    if (String.IsNullOrEmpty(scale))
                    {
                        continue;
                    }

                    lstScales.Items.Add(double.Parse(scale, Numbers.Nhi));
                }
                string tileWidth = ConfigTextStream.ExtractValue(value, "tilewidth");
                if (!String.IsNullOrEmpty(tileWidth))
                {
                    numTileWidth.Value = int.Parse(tileWidth);
                }

                string tileHeight = ConfigTextStream.ExtractValue(value, "tileheight");
                if (!String.IsNullOrEmpty(tileHeight))
                {
                    numTileHeight.Value = int.Parse(tileHeight);
                }

                txtTileUrl.Text = ConfigTextStream.ExtractValue(value, "tileurl");
                txtCopyright.Text = ConfigTextStream.ExtractValue(value, "copyright");
            }
        }

        public string TileCacheName
        {
            get { return txtName.Text; }
            set { txtName.Text = value; }
        }

        #endregion

        #region Events

        private void btnSpatialReference_Click(object sender, EventArgs e)
        {
            FormSpatialReference dlg = new FormSpatialReference(this.SpatialReference);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.SpatialReference = dlg.SpatialReference;
                if (this.SpatialReference != null)
                {
                    txtSpatialReference.Text = this.SpatialReference.Name + "/" + this.SpatialReference.Description;
                }
            }
        }

        async private void btnImport_Click(object sender, EventArgs e)
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
            filters.Add(new OpenDataFilter());

            ExplorerDialog dlg = new ExplorerDialog("Import Extent", filters, true);
            dlg.MulitSelection = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                IEnvelope bounds = null;
                foreach (IExplorerObject exObject in dlg.ExplorerObjects)
                {
                    if (exObject == null)
                    {
                        continue;
                    }

                    var instance = await exObject.GetInstanceAsync();
                    if (instance == null)
                    {
                        continue;
                    }

                    IEnvelope objEnvelope = null;

                    if (instance is IDataset)
                    {
                        foreach (IDatasetElement element in await ((IDataset)instance).Elements())
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            objEnvelope = ClassEnvelope(element.Class, this.SpatialReference);
                        }
                    }
                    else
                    {
                        objEnvelope = ClassEnvelope(instance as IClass, this.SpatialReference);
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
                    numLeft.Value = (decimal)bounds.minx;
                    numRight.Value = (decimal)bounds.maxx;
                    numBottom.Value = (decimal)bounds.miny;
                    numTop.Value = (decimal)bounds.maxx;
                }
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

                lstScales.Items.Clear();
                foreach (double s in scales.InnerList)
                {
                    lstScales.Items.Add(s);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            lstScales.Items.Remove(lstScales.SelectedItem);
        }

        private void lstScales_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemove.Enabled = lstScales.SelectedIndex >= 0;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xml Datei|*.xml";

            dlg.InitialDirectory = SystemVariables.ApplicationDirectory + @"\misc\tiling\import";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                XmlStream stream = new XmlStream("TileService");
                stream.ReadStream(dlg.FileName);

                this.LoadStream(stream);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Xml Datei|*.xml";

            dlg.InitialDirectory = SystemVariables.ApplicationDirectory + @"\misc\tiling\import";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                XmlStream stream = new XmlStream("TileCache");
                this.SaveStream(stream);
                stream.WriteStream(dlg.FileName);
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
                IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, classSRef, sRef);
                if (geom == null)
                {
                    return null;
                }

                envelope = geom.Envelope;
            }
            return envelope;
        }

        #endregion

        #region Persistable Member

        public void LoadStream(IPersistStream stream)
        {
            txtName.Text = (string)stream.Load("name", String.Empty);

            numTileWidth.Value = (int)stream.Load("tile_width", 100);
            numTileHeight.Value = (int)stream.Load("tile_height", 100);

            lstScales.Items.Clear();
            int scales_count = (int)stream.Load("scales_count", 0);
            for (int i = 0; i < scales_count; i++)
            {
                double s = (double)stream.Load("scale" + i, (double)0D);
                if (s == 0D)
                {
                    continue;
                }

                lstScales.Items.Add(s);
            }

            numLeft.Value = (decimal)(double)stream.Load("extent_minx0", 0D);
            numBottom.Value = (decimal)(double)stream.Load("extent_miny0", 0D);
            numRight.Value = (decimal)(double)stream.Load("extent_maxx0", 0D);
            numTop.Value = (decimal)(double)stream.Load("extent_maxy0", 0D);

            cmbOrigin.SelectedIndex = (int)stream.Load("extent_origin", 0);

            this.SpatialReference = (ISpatialReference)stream.Load("SpatialReference", null, new SpatialReference());
            if (this.SpatialReference != null)
            {
                txtSpatialReference.Text = this.SpatialReference.Name + "/" + this.SpatialReference.Description;
            }

            txtTileUrl.Text = (string)stream.Load("tile_url", String.Empty);
            txtCopyright.Text = (string)stream.Load("copyright", String.Empty);
        }

        public void SaveStream(IPersistStream stream)
        {
            stream.Save("name", txtName.Text);

            stream.Save("tile_width", (int)numTileWidth.Value);
            stream.Save("tile_height", (int)numTileHeight.Value);

            stream.Save("scales_count", lstScales.Items.Count);
            int counter = 0;
            foreach (double scale in lstScales.Items)
            {
                if (scale <= 0.0)
                {
                    continue;
                }

                stream.Save("scale" + counter, scale);
                counter++;
            }

            stream.Save("extent_minx0", (double)numLeft.Value);
            stream.Save("extent_miny0", (double)numBottom.Value);
            stream.Save("extent_maxx0", (double)numRight.Value);
            stream.Save("extent_maxy0", (double)numTop.Value);

            stream.Save("extent_origin", cmbOrigin.SelectedIndex);

            if (this.SpatialReference != null)
            {
                stream.Save("SpatialReference", this.SpatialReference);
            }

            stream.Save("tile_url", txtTileUrl.Text);
            stream.Save("copyright", txtCopyright.Text);
        }

        #endregion
    }
}
