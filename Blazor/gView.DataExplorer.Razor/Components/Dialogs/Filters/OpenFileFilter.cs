using gView.Framework.DataExplorer.Abstraction;
using System.Text.RegularExpressions;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;
public class OpenFileFilter : ExplorerDialogFilter
{
    private readonly string _filter;
    private readonly Regex _regex;

    public OpenFileFilter(string name = "", string filter = "*.*")
        : base($"{name} file ({filter})".Trim())
    {
        _filter = filter;
        _regex = new Regex("^" + Regex.Escape(filter)
                                   .Replace("\\*", ".*")
                                   .Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
    }

    public override Task<bool> Match(IExplorerObject exObject)
    {
        return Task.FromResult(DoesFilenameMatchFilter(exObject.Name, _filter));
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

    private bool DoesFilenameMatchFilter(string filename, string filter)
    {
        return _regex.IsMatch(filename);
    }
}
