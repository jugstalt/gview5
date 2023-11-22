using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Cmd.Core.Models;
using gView.Framework.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;

public class FieldsParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public FieldsParameterBuilder(string parameterPrefix = "")
    {
        _parameterPrefix = parameterPrefix;
    }

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions
        => new ICommandParameterDescription[]
         {
            new RequiredCommandParameter<string>("fields".PrependPrefix(_parameterPrefix))
            {
                Description = """
                    Fields Defintion (in on line as string): 
                            [ 
                                {
                                    "name": "FIELD_NAME",
                                    "alias": "FIELD_ALIAS",
                                    "type": "FIELD_TYPE",
                                    "size": size // optional for strings
                                },
                                {..} 
                            ] 
                    """
            }
         };

    public Task<T> Build<T>(IDictionary<string, object> parameters)
    {
        string fieldJsonString = parameters.GetRequiredValue<string>("fields".PrependPrefix(_parameterPrefix));
        var fieldModels = JsonConvert.DeserializeObject<IEnumerable<FieldModel>>(fieldJsonString);

        if(typeof(T).IsAssignableFrom(typeof(IFieldCollection)))
        {
            var fields = new FieldCollection(
                fieldModels.Select(m => new Field(m.Name)
                {
                    aliasname= m.Alias,
                    type = (FieldType)Enum.Parse(typeof(FieldType), m.Type, true),
                    size = m.Size
                }));

            return Task.FromResult<T>((T)(object)fields);
        }

        throw new Exception($"Can't build fields for type {typeof(T).Name}");
    }
}
