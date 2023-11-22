using gView.Carto.Plugins.Services;
using gView.Framework.Blazor.Services.Abstraction;

namespace gView.Carto.Plugins.Extensions;
static public class CartoApplicationScopeExtensions
{
    static public CartoApplicationScopeService ToCartoScopeService(this IApplicationScope appScope)
        => appScope is CartoApplicationScopeService ?
            (CartoApplicationScopeService)appScope :
            throw new Exception("AppScope is not an Service. Appliation Service not registered correctly");
}
