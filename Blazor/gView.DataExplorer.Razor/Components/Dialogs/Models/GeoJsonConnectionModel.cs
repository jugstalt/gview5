using gView.Blazor.Models.Dialogs;
using gView.Web.Framework.Web.Authorization;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public enum GeoJsonAuthType
{
    Basic = 0,
    Bearer = 1
}

public class GeoJsonConnectionModel : IDialogResultItem
{
    public GeoJsonConnectionModel()
    {
        Credentials = new WebAuthorizationCredentials("", "");
    }

    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;

    public WebAuthorizationCredentials Credentials { get; set; }
}
