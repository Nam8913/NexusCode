using System.Collections.Concurrent;

namespace NexusCode.Roslyn;

public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, MetricValue> _metrics = new();
    private readonly ConcurrentQueue<MetricEntry> _recentMetrics = new();

    public MetricsSnapshot GetSnapshot()
    {
        var snapshot = new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Metrics = _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        var recent = _recentMetrics.TakeLast(1000).ToList();
        snapshot.AverageIndexingLatency = recent
            .Where(m => m.Operation == "indexing")
            .Average(m => m.LatencyMs);
        snapshot.AverageQueryLatency = recent
            .Where(m => m.Operation == "query")
            .Average(m => m.LatencyMs);

        return snapshot;
    }

    public void RecordIndexingMetric(string repo, int files, int symbols, TimeSpan duration)
    {
        _metrics[$"indexing.{repo}.files"] = new MetricValue { IntValue = files, Unit = "files" };
        _metrics[$"indexing.{repo}.symbols"] = new MetricValue { IntValue = symbols, Unit = "symbols" };
        _metrics[$"indexing.{repo}.duration"] = new MetricValue { DoubleValue = duration.TotalSeconds, Unit = "seconds" };
        _metrics[$"indexing.{repo}.rate"] = new MetricValue { DoubleValue = files / duration.TotalSeconds, Unit = "files/sec" };

        _recentMetrics.Enqueue(new MetricEntry
        {
            Operation = "indexing",
            Timestamp = DateTime.UtcNow,
            LatencyMs = duration.TotalMilliseconds
        });
    }

    public void RecordQueryMetric(string operation, TimeSpan latency)
    {
        _metrics[$"query.{operation}.latency"] = new MetricValue { DoubleValue = latency.TotalMilliseconds, Unit = "ms" };

        _recentMetrics.Enqueue(new MetricEntry
        {
            Operation = "query",
            Timestamp = DateTime.UtcNow,
            LatencyMs = latency.TotalMilliseconds
        });
    }

    public void RecordGraphMetric(string operation, int nodes, int edges)
    {
        _metrics[$"graph.{operation}.nodes"] = new MetricValue { IntValue = nodes, Unit = "nodes" };
        _metrics[$"graph.{operation}.edges"] = new MetricValue { IntValue = edges, Unit = "edges" };
    }

    public void RecordMemoryMetric(long bytes)
    {
        _metrics["memory.usage"] = new MetricValue { DoubleValue = bytes / (1024.0 * 1024.0), Unit = "MB" };
    }

    public void RecordSearchMetric(string type, int results, TimeSpan latency)
    {
        _metrics[$"search.{type}.results"] = new MetricValue { IntValue = results, Unit = "results" };
        _metrics[$"search.{type}.latency"] = new MetricValue { DoubleValue = latency.TotalMilliseconds, Unit = "ms" };
    }

    public string ToPrometheusFormat()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var kvp in _metrics)
        {
            var name = kvp.Key.Replace(".", "_");
            if (kvp.Value.IntValue.HasValue)
                sb.AppendLine($"nexus_{name} {kvp.Value.IntValue.Value}");
            else if (kvp.Value.DoubleValue.HasValue)
                sb.AppendLine($"nexus_{name} {kvp.Value.DoubleValue.Value:F2}");
        }

        return sb.ToString();
    }
}

public class MetricValue
{
    public int? IntValue { get; set; }
    public double? DoubleValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class MetricEntry
{
    public string Operation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double LatencyMs { get; set; }
}

public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, MetricValue> Metrics { get; set; } = new();
    public double? AverageIndexingLatency { get; set; }
    public double? AverageQueryLatency { get; set; }
}
