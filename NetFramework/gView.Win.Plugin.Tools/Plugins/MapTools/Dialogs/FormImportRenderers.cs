using System.Windows.Forms;

namespace gView.Plugins.MapTools.Dialogs
{
    internal partial class FormImportRenderers : Form
    {
        public FormImportRenderers()
        {
            InitializeComponent();
        }

        public bool FeatureRenderer
        {
            get { return chkFeatureRenderer.Checked; }
        }
        public bool LabelRenderer
        {
            get { return chkLabelRenderer.Checked; }
        }
        public bool SelectionRenderer
        {
            get { return chkSelectionRenderer.Checked; }
        }

        public bool RenderScales
        {
            get { return chkRenderScales.Checked; }
        }

        public bool LabelScales
        {
            get { return chkLabelScales.Checked; }
        }

        public bool FilterQuery
        {
            get { return chkFilterQuery.Checked; }
        }
    }
}