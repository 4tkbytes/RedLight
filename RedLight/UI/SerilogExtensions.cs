using Serilog;
using Serilog.Configuration;

namespace RedLight.UI;

public static class SerilogExtensions
{
    public static LoggerConfiguration ImGuiConsole(
        this LoggerSinkConfiguration loggerConfiguration,
        ConsoleLog consoleLog,
        IFormatProvider formatProvider = null)
    {
        return loggerConfiguration.Sink(new ImGuiConsoleSink(consoleLog, formatProvider));
    }
}