using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests;

public class SymbolTableResolveTests
{
    [Fact]
    public void ResolveSymbol_ExactFullName_ReturnsSymbol()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::PlayerController", SymbolKind.Type));

        var result = table.ResolveSymbol("global::PlayerController");

        Assert.NotNull(result);
        Assert.Equal("global::PlayerController", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_ShortName_ReturnsExactNameMatch()
    {
        var table = new SymbolTable();
        var typeSym = CreateSymbol("global::PlayerController", SymbolKind.Type);
        var propSym = CreateSymbol("PlayerController", SymbolKind.Property);
        table.Add(typeSym);
        table.Add(propSym);

        var result = table.ResolveSymbol("PlayerController");

        Assert.NotNull(result);
        Assert.Equal("PlayerController", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_SingleSymbol_ReturnsIt()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::OnlyOne", SymbolKind.Method));

        var result = table.ResolveSymbol("OnlyOne");

        Assert.NotNull(result);
        Assert.Equal("global::OnlyOne", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_CaseInsensitive_ReturnsMatch()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::GameService", SymbolKind.Type));

        var result = table.ResolveSymbol("gameservice");

        Assert.NotNull(result);
        Assert.Equal("global::GameService", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_NoMatch_ReturnsNull()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::PlayerController", SymbolKind.Type));

        var result = table.ResolveSymbol("NonExistentThing");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveSymbol_EmptyTable_ReturnsNull()
    {
        var table = new SymbolTable();

        var result = table.ResolveSymbol("Anything");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveSymbol_PrefixFallback_ReturnsSymbol()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::SuperLongClassName", SymbolKind.Type));

        var result = table.ResolveSymbol("SuperLong");

        Assert.NotNull(result);
        Assert.Equal("global::SuperLongClassName", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_ContainsFallback_ReturnsSymbol()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::InventoryViewController", SymbolKind.Type));

        var result = table.ResolveSymbol("Inventory");

        Assert.NotNull(result);
        Assert.Equal("global::InventoryViewController", result!.FullName);
    }

    [Fact]
    public void ResolveSymbol_MultipleByKind_PrefersTypeOverMethod()
    {
        var table = new SymbolTable();
        table.Add(CreateSymbol("global::DoAction", SymbolKind.Method));
        table.Add(CreateSymbol("global::DoAction", SymbolKind.Type));

        var result = table.ResolveSymbol("DoAction");

        Assert.NotNull(result);
        Assert.Equal(SymbolKind.Type, result!.Kind);
    }

    [Fact]
    public void ReferenceEntity_FilePath_CanBeSet()
    {
        var reference = new ReferenceEntity
        {
            SymbolId = Guid.NewGuid(),
            FilePath = @"D:\Projects\MyCode.cs",
            Line = 42,
            Column = 10,
            Kind = ReferenceKind.Call
        };

        Assert.Equal(@"D:\Projects\MyCode.cs", reference.FilePath);
        Assert.Equal(42, reference.Line);
    }

    [Fact]
    public void ReferenceEntity_FilePath_NullByDefault()
    {
        var reference = new ReferenceEntity
        {
            SymbolId = Guid.NewGuid(),
            Line = 1,
            Kind = ReferenceKind.Read
        };

        Assert.Null(reference.FilePath);
    }

    [Fact]
    public void AddReference_WithFilePath_StoresFilePath()
    {
        var table = new SymbolTable();
        var symbol = CreateSymbol("global::Test", SymbolKind.Type);
        table.Add(symbol);

        var reference = new ReferenceEntity
        {
            SymbolId = symbol.Id,
            FilePath = @"D:\Code\ActualFile.cs",
            Line = 10,
            Kind = ReferenceKind.Call
        };
        table.AddReference(reference);

        var refs = table.GetReferences(symbol.Id);
        Assert.Single(refs);
        Assert.Equal(@"D:\Code\ActualFile.cs", refs[0].FilePath);
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
