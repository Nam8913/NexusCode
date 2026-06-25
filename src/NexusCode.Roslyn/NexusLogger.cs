using System.Collections.Concurrent;

namespace NexusCode.Roslyn;

public sealed class NexusLogger
{
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly string? _logFilePath;

    public int TotalEntries => _logQueue.Count;

    public NexusLogger(string? logFilePath = null)
    {
        _logFilePath = logFilePath;
    }

    public void LogIndexingStart(string repoPath)
    {
        Log(LogLevel.Info, $"Indexing started: {repoPath}");
    }

    public void LogIndexingProgress(string repoPath, int processed, int total)
    {
        var percent = total > 0 ? (double)processed / total * 100 : 0;
        Log(LogLevel.Info, $"[{repoPath}] Progress: {processed}/{total} ({percent:F1}%)");
    }

    public void LogIndexingComplete(string repoPath, int files, int symbols, TimeSpan duration)
    {
        Log(LogLevel.Info, $"[{repoPath}] Complete: {files} files, {symbols} symbols in {duration.TotalSeconds:F2}s");
    }

    public void LogSearchQuery(string query, int results, TimeSpan latency)
    {
        Log(LogLevel.Debug, $"Search: \"{query}\" → {results} results in {latency.TotalMilliseconds:F0}ms");
    }

    public void LogGraphOperation(string operation, int nodes, TimeSpan latency)
    {
        Log(LogLevel.Debug, $"Graph {operation}: {nodes} nodes in {latency.TotalMilliseconds:F0}ms");
    }

    public void LogError(string operation, Exception ex)
    {
        Log(LogLevel.Error, $"Error in {operation}: {ex.Message}");
    }

    public void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public void LogMemoryUsage(long bytes)
    {
        var mb = bytes / (1024.0 * 1024.0);
        Log(LogLevel.Info, $"Memory usage: {mb:F1} MB");
    }

    public List<LogEntry> GetRecentLogs(int count = 100)
    {
        return _logQueue.TakeLast(count).ToList();
    }

    public List<LogEntry> GetLogsByLevel(LogLevel level)
    {
        return _logQueue.Where(e => e.Level == level).ToList();
    }

    private void Log(LogLevel level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message
        };

        _logQueue.Enqueue(entry);

        while (_logQueue.Count > 10000)
            _logQueue.TryDequeue(out _);

        if (_logFilePath != null)
        {
            try
            {
                var logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch { }
        }
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}
