using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests;

public class SearchSourceTextTests : IDisposable
{
    private readonly string _tempDir;

    public SearchSourceTextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NexusCodeTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void SearchSourceText_FindsMatchingContent()
    {
        var filePath = Path.Combine(_tempDir, "GameService.cs");
        File.WriteAllLines(filePath, new[]
        {
            "public class GameService",
            "{",
            "    public void Initialize()",
            "    {",
            "    }",
            "}"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "GameService",
            FullName = "global::GameService",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 6
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("Initialize");

        Assert.Single(results);
        Assert.Equal(filePath, results[0].FilePath);
        Assert.Equal(3, results[0].Line);
        Assert.Contains("Initialize", results[0].Content);
    }

    [Fact]
    public void SearchSourceText_CaseInsensitive()
    {
        var filePath = Path.Combine(_tempDir, "Lower.cs");
        File.WriteAllLines(filePath, new[]
        {
            "public class LowerClass",
            "{",
            "    private string myField = \"hello\";",
            "}"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "LowerClass",
            FullName = "global::LowerClass",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 4
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("MYFIELD");

        Assert.Single(results);
    }

    [Fact]
    public void SearchSourceText_EmptyQuery_ReturnsEmpty()
    {
        var table = new SymbolTable();
        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchSourceText_NullQuery_ReturnsEmpty()
    {
        var table = new SymbolTable();
        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText(null!);

        Assert.Empty(results);
    }

    [Fact]
    public void SearchSourceText_NoMatch_ReturnsEmpty()
    {
        var filePath = Path.Combine(_tempDir, "NoMatch.cs");
        File.WriteAllLines(filePath, new[]
        {
            "public class NoMatch { }"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "NoMatch",
            FullName = "global::NoMatch",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 1
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("NonExistentSymbol");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchSourceText_MultipleMatches_ReturnsAll()
    {
        var filePath = Path.Combine(_tempDir, "Multi.cs");
        File.WriteAllLines(filePath, new[]
        {
            "class A { }",
            "class B : A { }",
            "class C : A { }",
            "class D { }"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "A",
            FullName = "global::A",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 1
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText(": A");

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchSourceText_MaxResultsLimitsOutput()
    {
        var lines = Enumerable.Range(0, 50).Select(i => $"// Line {i} public void").ToArray();
        var filePath = Path.Combine(_tempDir, "Big.cs");
        File.WriteAllLines(filePath, lines);

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "Big",
            FullName = "global::Big",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 50
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("public void", 5);

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void SearchSourceText_ScoreHigherForStartOfLine()
    {
        var filePath = Path.Combine(_tempDir, "Score.cs");
        File.WriteAllLines(filePath, new[]
        {
            "public class Score",
            "    // public class nested"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "Score",
            FullName = "global::Score",
            Kind = SymbolKind.Type,
            FilePath = filePath,
            StartLine = 1,
            EndLine = 2
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("public class");

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void SearchSourceText_NonexistentFile_SkipsGracefully()
    {
        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "Ghost",
            FullName = "global::Ghost",
            Kind = SymbolKind.Type,
            FilePath = @"Z:\nonexistent\ghost.cs",
            StartLine = 1,
            EndLine = 1
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("anything");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchSourceText_IncludesMethodSymbols()
    {
        var filePath = Path.Combine(_tempDir, "WithMethod.cs");
        File.WriteAllLines(filePath, new[]
        {
            "class WithMethod {",
            "    void DoWork() { }",
            "}"
        });

        var table = new SymbolTable();
        table.Add(new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = "DoWork",
            FullName = "global::WithMethod.DoWork",
            Kind = SymbolKind.Method,
            FilePath = filePath,
            StartLine = 2,
            EndLine = 2
        });

        var engine = new SymbolSearchEngine(table, new KnowledgeGraph());

        var results = engine.SearchSourceText("DoWork");

        Assert.Single(results);
    }
}
