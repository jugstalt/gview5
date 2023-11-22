using gView.Framework.system;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace gView.Blazor.Core.Services;

public class PluginManagerService
{
    private readonly PlugInManager _plugInManager;

    public PluginManagerService()
    {
        _plugInManager = new PlugInManager();
    }

    public IEnumerable<T> GetPlugins<T>(Plugins.Type pluginType)
        => _plugInManager
            .GetPlugins(pluginType)
            .Select(t => _plugInManager.CreateInstance<T>(t))
            .ToArray();
    

    public IEnumerable<System.Type> GetPluginTypes(Plugins.Type pluginType)
        => _plugInManager
            .GetPlugins(pluginType)
            .ToArray();
}
