using NexusCode.Tests.Fixtures;
using NexusCode.Indexer;
using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests.Integration;

public class IndexerIntegrationTests : IDisposable
{
    private readonly string _repoPath;

    public IndexerIntegrationTests()
    {
        _repoPath = SampleRepository.CreateTempRepo();
    }

    public void Dispose()
    {
        SampleRepository.Cleanup(_repoPath);
    }

    [Fact]
    public async Task IndexRepository_RunsWithoutError()
    {
        var indexer = new CodeIndexer();
        var result = await indexer.IndexAsync(_repoPath, new IndexOptions());

        Assert.True(result.Success);
        Assert.True(result.Duration.TotalSeconds < 30);
    }

    [Fact]
    public async Task IndexRepository_ExtractsSymbols()
    {
        var indexer = new CodeIndexer();
        await indexer.IndexAsync(_repoPath, new IndexOptions());

        Assert.True(indexer.SymbolTable.Count > 0);
    }

    [Fact]
    public async Task IndexRepository_BuildsGraph()
    {
        var indexer = new CodeIndexer();
        await indexer.IndexAsync(_repoPath, new IndexOptions());

        Assert.True(indexer.Graph.NodeCount > 0);
    }

    [Fact]
    public async Task MultiRepoManager_IndexesRepository()
    {
        var manager = new MultiRepoManager();
        var result = await manager.IndexRepositoryAsync(_repoPath);

        Assert.True(result.Success);
        Assert.True(result.SymbolsExtracted > 0);
    }

    [Fact]
    public async Task IncrementalIndexer_CheckpointWorks()
    {
        var indexer = new IncrementalIndexer();
        var table = new SymbolTable();
        var graph = new KnowledgeGraph();

        var result = await indexer.IndexIncrementalAsync(_repoPath, table, graph);
        Assert.True(result.Success);

        var checkpoint = await indexer.LoadCheckpointAsync(_repoPath);
        Assert.NotNull(checkpoint);
    }
}
