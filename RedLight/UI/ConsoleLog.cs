namespace RedLight.UI;

/// <summary>
/// Manages console log entries and command history
/// </summary>
public class ConsoleLog
{
    private List<string> logs = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyPosition = -1;

    /// <summary>
    /// Adds a message to the console log
    /// </summary>
    /// <param name="message">The message to add</param>
    public void AddLog(string message)
    {
        logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    /// <summary>
    /// Gets all log entries
    /// </summary>
    public IReadOnlyList<string> Logs => logs;

    /// <summary>
    /// Adds a command to history
    /// </summary>
    /// <param name="command">The command to add</param>
    public void AddCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        commandHistory.Add(command);
        historyPosition = -1;
    }

    /// <summary>
    /// Gets previous command from history
    /// </summary>
    /// <param name="currentInput">Current input to save if first navigation</param>
    /// <returns>Previous command or empty string if none</returns>
    public string GetPreviousCommand(string currentInput)
    {
        if (commandHistory.Count == 0)
            return string.Empty;

        if (historyPosition == -1)
            historyPosition = commandHistory.Count - 1;
        else if (historyPosition > 0)
            historyPosition--;

        return historyPosition >= 0 ? commandHistory[historyPosition] : string.Empty;
    }

    /// <summary>
    /// Gets next command from history
    /// </summary>
    /// <returns>Next command or empty string if none</returns>
    public string GetNextCommand()
    {
        if (historyPosition < commandHistory.Count - 1)
        {
            historyPosition++;
            return commandHistory[historyPosition];
        }

        historyPosition = -1;
        return string.Empty;
    }

    /// <summary>
    /// Clears all console logs
    /// </summary>
    public void Clear()
    {
        logs.Clear();
    }
}