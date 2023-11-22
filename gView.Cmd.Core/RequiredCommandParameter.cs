namespace gView.Cmd.Core;
public class RequiredCommandParameter<T> : CommandParameter<T>
{
    public RequiredCommandParameter(string name)
        : base(name)
    {
        IsRequired = true;
    }
}
