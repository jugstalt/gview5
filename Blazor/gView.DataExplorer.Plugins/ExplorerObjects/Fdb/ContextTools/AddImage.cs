using gView.Cmd.Core.Abstraction;
using gView.Cmd.Fdb.Lib;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataSources.Fdb.MSSql;
using gView.DataSources.Fdb.PostgreSql;
using gView.DataSources.Fdb.SQLite;
using gView.DataSources.GDAL;
using gView.DataSources.Raster.File;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
internal class AddImage : IExplorerObjectContextTool
{
    public string Name => "Add Single Image";

    public string Icon => "basic:round-plus";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return true;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var scopeService = scope.ToExplorerScopeService();
        var instance = await exObject.GetInstanceAsync() as IRasterCatalogClass;

        var model = await scopeService.ShowKnownDialog(Framework.Blazor.KnownDialogs.ExplorerDialog,
                                                       title: "Select Image",
                                                       model: new ExplorerDialogModel()
                                                       {
                                                           Filters = new List<ExplorerDialogFilter> {
                                                                new OpenRasterClassFilter()
                                                           },
                                                           Mode = ExploerDialogMode.Open
                                                       });

        if (model != null && model.Result.ExplorerObjects.Count == 1)
        {
            var rasterFile = await model.Result.ExplorerObjects.First().GetInstanceAsync() as IRasterFile;
            if (rasterFile == null)
            {
                throw new System.Exception("Image is not an RasterFile");
            }

            IDictionary<string, object> parameters = new Dictionary<string, object>();
            ICommand command = new ImageDatasetUtilCommand();

            parameters.Add("fdb", instance switch
            {
                SqlFDBImageCatalogClass => "sql",
                pgImageCatalogClass => "postgres",
                SQLiteFDBImageCatalogClass => "sqlite",
                _ => throw new System.Exception($"Can't add images to {instance?.GetType().Name}")
            });
            parameters.Add("connstr", instance.Dataset.ConnectionString);

            parameters.Add("job", "add");
            parameters.Add("filename", rasterFile.Filename);

            parameters.Add("provider", rasterFile switch
            {
                RasterFileClass => "raster",
                MrSidFileClass => "raster",
                RasterClassV1 => "gdal",
                RasterClassV3 => "gdal",
                _ => "first"
            });


            await scopeService.ShowKnownDialog(
                    KnownDialogs.ExecuteCommand,
                    $"Add File",
                    new ExecuteCommandModel()
                    {
                        CommandItems = new[]
                        {
                            new CommandItem()
                            {
                                Command = command,
                                Parameters = parameters
                            }
                        }
                    });

            return true;
        }

        return false;
    }
}
