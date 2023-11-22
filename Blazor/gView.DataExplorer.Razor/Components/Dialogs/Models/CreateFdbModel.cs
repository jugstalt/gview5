namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class CreateFdbModel : ConnectionStringModel
{
    public CreateFdbModel()
        : this("", true)
    {
    }

    public CreateFdbModel(string providerId,
                       bool UseProviderInConnectionString)
        : base(providerId, UseProviderInConnectionString)
    {
    }

    //public string DatabaseName { get; set; }
    public bool CreateRepositoryOnly { get; set; }
}
