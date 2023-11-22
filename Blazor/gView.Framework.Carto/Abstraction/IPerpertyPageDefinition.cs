using System;

namespace gView.Framework.Carto.Abstraction;

public interface IPerpertyPageDefinition
{
    Type InterfaceType { get; }
    Type InstanceType { get; }
    Type PropertyPageType { get; }
}
