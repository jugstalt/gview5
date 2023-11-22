namespace gView.Blazor.Core.Services;

public class IconService
{
    public string FromString(string iconName)
     => iconName.Contains(":") ?
            $"sketchpen-icon-{iconName.Split(':')[0]}-bg-light-24 {iconName.Split(':')[1]}" :
            $"sketchpen-icon-default-bg-light-24 {iconName}";
}
