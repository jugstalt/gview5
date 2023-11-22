using gView.Framework.system;
using System.Windows.Forms;

namespace gView.Interoperability.OGC.UI.Dataset.WMS
{
    public partial class WMSMetadata : UserControl, IPlugInParameter
    {
        private WMS_Export_Metadata _metadata = null;

        public WMSMetadata()
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
                _metadata = value as WMS_Export_Metadata;
                epsgSelector1.Metadata = value as IEpsgMetadata;
            }
        }

        #endregion
    }
}
