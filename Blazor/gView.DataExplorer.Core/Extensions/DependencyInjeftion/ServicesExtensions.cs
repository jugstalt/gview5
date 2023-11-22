using gView.DataExplorer.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace gView.DataExplorer.Core.Extensions.DependencyInjeftion;

static public class ServicesExtensions
{
    static public IServiceCollection AddEventBus(this IServiceCollection services)
    {
        return services
            .AddScoped<ExplorerEventBusService>();
    }
}
