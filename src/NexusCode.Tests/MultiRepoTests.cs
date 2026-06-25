using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests;

public class MultiRepoTests
{
    [Fact]
    public void MultiRepoManager_AddRepository()
    {
        var manager = new MultiRepoManager();
        var repo = new RepoIndex
        {
            Name = "TestRepo",
            Path = "/test",
            SymbolCount = 100,
            NodeCount = 50,
            EdgeCount = 75
        };

        manager.AddRepository(repo);

        Assert.Single(manager.GetAllRepositories());
        Assert.Equal("TestRepo", manager.GetAllRepositories()[0].Name);
    }

    [Fact]
    public void MultiRepoManager_RemoveRepository()
    {
        var manager = new MultiRepoManager();
        var repo = new RepoIndex { Name = "ToRemove" };
        manager.AddRepository(repo);

        manager.RemoveRepository("ToRemove");

        Assert.Empty(manager.GetAllRepositories());
    }

    [Fact]
    public void CrossRepoSearch_MergesResults()
    {
        var manager = new MultiRepoManager();
        var table1 = new SymbolTable();
        var table2 = new SymbolTable();

        table1.Add(new SymbolEntity { Id = Guid.NewGuid(), Name = "Player", FullName = "Game.Player", Kind = Domain.SymbolKind.Type });
        table2.Add(new SymbolEntity { Id = Guid.NewGuid(), Name = "Player", FullName = "Engine.Player", Kind = Domain.SymbolKind.Type });

        var graph1 = new KnowledgeGraph();
        var graph2 = new KnowledgeGraph();

        manager.AddRepository(new RepoIndex { Name = "Repo1", SymbolTable = table1, Graph = graph1 });
        manager.AddRepository(new RepoIndex { Name = "Repo2", SymbolTable = table2, Graph = graph2 });

        var search = new CrossRepoSearchEngine(manager);
        var results = search.Search("Player");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Repository == "Repo1");
        Assert.Contains(results, r => r.Repository == "Repo2");
    }

    [Fact]
    public void RepoComparator_FindsCommonSymbols()
    {
        var manager = new MultiRepoManager();
        var table1 = new SymbolTable();
        var table2 = new SymbolTable();

        var sharedId = Guid.NewGuid();
        table1.Add(new SymbolEntity { Id = sharedId, Name = "Shared", FullName = "Common.Shared", Kind = Domain.SymbolKind.Type });
        table2.Add(new SymbolEntity { Id = Guid.NewGuid(), Name = "Shared", FullName = "Common.Shared", Kind = Domain.SymbolKind.Type });

        table1.Add(new SymbolEntity { Id = Guid.NewGuid(), Name = "Only1", FullName = "Common.Only1", Kind = Domain.SymbolKind.Type });
        table2.Add(new SymbolEntity { Id = Guid.NewGuid(), Name = "Only2", FullName = "Common.Only2", Kind = Domain.SymbolKind.Type });

        var graph1 = new KnowledgeGraph();
        var graph2 = new KnowledgeGraph();

        manager.AddRepository(new RepoIndex { Name = "Repo1", SymbolTable = table1, Graph = graph1 });
        manager.AddRepository(new RepoIndex { Name = "Repo2", SymbolTable = table2, Graph = graph2 });

        var comparator = new RepoComparator(manager);
        var result = comparator.Compare("Repo1", "Repo2");

        Assert.Equal(1, result.CommonSymbols);
        Assert.Equal(1, result.UniqueToRepo1);
        Assert.Equal(1, result.UniqueToRepo2);
    }

    [Fact]
    public void RepoHealthAnalyzer_ScoreCalculation()
    {
        var manager = new MultiRepoManager();
        var table = new SymbolTable();
        var graph = new KnowledgeGraph();

        for (int i = 0; i < 10; i++)
        {
            table.Add(new SymbolEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Class{i}",
                FullName = $"NS.Class{i}",
                Kind = Domain.SymbolKind.Type,
                TypeName = "Class"
            });
        }

        manager.AddRepository(new RepoIndex
        {
            Name = "HealthyRepo",
            SymbolTable = table,
            Graph = graph,
            SymbolCount = 10
        });

        var analyzer = new RepoHealthAnalyzer(manager);
        var report = analyzer.Analyze("HealthyRepo");

        Assert.True(report.Score > 80);
        Assert.Equal(10, report.TotalSymbols);
        Assert.Equal(10, report.ClassCount);
    }

    [Fact]
    public void RepoHealthAnalyzer_DetectsLargeClasses()
    {
        var manager = new MultiRepoManager();
        var table = new SymbolTable();
        var graph = new KnowledgeGraph();

        var classId = Guid.NewGuid();
        table.Add(new SymbolEntity { Id = classId, Name = "GodClass", FullName = "NS.GodClass", Kind = Domain.SymbolKind.Type, TypeName = "Class" });

        for (int i = 0; i < 35; i++)
        {
            table.Add(new SymbolEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Method{i}",
                FullName = $"NS.GodClass.Method{i}",
                Kind = Domain.SymbolKind.Method,
                ContainerId = classId
            });
        }

        manager.AddRepository(new RepoIndex
        {
            Name = "BadRepo",
            SymbolTable = table,
            Graph = graph
        });

        var analyzer = new RepoHealthAnalyzer(manager);
        var report = analyzer.Analyze("BadRepo");

        Assert.Contains(report.Warnings, w => w.Contains("Large class"));
    }
}
