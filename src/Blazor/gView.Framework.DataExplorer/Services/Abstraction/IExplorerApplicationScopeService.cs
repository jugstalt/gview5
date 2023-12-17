﻿using gView.Framework.Blazor.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Services.Abstraction;

public interface IExplorerApplicationScopeService : IApplicationScope
{
    IExplorerObject? CurrentExplorerObject { get; }
    IEnumerable<IExplorerObject>? ContextExplorerObjects { get; }

    Task ForceContentRefresh();

    void SetClipboardItem(ClipboardItem item);
    Type? GetClipboardItemType();
    IEnumerable<T> GetClipboardElements<T>();

    public string GetToolConfigFilename(params string[] paths);
    public IEnumerable<string> GetToolConfigFiles(params string[] paths);

    void AddToSnackbar(string message);
}
