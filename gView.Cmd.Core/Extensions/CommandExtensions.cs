using gView.Cmd.Core.Abstraction;
using System.Collections.Generic;

namespace gView.Cmd.Core.Extensions;
static public class CommandExtensions
{
    static public void LogUsage(this ICommand command, ICommandLogger? logger = null)
    {
        logger?.LogLine("Usage:");

        command.ParameterDescriptions.LogParameterDescriptions(logger);
    }

    static private void LogParameterDescriptions(this IEnumerable<ICommandParameterDescription> parametersDescriptions, ICommandLogger? logger = null)
    {
        foreach (var parameterDescription in parametersDescriptions)
        {
            if(parameterDescription is CommentParameter)
            {
                logger?.LogLine(((CommentParameter)parameterDescription).Description);
            }
            else if (parameterDescription.ParameterType.IsValueType || parameterDescription.ParameterType == typeof(string))
            {
                logger?.LogLine($"  -{parameterDescription.Name}: {parameterDescription.Description}");
            }
            else
            {
                try
                {
                    var builder = parameterDescription.GetBuilder();
                    builder.ParameterDescriptions.LogParameterDescriptions(logger);
                } catch { }
            }
        }
    }
}
