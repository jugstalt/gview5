using gView.Framework.Sys.UI.Extensions;
using gView.Framework.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Dialogs
{
    internal partial class FormLegend : Form
    {
        private IMapDocument _doc;

        public FormLegend(IMapDocument mapDocument)
        {
            InitializeComponent();

            _doc = mapDocument;

            if (_doc.Application is IMapApplication)
            {
                this.ShowInTaskbar = false;
                this.TopLevel = true;
                this.Owner = ((IMapApplication)_doc.Application).DocumentWindow as Form;
            }
        }

        async private void FormLegend_Load(object sender, EventArgs e)
        {
            await RefreshLegend();
        }

        async public Task RefreshLegend()
        {
            if (!this.Visible)
            {
                return;
            }

            if (_doc != null && _doc.FocusMap != null && _doc.FocusMap.TOC != null)
            {
                pictureBox1.Image = (await _doc.FocusMap.TOC.Legend()).CloneToGdiBitmap();
                pictureBox1.Width = pictureBox1.Image.Width;
                pictureBox1.Height = pictureBox1.Image.Height;
            }
        }
    }
}