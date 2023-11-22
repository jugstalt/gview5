using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Framework.Data;
using gView.Framework.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;

public class EnvelopeParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public EnvelopeParameterBuilder(string parameterPrefix = "")
    {
        _parameterPrefix = parameterPrefix;
    }

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions
         => new ICommandParameterDescription[]
         {
            new RequiredCommandParameter<double>("minx".PrependPrefix(_parameterPrefix))
            {
                Description = "Minimum X Coordinate"
            },
            new RequiredCommandParameter<double>("miny".PrependPrefix(_parameterPrefix))
            { 
                Description = "Minimum Y Coordinate"
            },
            new RequiredCommandParameter<double>("maxx".PrependPrefix(_parameterPrefix))
            {
                Description = "Maximum X Coordinate"
            },
            new RequiredCommandParameter<double>("maxy".PrependPrefix(_parameterPrefix))
            {
                Description = "Maximum Y Coordinate"
            },
         };

    public Task<T> Build<T>(IDictionary<string, object> parameters)
    { 
        if (typeof(T) == typeof(IEnvelope))
        {
            var envelope = new Envelope(
                parameters.GetRequiredValue<double>("minx".PrependPrefix(_parameterPrefix)),
                parameters.GetRequiredValue<double>("miny".PrependPrefix(_parameterPrefix)),
                parameters.GetRequiredValue<double>("maxx".PrependPrefix(_parameterPrefix)),
                parameters.GetRequiredValue<double>("maxy".PrependPrefix(_parameterPrefix)));

            return Task.FromResult<T>((T)(object)envelope);
        }

        throw new ArgumentException($"Can't build type {typeof(T).Name}");
    }
}
