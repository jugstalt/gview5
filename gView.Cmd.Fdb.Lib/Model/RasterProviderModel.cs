using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Cmd.Fdb.Lib.Model;
public class RasterProviderModel
{
    public Guid PluginGuid { get; set; } = Guid.Empty;
    public string Format { get; set; } = string.Empty;
}
