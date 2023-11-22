using gView.Cmd.Core.Abstraction;
using System;

namespace gView.Cmd.Core;
public class ConsoleLogger : ICommandLogger
{
    private bool _requireNewline = false;
    public void Log(string message)
    {
        Console.Write(message);
        _requireNewline = true;
    }

    public void LogLine(string message)
    {
        if (_requireNewline)
        {
            Console.WriteLine();
        }

        Console.WriteLine(message);
        _requireNewline = false;
    }
}
