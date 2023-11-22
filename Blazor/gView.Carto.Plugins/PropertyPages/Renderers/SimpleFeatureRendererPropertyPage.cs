using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.Carto.Rendering;
using gView.Framework.system;

namespace gView.Carto.Plugins.PropertyPages.Renderers;

[RegisterPlugIn("CFB9B4B6-E7A7-44F6-AF57-C0F4ADED93D1")]
internal class SimpleFeatureRendererPropertyPage : IPerpertyPageDefinition
{
    public Type InterfaceType => typeof(IFeatureRenderer);

    public Type InstanceType => typeof(SimpleRenderer);

    public Type PropertyPageType => typeof(gView.Carto.Razor.Components.Controls.Renderers.SimpleFeatureRendererPropertyPage);
}
