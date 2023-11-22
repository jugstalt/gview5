using gView.Cmd.Core.Abstraction;
using System;

namespace gView.Cmd.Core;
public class CommandParameter<T> : ICommandParameterDescription
{
    public CommandParameter(string name)
    {
        this.Name = name;
    }

    public string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public Type ParameterType => typeof(T);
}
