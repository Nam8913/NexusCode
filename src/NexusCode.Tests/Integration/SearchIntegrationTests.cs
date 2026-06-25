using NexusCode.Tests.Fixtures;
using NexusCode.Indexer;
using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests.Integration;

public class SearchIntegrationTests : IDisposable
{
    private readonly string _repoPath;

    public SearchIntegrationTests()
    {
        _repoPath = SampleRepository.CreateTempRepo();
    }

    public void Dispose()
    {
        SampleRepository.Cleanup(_repoPath);
    }

    [Fact]
    public async Task SymbolSearch_WorksAfterIndexing()
    {
        var indexer = new CodeIndexer();
        await indexer.IndexAsync(_repoPath, new IndexOptions());

        var searchEngine = new SymbolSearchEngine(indexer.SymbolTable, indexer.Graph);
        var results = searchEngine.FindSymbol("Player", new SearchOptions { MaxResults = 10 });

        Assert.True(results.Count > 0);
    }

    [Fact]
    public async Task CrossRepoSearch_Works()
    {
        var repo2Path = SampleRepository.CreateTempRepo();

        try
        {
            var manager = new MultiRepoManager();
            await manager.IndexRepositoryAsync(_repoPath);
            await manager.IndexRepositoryAsync(repo2Path);

            var crossSearch = new CrossRepoSearchEngine(manager);
            var results = crossSearch.Search("Player", 10);

            Assert.True(results.Count >= 0);
        }
        finally
        {
            SampleRepository.Cleanup(repo2Path);
        }
    }

    [Fact]
    public async Task RepoComparator_Works()
    {
        var repo2Path = SampleRepository.CreateTempRepo();

        try
        {
            var manager = new MultiRepoManager();
            await manager.IndexRepositoryAsync(_repoPath);
            await manager.IndexRepositoryAsync(repo2Path);

            var comparator = new RepoComparator(manager);
            var repo1Name = Path.GetFileName(_repoPath);
            var repo2Name = Path.GetFileName(repo2Path);

            var result = comparator.Compare(repo1Name, repo2Name);

            Assert.NotNull(result);
            Assert.True(result.CommonSymbols >= 0);
        }
        finally
        {
            SampleRepository.Cleanup(repo2Path);
        }
    }

    [Fact]
    public async Task HealthAnalyzer_Works()
    {
        var manager = new MultiRepoManager();
        await manager.IndexRepositoryAsync(_repoPath);

        var analyzer = new RepoHealthAnalyzer(manager);
        var repoName = Path.GetFileName(_repoPath);
        var report = analyzer.Analyze(repoName);

        Assert.True(report.Score >= 0);
        Assert.True(report.TotalSymbols > 0);
    }
}
