using gView.Framework.system;
using System.Windows.Forms;

namespace gView.Interoperability.OGC.UI.Dataset.WFS
{
    public partial class WFSMetadata : UserControl, IPlugInParameter
    {
        private WFS_Export_Metadata _metadata = null;


        public WFSMetadata()
        {
            InitializeComponent();
        }

        #region IPlugInParameter Member

        public object Parameter
        {
            get
            {
                return _metadata;
            }
            set
            {
                _metadata = value as WFS_Export_Metadata;
                epsgSelector1.Metadata = value as IEpsgMetadata;
            }
        }

        #endregion
    }
}
