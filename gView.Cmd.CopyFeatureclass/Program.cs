using System.Threading.Tasks;
using gView.Cmd.Core;
using gView.Cmd.Core.Extensions;
using gView.CopyFeatureclass.Lib;
using gView.Framework.system;

namespace gView.Cmd.CopyFeatureclass;

class Program
{
    async static Task<int> Main(string[] args)
    {
        PlugInManager.InitSilent = true;

        var parameters = args.ParseCommandLineArguments();
        var command = new CopyFeatureClassCommand();
        var logger = new gView.Cmd.Core.ConsoleLogger();

        if (!parameters.VerifyParameters(command.ParameterDescriptions, logger))
        {
            command.LogUsage(logger);
            return 1;
        }

        if(await command.Run(parameters, null, logger) == false)
        {
            return 1;
        }

        return 0;
    }
}
