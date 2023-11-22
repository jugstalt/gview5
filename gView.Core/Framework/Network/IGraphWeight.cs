using gView.Framework.IO;
using System;

namespace gView.Framework.Network
{
    public interface IGraphWeight : IPersistable
    {
        string Name { get; set; }
        Guid Guid { get; }
        GraphWeightDataType DataType { get; set; }

        GraphWeightFeatureClasses FeatureClasses { get; }
    }
}
