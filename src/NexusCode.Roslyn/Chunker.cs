using System.Security.Cryptography;
using System.Text;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class Chunker
{
    public List<ChunkEntity> GenerateChunks(SymbolEntity symbol, string sourceCode, Guid repositoryId, Guid projectId)
    {
        var chunks = new List<ChunkEntity>();
        var lines = sourceCode.Split('\n');
        var startLine = Math.Max(0, symbol.StartLine);
        var endLine = Math.Min(lines.Length - 1, symbol.EndLine);

        if (startLine >= lines.Length || endLine < startLine)
            return chunks;

        var content = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1));

        chunks.Add(new ChunkEntity
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ProjectId = projectId,
            SymbolId = symbol.Id,
            Content = content,
            ChunkType = ChunkType.Method,
            StartLine = startLine + 1,
            EndLine = endLine + 1,
            ContentHash = ComputeHash(content),
            TokenCount = EstimateTokens(content),
            Metadata = new Dictionary<string, string>
            {
                ["symbolName"] = symbol.Name,
                ["fullName"] = symbol.FullName,
                ["kind"] = symbol.Kind.ToString()
            }
        });

        return chunks;
    }

    public List<ChunkEntity> GenerateChunksFromFile(string filePath, string sourceCode, Guid repositoryId, Guid projectId)
    {
        var chunks = new List<ChunkEntity>();
        var lines = sourceCode.Split('\n');

        var chunkSize = 100;
        for (int i = 0; i < lines.Length; i += chunkSize)
        {
            var end = Math.Min(i + chunkSize, lines.Length);
            var content = string.Join("\n", lines.Skip(i).Take(end - i));

            chunks.Add(new ChunkEntity
            {
                Id = Guid.NewGuid(),
                RepositoryId = repositoryId,
                ProjectId = projectId,
                SourceFileId = Guid.NewGuid(),
                Content = content,
                ChunkType = ChunkType.File,
                StartLine = i + 1,
                EndLine = end,
                ContentHash = ComputeHash(content),
                TokenCount = EstimateTokens(content),
                Metadata = new Dictionary<string, string>
                {
                    ["filePath"] = filePath
                }
            });
        }

        return chunks;
    }

    public ChunkEntity CreateGraphContextChunk(SymbolEntity symbol, string sourceCode, List<SymbolEntity> relatedSymbols, Guid repositoryId, Guid projectId)
    {
        var lines = sourceCode.Split('\n');
        var startLine = Math.Max(0, symbol.StartLine);
        var endLine = Math.Min(lines.Length - 1, symbol.EndLine);
        var content = startLine < lines.Length && endLine >= startLine
            ? string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1))
            : "";

        var contextHeader = $"// Symbol: {symbol.FullName}\n";
        if (relatedSymbols.Count > 0)
        {
            contextHeader += $"// Related: {string.Join(", ", relatedSymbols.Take(5).Select(s => s.Name))}\n";
        }
        contextHeader += "\n";

        var fullContent = contextHeader + content;

        return new ChunkEntity
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            ProjectId = projectId,
            SymbolId = symbol.Id,
            Content = fullContent,
            ChunkType = ChunkType.GraphContext,
            StartLine = startLine + 1,
            EndLine = endLine + 1,
            ContentHash = ComputeHash(fullContent),
            TokenCount = EstimateTokens(fullContent),
            Metadata = new Dictionary<string, string>
            {
                ["symbolName"] = symbol.Name,
                ["fullName"] = symbol.FullName,
                ["relatedSymbols"] = string.Join(",", relatedSymbols.Select(s => s.Name))
            }
        };
    }

    private static string ComputeHash(string text)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hash);
    }

    private static int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / 3.0);
    }
}
