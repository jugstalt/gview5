namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenFDBFeatureclassFilter : ExplorerOpenDialogFilter
{
    public OpenFDBFeatureclassFilter()
        : base("FDB Feature Class(es)")
    {
        this.ExplorerObjectGUIDs.Add(new Guid("FE6E1EA7-1300-400c-8674-68465859E991"));
        this.ExplorerObjectGUIDs.Add(new Guid("A610B342-E911-4c52-8E35-72A69B52440A"));
        this.ExplorerObjectGUIDs.Add(new Guid("16DB07EC-5C30-4C2E-85AC-B49A44188B1A"));
    }
}
