using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.FDB;
using gView.Framework.system;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class SaveFeatureClassFilters
{
    public static List<ExplorerDialogFilter> AllFilters
    {
        get
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();

            PlugInManager compMan = new PlugInManager();
            bool dsAdded = false;
            foreach (var dsType in compMan.GetPlugins(Plugins.Type.IDataset))
            {
                var ds = compMan.CreateInstance<IDataset>(dsType);
                if (ds == null ||
                    ds.Database == null)
                {
                    continue;
                }

                if (ds.Database is IFileFeatureDatabase)
                {
                    filters.Add(new SaveFileFeatureClass(ds));
                }
                else if (ds is IFeatureUpdater)
                {
                    if (!dsAdded)
                    {
                        filters.Insert(0, new SaveFeatureClass());
                        dsAdded = true;
                    }
                }
            }

            return filters;
        }
    }

    public static List<ExplorerDialogFilter> DatabaseFilters
    {
        get
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();

            PlugInManager compMan = new PlugInManager();
            bool dsAdded = false;
            foreach (var dsType in compMan.GetPlugins(Plugins.Type.IDataset))
            {
                var ds = compMan.CreateInstance<IDataset>(dsType);
                if (ds == null ||
                    ds.Database == null)
                {
                    continue;
                }

                if (ds.Database is IFileFeatureDatabase)
                {
                    continue;
                }
                else if (ds.Database is IFeatureUpdater)
                {
                    if (!dsAdded)
                    {
                        filters.Insert(0, new SaveFeatureClass());
                        dsAdded = true;
                    }
                }
            }

            return filters;
        }
    }

    private class SaveFeatureClass : ExplorerDialogFilter
    {
        public SaveFeatureClass()
            : base("Feature Class")
        {
        }

        async public override Task<bool> Match(IExplorerObject exObject)
        {
            if (exObject == null)
            {
                return false;
            }

            var instance = await exObject.GetInstanceAsync();
            if (instance is IDataset &&
               ((IDataset)instance).Database is IFeatureUpdater)
            {
                return true;
            }

            return false;
        }
    }

    private class SaveFileFeatureClass : ExplorerDialogFilter
    {
        IDataset _ds;
        public SaveFileFeatureClass(IDataset ds)
            : base(((IFileFeatureDatabase)ds.Database).DatabaseName)
        {
            _ds = ds;
        }

        public override Task<bool> Match(IExplorerObject exObject)
        {
            try
            {
                return Task.FromResult(Directory.Exists(exObject.FullName));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public override object FilterObject
        {
            get
            {
                return _ds;
            }
        }
    }
}
