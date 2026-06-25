using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests;

public class SymbolTableTests
{
    [Fact]
    public void Add_And_GetById_ReturnsSymbol()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("Test.Class", SymbolKind.Type);

        table.Add(symbol);

        var result = table.GetById(symbol.Id);
        Assert.NotNull(result);
        Assert.Equal("Test.Class", result!.FullName);
    }

    [Fact]
    public void GetByFullName_ReturnsCorrectSymbol()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("MyApp.PlayerController", SymbolKind.Type);

        table.Add(symbol);

        var result = table.GetByFullName("MyApp.PlayerController");
        Assert.NotNull(result);
        Assert.Equal("PlayerController", result!.Name);
    }

    [Fact]
    public void GetByName_ReturnsMultipleSymbols()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("A.Update", SymbolKind.Method));
        table.Add(CreateSymbol("B.Update", SymbolKind.Method));
        table.Add(CreateSymbol("C.Start", SymbolKind.Method));

        var results = table.GetByName("Update");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void GetByKind_FiltersCorrectly()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("Test.Class", SymbolKind.Type));
        table.Add(CreateSymbol("Test.Method", SymbolKind.Method));
        table.Add(CreateSymbol("Test.Property", SymbolKind.Property));

        var classes = table.GetByKind(SymbolKind.Type);
        var methods = table.GetByKind(SymbolKind.Method);

        Assert.Single(classes);
        Assert.Single(methods);
    }

    [Fact]
    public void Remove_DeletesSymbol()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("Test.Class", SymbolKind.Type);
        table.Add(symbol);

        table.Remove(symbol.Id);

        Assert.Null(table.GetById(symbol.Id));
        Assert.Equal(0, table.Count);
    }

    [Fact]
    public void Update_ModifiesSymbol()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("Test.Class", SymbolKind.Type);
        table.Add(symbol);

        symbol.EndLine = 100;
        table.Update(symbol);

        var result = table.GetById(symbol.Id);
        Assert.Equal(100, result!.EndLine);
    }

    [Fact]
    public void AddReference_And_GetReferences_Works()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("Test.Method", SymbolKind.Method);
        table.Add(symbol);

        var reference = new ReferenceEntity
        {
            SymbolId = symbol.Id,
            Line = 10,
            Column = 5,
            Kind = ReferenceKind.Call
        };
        table.AddReference(reference);

        var refs = table.GetReferences(symbol.Id);
        Assert.Single(refs);
        Assert.Equal(10, refs[0].Line);
    }

    [Fact]
    public void Count_ReturnsCorrectNumber()
    {
        var table = new SymbolTable();
        Assert.Equal(0, table.Count);

        table.Add(CreateSymbol("A.Class", SymbolKind.Type));
        table.Add(CreateSymbol("B.Class", SymbolKind.Type));
        Assert.Equal(2, table.Count);
    }

    private static SymbolEntity CreateSymbol(string fullName, SymbolKind kind)
    {
        var parts = fullName.Split('.');
        return new SymbolEntity
        {
            Id = Guid.NewGuid(),
            Name = parts.Last(),
            FullName = fullName,
            Kind = kind,
            FilePath = "test.cs",
            StartLine = 1,
            EndLine = 10
        };
    }
}
