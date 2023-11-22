using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.IO;

namespace gView.Carto.Core.Abstractions;

public interface ICartoDocument : IPersistableLoadAsync
{
    string FilePath { get; set; }

    IMap Map { get; }

    ITableRelations TableRelations { get; }
}
