using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class SaveFileFilter : ExplorerDialogFilter
{
    public SaveFileFilter(string name = "", string filter = "*.*")
        : base($"{name} file ({filter})".Trim())
    {
    }

    public override Task<bool> Match(IExplorerObject exObject)
    {
        try
        {
            return Task.FromResult(Directory.Exists(exObject.FullName));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public override IEnumerable<IExplorerObject> FilterExplorerObjects(IEnumerable<IExplorerObject> explorerObjects)
    {
        return explorerObjects
                    .Where(e => e.GetType().Name switch
                    {
                        "ComputerObject" => true,
                        "DriveObject" => true,
                        "MappedDriveObject" => true,
                        "DirectoryObject" => true,
                        "FileObject" => true,
                        _ => false
                    });
    }
}
