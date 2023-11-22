using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Cmd.Core.Models;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.system;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;

public class AutoFieldsParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public AutoFieldsParameterBuilder(string parameterPrefix = "")
    {
        _parameterPrefix = parameterPrefix;
    }

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions
        => new ICommandParameterDescription[]
         {
            new RequiredCommandParameter<string>("autofields".PrependPrefix(_parameterPrefix))
            {
                Description = """
                    Fields Defintion (in on line as string): 
                            [ 
                                {
                                    "name": "FIELD_NAME",
                                    "pluginguid": "PLUGIN_GUID"
                                },
                                {..} 
                            ] 
                    """
            }
         };

    public Task<T> Build<T>(IDictionary<string, object> parameters)
    {
        string autoFieldJsonString = parameters.GetRequiredValue<string>("autofields".PrependPrefix(_parameterPrefix));
        var autoFieldsModels = JsonConvert.DeserializeObject<IEnumerable<AutoFieldModel>>(autoFieldJsonString);

        if (typeof(T).IsAssignableFrom(typeof(IFieldCollection)))
        {
            if (autoFieldsModels == null)
            {
                return Task.FromResult<T>((T)(object)new FieldCollection());
            }

            var autoFields = new List<IAutoField>();

            foreach (var autoFieldModel in autoFieldsModels)
            {
                var autoField = PlugInManager.Create(autoFieldModel.PluginGuid) as IAutoField;

                if (autoField == null)
                {
                    throw new Exception($"Plugin with guid {autoFieldModel.PluginGuid} is not an IAutoField Plugin");
                }

                if (autoField is Field)
                {
                    ((Field)autoField).name = autoFieldModel.Name;
                }

                autoFields.Add(autoField);
            }

            return Task.FromResult<T>((T)(object)new FieldCollection(autoFields));
        }

        throw new Exception($"Can't build fields for type {typeof(T).Name}");
    }
}
