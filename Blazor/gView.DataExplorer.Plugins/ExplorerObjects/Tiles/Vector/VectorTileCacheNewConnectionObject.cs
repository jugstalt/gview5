using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Vector
{
    [gView.Framework.system.RegisterPlugIn("4C43C243-43D7-4B57-AD52-504659561C6E")]
    public class VectorTileCacheNewConnectionObject : ExplorerObjectCls<VectorTileCacheGroupExplorerObject>,
                                                      IExplorerSimpleObject,
                                                      IExplorerObjectDoubleClick,
                                                      IExplorerObjectCreatable
    {
        public VectorTileCacheNewConnectionObject()
            : base() { }
        public VectorTileCacheNewConnectionObject(VectorTileCacheGroupExplorerObject parent)
            : base(parent, 0) { }

        #region IExplorerSimpleObject Members

        public string Icon => "basic:round-plus";

        #endregion

        #region IExplorerObject Members

        public string Name => "New Connection...";

        public string FullName => "";

        public string Type => "New Tile Cache Connection";

        public void Dispose()
        {

        }

        public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

        #endregion

        #region IExplorerObjectDoubleClick Members

        async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
        {
            var model = await appScope.ToExplorerScopeService().ShowModalDialog(
                                typeof(gView.DataExplorer.Razor.Components.Dialogs.VectorTileCacheConnectionDialog),
                                "Vector-Tile-Cache Connection",
                                new VectorTileCacheConnectionModel());

            if (model != null)
            {
                ConfigConnections connStream = new ConfigConnections("VectorTileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");

                string connectionString = model.ToConnectionString();
                string id = model.Name;
                id = connStream.GetName(id);

                connStream.Add(id, connectionString);
                e.NewExplorerObject = new VectorTileCacheDatasetExplorerObject(this.TypedParent, id, connectionString);
            }
        }

        #endregion

        #region ISerializableExplorerObject Member

        public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
        {
            if (cache?.Contains(FullName) == true)
            {
                return Task.FromResult<IExplorerObject?>(cache[FullName]);
            }

            return Task.FromResult<IExplorerObject?>(null);
        }

        #endregion

        #region IExplorerObjectCreatable Member

        public bool CanCreate(IExplorerObject parentExObject)
        {
            return (parentExObject is VectorTileCacheGroupExplorerObject);
        }

        async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
        {
            ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();
            await ExplorerObjectDoubleClick(appScope, e);
            return e.NewExplorerObject;
        }

        #endregion
    }
}
