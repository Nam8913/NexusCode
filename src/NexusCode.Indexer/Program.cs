using NexusCode.Domain;
using NexusCode.Indexer;

if (args.Length == 0)
{
    Console.WriteLine("NexusCode Indexer");
    Console.WriteLine("Usage: NexusCode.Indexer <repository-path>");
    return;
}

var repositoryPath = args[0];

if (!Directory.Exists(repositoryPath))
{
    Console.Error.WriteLine($"Directory not found: {repositoryPath}");
    return;
}

Console.WriteLine($"Indexing repository: {repositoryPath}");
Console.WriteLine();

using var indexer = new CodeIndexer();

var progress = new Progress<IndexProgress>(p =>
{
    Console.Write($"\r{p.Status} - {p.ProcessedFiles}/{p.TotalFiles} ({p.PercentComplete:F1}%)");
});

var options = new IndexOptions
{
    MaxParallelism = Environment.ProcessorCount
};

var result = await indexer.IndexAsync(repositoryPath, options, progress);

Console.WriteLine();
Console.WriteLine();

if (result.Success)
{
    Console.WriteLine("Indexing complete!");
    Console.WriteLine($"  Files indexed: {result.FilesIndexed}");
    Console.WriteLine($"  Symbols extracted: {result.SymbolsExtracted}");
    Console.WriteLine($"  Graph nodes: {result.GraphNodesCreated}");
    Console.WriteLine($"  Graph edges: {result.GraphEdgesCreated}");
    Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F2}s");

    Console.WriteLine();
    Console.WriteLine("Symbol table stats:");
    Console.WriteLine($"  Total symbols: {indexer.SymbolTable.Count}");

    var classCount = indexer.SymbolTable.GetByKind(SymbolKind.Type)
        .Count(s => s.TypeName == "Class");
    var methodCount = indexer.SymbolTable.GetByKind(SymbolKind.Method).Count;
    var propertyCount = indexer.SymbolTable.GetByKind(SymbolKind.Property).Count;
    var fieldCount = indexer.SymbolTable.GetByKind(SymbolKind.Field).Count;

    Console.WriteLine($"  Classes: {classCount}");
    Console.WriteLine($"  Methods: {methodCount}");
    Console.WriteLine($"  Properties: {propertyCount}");
    Console.WriteLine($"  Fields: {fieldCount}");

    Console.WriteLine();
    Console.WriteLine("Graph stats:");
    Console.WriteLine($"  Nodes: {indexer.Graph.NodeCount}");
    Console.WriteLine($"  Edges: {indexer.Graph.EdgeCount}");
}
else
{
    Console.Error.WriteLine($"Indexing failed: {result.Error}");
}
