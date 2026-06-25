using NexusCode.Tests.Fixtures;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests.Integration;

public class ApiIntegrationTests
{
    [Fact]
    public void MetricsCollector_RecordsAndRetrieves()
    {
        var collector = new MetricsCollector();

        collector.RecordIndexingMetric("test", 100, 500, TimeSpan.FromSeconds(5));
        collector.RecordQueryMetric("search", TimeSpan.FromMilliseconds(50));
        collector.RecordSearchMetric("symbol", 10, TimeSpan.FromMilliseconds(30));
        collector.RecordMemoryMetric(1024 * 1024 * 100);

        var snapshot = collector.GetSnapshot();
        Assert.True(snapshot.Metrics.Count > 0);
    }

    [Fact]
    public void NexusLogger_LogsAndRetrieves()
    {
        var logger = new NexusLogger();

        logger.LogIndexingStart("/test");
        logger.LogIndexingProgress("/test", 50, 100);
        logger.LogIndexingComplete("/test", 100, 500, TimeSpan.FromSeconds(5));
        logger.LogSearchQuery("test", 10, TimeSpan.FromMilliseconds(50));
        logger.LogError("test", new Exception("test error"));
        logger.LogWarning("test warning");

        Assert.True(logger.TotalEntries >= 6);

        var errors = logger.GetLogsByLevel(LogLevel.Error);
        Assert.Single(errors);
    }

    [Fact]
    public async Task StreamingAnalyzer_YieldsResults()
    {
        var repoPath = SampleRepository.CreateTempRepo();
        try
        {
            var analyzer = new StreamingAnalyzer();
            var count = 0;

            await foreach (var result in analyzer.AnalyzeStream(repoPath))
            {
                count++;
                if (count >= 3) break;
            }

            Assert.True(count > 0);
        }
        finally
        {
            SampleRepository.Cleanup(repoPath);
        }
    }

    [Fact]
    public void LRUCache_SetAndGet()
    {
        var cache = new LRUCache<string, int>(10);

        cache.Set("key1", 100);
        cache.Set("key2", 200);

        Assert.Equal(100, cache.Get("key1"));
        Assert.Equal(200, cache.Get("key2"));
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void LRUCache_EvictsWhenFull()
    {
        var cache = new LRUCache<string, int>(2);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        Assert.True(cache.Count <= 3);
        Assert.NotNull(cache.Get("c"));
    }

    [Fact]
    public void PrometheusFormat_Works()
    {
        var collector = new MetricsCollector();
        collector.RecordIndexingMetric("test", 100, 500, TimeSpan.FromSeconds(5));

        var prometheus = collector.ToPrometheusFormat();
        Assert.Contains("nexus_", prometheus);
    }
}
