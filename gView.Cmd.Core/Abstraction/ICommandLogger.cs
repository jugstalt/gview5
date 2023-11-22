namespace gView.Cmd.Core.Abstraction;
public interface ICommandLogger
{
    void LogLine(string message);
    void Log(string message);
}
