using Serilog.Core;
using Serilog.Events;

namespace RedLight.UI.ImGui;

public class ImGuiConsoleSink : ILogEventSink
{
    private readonly ConsoleLog _consoleLog;
    private readonly IFormatProvider _formatProvider;

    public ImGuiConsoleSink(ConsoleLog consoleLog, IFormatProvider formatProvider = null)
    {
        _consoleLog = consoleLog;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_formatProvider);
        var level = logEvent.Level.ToString().Substring(0, 3).ToUpperInvariant();
        _consoleLog.AddLog($"[{level}] {message}");
    }
}