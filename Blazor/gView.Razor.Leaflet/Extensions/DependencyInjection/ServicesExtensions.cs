using gView.Razor.Leaflet.Services;
using Microsoft.Extensions.DependencyInjection;

namespace gView.Razor.Leaflet.Extensions.DependencyInjection;

static public class ServicesExtensions
{
    static public IServiceCollection AddLeafletService(this IServiceCollection services)
        => services
            .AddScoped<LeafletService>()
            .AddScoped<LeafletInteropService>();
}
