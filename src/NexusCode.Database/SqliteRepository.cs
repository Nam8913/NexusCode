using Microsoft.Data.Sqlite;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Database;

public sealed class SqliteRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteRepository(string dbPath = "nexus.db")
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS symbols (
                id TEXT PRIMARY KEY,
                repository_id TEXT NOT NULL,
                name TEXT NOT NULL,
                full_name TEXT NOT NULL,
                kind INTEGER NOT NULL,
                type_name TEXT,
                file_path TEXT,
                start_line INTEGER,
                end_line INTEGER,
                metadata TEXT DEFAULT '{}'
            );
            CREATE INDEX IF NOT EXISTS idx_symbols_name ON symbols(name);
            CREATE INDEX IF NOT EXISTS idx_symbols_full_name ON symbols(full_name);
            CREATE INDEX IF NOT EXISTS idx_symbols_kind ON symbols(kind);

            CREATE TABLE IF NOT EXISTS graph_nodes (
                id BLOB PRIMARY KEY,
                full_name TEXT NOT NULL,
                label TEXT NOT NULL,
                kind INTEGER NOT NULL,
                metadata TEXT DEFAULT '{}'
            );
            CREATE INDEX IF NOT EXISTS idx_nodes_kind ON graph_nodes(kind);

            CREATE TABLE IF NOT EXISTS graph_edges (
                id BLOB PRIMARY KEY,
                source_id BLOB NOT NULL,
                target_id BLOB NOT NULL,
                kind INTEGER NOT NULL,
                weight REAL DEFAULT 1.0
            );
            CREATE INDEX IF NOT EXISTS idx_edges_source ON graph_edges(source_id);
            CREATE INDEX IF NOT EXISTS idx_edges_target ON graph_edges(target_id);

            CREATE TABLE IF NOT EXISTS chunks (
                id TEXT PRIMARY KEY,
                repository_id TEXT NOT NULL,
                symbol_id TEXT,
                content TEXT NOT NULL,
                chunk_type INTEGER NOT NULL,
                content_hash TEXT,
                token_count INTEGER DEFAULT 0,
                embedding BLOB
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void SaveSymbols(IEnumerable<SymbolEntity> symbols)
    {
        using var transaction = _connection.BeginTransaction();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO symbols (id, repository_id, name, full_name, kind, type_name, file_path, start_line, end_line, metadata)
            VALUES ($id, $repo, $name, $fullName, $kind, $typeName, $filePath, $startLine, $endLine, $metadata)";

        foreach (var symbol in symbols)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$id", symbol.Id.ToString());
            cmd.Parameters.AddWithValue("$repo", symbol.RepositoryId.ToString());
            cmd.Parameters.AddWithValue("$name", symbol.Name);
            cmd.Parameters.AddWithValue("$fullName", symbol.FullName);
            cmd.Parameters.AddWithValue("$kind", (int)symbol.Kind);
            cmd.Parameters.AddWithValue("$typeName", symbol.TypeName ?? "");
            cmd.Parameters.AddWithValue("$filePath", symbol.FilePath ?? "");
            cmd.Parameters.AddWithValue("$startLine", symbol.StartLine);
            cmd.Parameters.AddWithValue("$endLine", symbol.EndLine);
            cmd.Parameters.AddWithValue("$metadata", System.Text.Json.JsonSerializer.Serialize(symbol.Metadata));
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void SaveGraph(KnowledgeGraph graph)
    {
        using var transaction = _connection.BeginTransaction();

        var nodeCmd = _connection.CreateCommand();
        nodeCmd.CommandText = "INSERT OR REPLACE INTO graph_nodes (id, full_name, label, kind, metadata) VALUES ($id, $fullName, $label, $kind, $metadata)";

        foreach (var node in GetAllNodes(graph))
        {
            nodeCmd.Parameters.Clear();
            nodeCmd.Parameters.AddWithValue("$id", node.Id);
            nodeCmd.Parameters.AddWithValue("$fullName", node.FullName);
            nodeCmd.Parameters.AddWithValue("$label", node.Label);
            nodeCmd.Parameters.AddWithValue("$kind", (int)node.Kind);
            nodeCmd.Parameters.AddWithValue("$metadata", System.Text.Json.JsonSerializer.Serialize(node.Metadata));
            nodeCmd.ExecuteNonQuery();
        }

        var edgeCmd = _connection.CreateCommand();
        edgeCmd.CommandText = "INSERT OR REPLACE INTO graph_edges (id, source_id, target_id, kind, weight) VALUES ($id, $sourceId, $targetId, $kind, $weight)";

        foreach (var edge in GetAllEdges(graph))
        {
            edgeCmd.Parameters.Clear();
            edgeCmd.Parameters.AddWithValue("$id", edge.Id);
            edgeCmd.Parameters.AddWithValue("$sourceId", edge.SourceId);
            edgeCmd.Parameters.AddWithValue("$targetId", edge.TargetId);
            edgeCmd.Parameters.AddWithValue("$kind", (int)edge.Kind);
            edgeCmd.Parameters.AddWithValue("$weight", edge.Weight);
            edgeCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<SymbolEntity> LoadSymbols()
    {
        var symbols = new List<SymbolEntity>();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM symbols";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            symbols.Add(new SymbolEntity
            {
                Id = Guid.Parse(reader.GetString(0)),
                RepositoryId = Guid.Parse(reader.GetString(1)),
                Name = reader.GetString(2),
                FullName = reader.GetString(3),
                Kind = (SymbolKind)reader.GetInt32(4),
                TypeName = reader.GetString(5),
                FilePath = reader.GetString(6),
                StartLine = reader.GetInt32(7),
                EndLine = reader.GetInt32(8),
                Metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(9)) ?? new()
            });
        }

        return symbols;
    }

    private List<GraphNodeEntity> GetAllNodes(KnowledgeGraph graph)
    {
        var nodes = new List<GraphNodeEntity>();
        foreach (var kind in Enum.GetValues<NodeKind>())
        {
            nodes.AddRange(graph.GetNodesByKind(kind));
        }
        return nodes;
    }

    private List<GraphEdgeEntity> GetAllEdges(KnowledgeGraph graph)
    {
        var edges = new List<GraphEdgeEntity>();
        foreach (var kind in Enum.GetValues<EdgeKind>())
        {
            edges.AddRange(graph.GetEdgesByKind(kind));
        }
        return edges;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
