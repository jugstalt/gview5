﻿using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Data;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using MudBlazor.Charts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools
{
    [gView.Framework.system.RegisterPlugIn("4F54D455-1C22-469e-9DBB-78DBBEF6078D")]
    public class Delete : IExplorerTool
    {
        public string Name => "Delete";

        public string ToolTip => "";

        public string Icon => "basic:trashcan";

        public ExplorerToolTarget Target => ExplorerToolTarget.SelectedContextExplorerObjects;

        public bool IsEnabled(IExplorerApplicationScope scope)
        {
            var scopeService = scope.ToScopeService();

            return scopeService.ContextExplorerObjects?
                .Where(e => e is IExplorerObjectDeletable)
                .Count() > 0;
        }

        async public Task<bool> OnEvent(IExplorerApplicationScope scope)
        {
            var scopeService = scope.ToScopeService();

            if (scopeService.ContextExplorerObjects != null && scopeService.ContextExplorerObjects.Any())
            {
                var model = new DeleteObjectsModel()
                {
                    ExplorerObjects = new List<SelectItemModel<IExplorerObject>>(
                        scopeService.ContextExplorerObjects
                            .Where(e => e is IExplorerObjectDeletable)
                            .Select(e => new SelectItemModel<IExplorerObject>(e)))
                };


                model = await scopeService.ShowModalDialog(
                    typeof(Razor.Components.Dialogs.DeleteObjectsDialog),
                    this.Name,
                    model);

                if (model != null)
                {
                    foreach (var exObject in model.SelectedExplorerItems)
                    {
                       await (exObject as IExplorerObjectDeletable)!
                                    .DeleteExplorerObject(new ExplorerObjectEventArgs());
                    }
                }

                await scopeService.EventBus.FireFreshContentAsync();
            }

            return true;
        }

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion

        #region IOrder

        public int SortOrder => 25;

        #endregion
    }
}
