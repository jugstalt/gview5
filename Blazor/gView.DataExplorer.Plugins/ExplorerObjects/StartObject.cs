using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects;
internal class StartObject : ExplorerParentObject,
                              IExplorerObject
{
    public StartObject()
        : base()
    {
    }

    #region IExplorerObject Member

    public string Name
    {
        get { return "Start"; }
    }

    public string FullName
    {
        get { return ""; }
    }

    public string? Type
    {
        get { return ""; }
    }

    public string Icon => "basic:home";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (FullName == String.Empty)
        {
            return Task.FromResult<IExplorerObject?>(new StartObject());
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();


        PlugInManager compMan = new PlugInManager();

        foreach (var exObjectType in compMan.GetPlugins(Framework.system.Plugins.Type.IExplorerObject))
        {
            var exObject = compMan.CreateInstance<IExplorerObject>(exObjectType);

            if (!(exObject is IExplorerGroupObject))
            {
                continue;
            }

            ((IExplorerGroupObject)exObject).SetParentExplorerObject(this);
            base.AddChildObject(exObject);
        }

        return true;
    }

    #endregion
}
