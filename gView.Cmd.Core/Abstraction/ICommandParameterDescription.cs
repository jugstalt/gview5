using System;

namespace gView.Cmd.Core.Abstraction;

public interface ICommandParameterDescription
{
    string Name { get; }
    string Description { get; }
    bool IsRequired { get; }
    public Type ParameterType { get; }
}
