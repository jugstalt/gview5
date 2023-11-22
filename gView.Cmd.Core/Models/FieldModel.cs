using gView.Framework.Data;

namespace gView.Cmd.Core.Models;

public class FieldModel
{
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string Type { get; set; } = FieldType.String.ToString();
    public int Size { get; set; }

}
