using gView.Carto.Core.Abstractions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.IO;
using System.Threading.Tasks;
using gView.Framework.Data.Relations;

namespace gView.Carto.Core;

public class CartoDocument : ICartoDocument
{
    public CartoDocument(string name = "map1")
    {
        this.Name = name;
        this.Map = new Map() { Name = name };
    }

    #region ICartoDocument

    public string Name { get; set; }
    public string FilePath { get; set; } = "";

    public IMap Map { get; private set; }

    public ITableRelations TableRelations { get; private set; } = new TableRelations(null);

    #endregion

    #region IPersistableLoadAsync

    async public Task<bool> LoadAsync(IPersistStream stream)
    {
        // Kompatibility to older versions:
        // more maps in one document were possible...
        int focusIndex = (int)stream.Load("focusMapIndex", 0);

        IMap map;
        int mapIndex = 0;
        while ((map = (await stream.LoadAsync<IMap>("IMap", new Map()))) != null)
        {
            if (mapIndex == focusIndex)
            {
                this.Map = map;
            }
            mapIndex++;
        }
        this.Name = this.Map.Name;

        this.TableRelations = (TableRelations)stream.Load("TableRelations", new TableRelations(null), new TableRelations(null));

        // ToDo:
        // Modules, eg MapEditLayers

        return true;
    }

    public void Save(IPersistStream stream)
    {
        stream.Save("focusMapIndex", 0);  // Compatibility to older versions

        stream.Save("IMap", this.Map);
        stream.Save("TableRelations", this.TableRelations);

        // ToDo:
        // Modules, eg MapEditLayers
    }

    #endregion
}
