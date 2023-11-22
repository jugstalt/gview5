using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Abstraction;
public interface ICommandPararmeterBuilder
{
    IEnumerable<ICommandParameterDescription> ParameterDescriptions { get; }

    Task<T> Build<T>(IDictionary<string, object> parameters); 
}
