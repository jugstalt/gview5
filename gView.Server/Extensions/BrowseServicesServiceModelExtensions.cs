using gView.Server.Models;
using System;

namespace gView.Server.Extensions;

internal static class BrowseServicesServiceModelExtensions
{
    public static string ReplaceRequest(this BrowseServicesServiceModel model, string request)
    {
        return request?
            .Replace("{server}", model.Server)
            .Replace("{onlineresource}", model.OnlineResource)
            .Replace("{service}", model.MapService.Name)
            .Replace("{folder}", model.MapService.Folder)
            .Replace("{folder/service}", $"{(String.IsNullOrWhiteSpace(model.MapService.Folder)
                                            ? ""
                                            : $"{model.MapService.Folder}/")}{model.MapService.Name}")
            .Replace("{folder@service}", $"{(String.IsNullOrWhiteSpace(model.MapService.Folder)
                                            ? ""
                                            : $"{model.MapService.Folder}@")}{model.MapService.Name}");
    }
}
