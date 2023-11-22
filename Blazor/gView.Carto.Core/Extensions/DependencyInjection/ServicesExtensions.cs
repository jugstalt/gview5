using gView.Carto.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace gView.Carto.Core.Extensions.DependencyInjection;
static public class ServicesExtensions
{
    static public IServiceCollection AddEventBus(this IServiceCollection services)
    {
        return services
            .AddScoped<CartoEventBusService>();
    }
}
