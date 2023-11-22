using gView.Framework.IO;
using gView.Framework.system;

namespace gView.Framework.Network
{
    public interface IGraphWeightFeatureClass : IPersistable
    {
        int FcId { get; }
        string FieldName { get; set; }
        ISimpleNumberCalculation SimpleNumberCalculation { get; set; }
    }
}
