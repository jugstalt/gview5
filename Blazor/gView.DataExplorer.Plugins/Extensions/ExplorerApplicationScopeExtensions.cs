using gView.DataExplorer.Plugins.Services;
using gView.Framework.Blazor.Services.Abstraction;
using System;

namespace gView.DataExplorer.Plugins.Extensions;

static public class ExplorerApplicationScopeExtensions
{
    static public ExplorerApplicationScopeService ToExplorerScopeService(this IApplicationScope appScope)
        => appScope is ExplorerApplicationScopeService ?
            (ExplorerApplicationScopeService)appScope :
            throw new Exception("AppScope is not an Service. Appliation Service not registered correctly");
}
