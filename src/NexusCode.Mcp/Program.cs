using System.Text.Json;
using System.Collections.Concurrent;
using NexusCode.Domain;
using NexusCode.Roslyn;
using NexusCode.Indexer;

Console.Error.WriteLine("[NexusCode MCP] Starting...");

// Initialize
var symbolTable = new SymbolTable();
var graph = new KnowledgeGraph();
var searchEngine = new SymbolSearchEngine(symbolTable, graph);

// Session tracking
var sessions = new ConcurrentDictionary<string, McpSession>();

Console.Error.WriteLine("[NexusCode MCP] Ready.");

var reader = Console.In;
var writer = Console.Out;

while (true)
{
    var line = await reader.ReadLineAsync();
    if (line == null) break;
    if (string.IsNullOrWhiteSpace(line)) continue;

    JsonElement? request = null;
    try { request = JsonSerializer.Deserialize<JsonElement>(line); }
    catch { continue; }
    if (request == null) continue;

    var method = request.Value.GetProperty("method").GetString();
    var id = request.Value.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : (int?)null;

    object? response = null;

    if (method == "initialize")
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        sessions[sessionId] = new McpSession { SessionId = sessionId, CreatedAt = DateTime.UtcNow };

        response = MakeResponse(id, new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { tools = new { } },
            serverInfo = new { name = "nexuscode", version = "1.0.0", sessionId }
        });
    }
    else if (method == "notifications/initialized") { continue; }
    else if (method == "tools/list")
    {
        response = MakeResponse(id, new
        {
            tools = new object[]
            {
                new { name = "list_symbols", description = "List symbols filtered by kind and optional name search", inputSchema = new { type = "object", properties = new { kind = new { type = "string", description = "Filter: Class, Method, Property, Field, Event, Interface, Struct, Enum, Namespace (empty = all)" }, name = new { type = "string", description = "Optional name filter (empty = list all)" } } } },
                new { name = "find_symbol", description = "Find symbol by name", inputSchema = new { type = "object", properties = new { query = new { type = "string", description = "Symbol name" } } } },
                new { name = "find_references", description = "Find all references to a symbol", inputSchema = new { type = "object", properties = new { symbolName = new { type = "string", description = "Fully qualified name" } } } },
                new { name = "find_callers", description = "Find methods that call a given method", inputSchema = new { type = "object", properties = new { method = new { type = "string", description = "Fully qualified method name" } } } },
                new { name = "find_callees", description = "Find methods called by a given method", inputSchema = new { type = "object", properties = new { method = new { type = "string", description = "Fully qualified method name" } } } },
                new { name = "find_implementations", description = "Find types implementing an interface", inputSchema = new { type = "object", properties = new { interfaceName = new { type = "string", description = "Interface name" } } } },
                new { name = "find_derived_types", description = "Find types inheriting from a type", inputSchema = new { type = "object", properties = new { typeName = new { type = "string", description = "Type name" } } } },
                new { name = "search_code", description = "Search code by query", inputSchema = new { type = "object", properties = new { query = new { type = "string", description = "Search query" } } } },
                new { name = "get_symbol_info", description = "Get detailed symbol info", inputSchema = new { type = "object", properties = new { symbolName = new { type = "string", description = "Symbol name" } } } },
                // new { name = "get_graph_stats", description = "Get graph statistics", inputSchema = new { type = "object", properties = new { } } },
                new { name = "explain_architecture", description = "Explain codebase architecture", inputSchema = new { type = "object", properties = new { } } },
                new { name = "index_repository", description = "Index a C# repository for analysis", inputSchema = new { type = "object", properties = new { path = new { type = "string", description = "Repository path (e.g. D:\\MyProject)" } } } },
                new { name = "blast_radius", description = "Analyze blast radius - find all code affected if a symbol is changed", inputSchema = new { type = "object", properties = new { symbolName = new { type = "string", description = "Fully qualified symbol name" }, depth = new { type = "integer", description = "Traversal depth (default: 2)" } } } },
                
            }
        });
    }
    else if (method == "tools/call")
    {
        var toolParams = request.Value.GetProperty("params");
        var toolName = toolParams.GetProperty("name").GetString();
        JsonElement toolArguments = toolParams.TryGetProperty("arguments", out var argProp) ? argProp : default;

        var result = toolName switch
        {
            "find_symbol" => HandleFindSymbol(toolArguments),
            "find_references" => HandleFindReferences(toolArguments),
            "find_callers" => HandleFindCallers(toolArguments),
            "find_callees" => HandleFindCallees(toolArguments),
            "find_implementations" => HandleFindImplementations(toolArguments),
            "find_derived_types" => HandleFindDerivedTypes(toolArguments),
            "search_code" => HandleSearchCode(toolArguments),
            "get_symbol_info" => HandleGetSymbolInfo(toolArguments),
            // "get_graph_stats" => HandleGetGraphStats(),
            "explain_architecture" => HandleExplainArchitecture(),
            "index_repository" => await HandleIndexRepository(toolArguments),
            "blast_radius" => HandleBlastRadius(toolArguments),
            "list_symbols" => HandleListSymbols(toolArguments),
            _ => $"Unknown tool: {toolName}"
        };

        response = MakeResponse(id, new
        {
            content = new object[] { new { type = "text", text = result } }
        });
    }
    else if (id != null)
    {
        response = MakeResponse(id, null, new { code = -32601, message = $"Method not found: {method}" });
    }

    if (response != null)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(response));
        await writer.FlushAsync();
    }
}

// Tool handlers
string HandleFindSymbol(JsonElement? args)
{
    var query = args?.TryGetProperty("query", out var q) == true ? q.GetString() ?? "" : "";
    var symbols = symbolTable.GetByName(query);
    if (symbols.Count == 0) return $"No symbols found for '{query}'";
    var list = symbols.Take(20).Select(s => $"- {s.FullName} ({s.Kind}) @ {s.FilePath}:{s.StartLine}");
    return $"Found {symbols.Count} symbols:\n{string.Join("\n", list)}";
}

string HandleFindReferences(JsonElement? args)
{
    var name = args?.TryGetProperty("symbolName", out var n) == true ? n.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Symbol not found: {name}";
    var refs = symbolTable.GetReferences(symbol.Id);
    if (refs.Count == 0) return $"No references to {name}";
    var list = refs.Take(20).Select(r => $"- Line {r.Line} in {r.SourceFileId}");
    return $"Found {refs.Count} references to {name}:\n{string.Join("\n", list)}";
}

string HandleFindCallers(JsonElement? args)
{
    var name = args?.TryGetProperty("method", out var m) == true ? m.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Symbol not found: {name}";
    var callers = searchEngine.FindCallers(symbol.Id, 1);
    if (callers.Count == 0) return $"No callers found for {name}";
    var list = callers.Take(20).Select(c => $"- {c.Symbol.FullName}");
    return $"Found {callers.Count} callers of {name}:\n{string.Join("\n", list)}";
}

string HandleFindCallees(JsonElement? args)
{
    var name = args?.TryGetProperty("method", out var m) == true ? m.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Symbol not found: {name}";
    var callees = searchEngine.FindCallees(symbol.Id, 1);
    if (callees.Count == 0) return $"No callees found for {name}";
    var list = callees.Take(20).Select(c => $"- {c.Symbol.FullName}");
    return $"Found {callees.Count} callees of {name}:\n{string.Join("\n", list)}";
}

string HandleFindImplementations(JsonElement? args)
{
    var name = args?.TryGetProperty("interfaceName", out var i) == true ? i.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Interface not found: {name}";
    var impls = searchEngine.FindImplementations(symbol.Id);
    if (impls.Count == 0) return $"No implementations found for {name}";
    var list = impls.Take(20).Select(s => $"- {s.FullName}");
    return $"Found {impls.Count} implementations of {name}:\n{string.Join("\n", list)}";
}

string HandleFindDerivedTypes(JsonElement? args)
{
    var name = args?.TryGetProperty("typeName", out var t) == true ? t.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Type not found: {name}";
    var derived = searchEngine.FindDerivedTypes(symbol.Id);
    if (derived.Count == 0) return $"No derived types found for {name}";
    var list = derived.Take(20).Select(s => $"- {s.FullName}");
    return $"Found {derived.Count} derived types of {name}:\n{string.Join("\n", list)}";
}

string HandleSearchCode(JsonElement? args)
{
    var query = args?.TryGetProperty("query", out var q) == true ? q.GetString() ?? "" : "";
    var results = searchEngine.FindSymbol(query, new SearchOptions { MaxResults = 20 });
    if (results.Count == 0) return $"No results for '{query}'";
    var list = results.Take(20).Select(r => $"- {r.Symbol.FullName} ({r.Symbol.Kind}) Score: {r.Score:F2}");
    return $"Found {results.Count} results for '{query}':\n{string.Join("\n", list)}";
}

string HandleGetSymbolInfo(JsonElement? args)
{
    var name = args?.TryGetProperty("symbolName", out var n) == true ? n.GetString() ?? "" : "";
    var symbol = symbolTable.GetByFullName(name);
    if (symbol == null) return $"Symbol not found: {name}";
    var callers = searchEngine.FindCallers(symbol.Id, 1);
    var callees = searchEngine.FindCallees(symbol.Id, 1);
    var refs = symbolTable.GetReferences(symbol.Id);
    return $"Symbol: {symbol.FullName}\nKind: {symbol.Kind}\nFile: {symbol.FilePath}:{symbol.StartLine}\nAccess: {symbol.AccessModifier}\nCallers: {callers.Count}\nCallees: {callees.Count}\nReferences: {refs.Count}";
}

string HandleGetGraphStats()
{
    return $"Graph Statistics:\nNodes: {graph.NodeCount}\nEdges: {graph.EdgeCount}\nSymbols: {symbolTable.Count}";
}

string HandleExplainArchitecture()
{
    var classes = symbolTable.GetByKind(SymbolKind.Type).Where(s => s.TypeName == "Class").Count();
    var methods = symbolTable.GetByKind(SymbolKind.Method).Count;
    var props = symbolTable.GetByKind(SymbolKind.Property).Count;
    var fields = symbolTable.GetByKind(SymbolKind.Field).Count;
    return $"Architecture:\nTotal Symbols: {symbolTable.Count}\nClasses: {classes}\nMethods: {methods}\nProperties: {props}\nFields: {fields}\nGraph Nodes: {graph.NodeCount}\nGraph Edges: {graph.EdgeCount}";
}

string HandleBlastRadius(JsonElement? args)
{
    var symbolName = args?.TryGetProperty("symbolName", out var n) == true ? n.GetString() ?? "" : "";
    var depth = args?.TryGetProperty("depth", out var d) == true ? d.GetInt32() : 2;

    var symbol = symbolTable.GetByFullName(symbolName);
    if (symbol == null) return $"Symbol not found: {symbolName}";

    var callers = searchEngine.FindCallers(symbol.Id, depth).ToList();
    var callees = searchEngine.FindCallees(symbol.Id, depth).ToList();
    var refs = symbolTable.GetReferences(symbol.Id);

    var derivedTypes = new List<SymbolEntity>();
    var implementations = new List<SymbolEntity>();
    if (symbol.Kind == SymbolKind.Type)
    {
        derivedTypes = searchEngine.FindDerivedTypes(symbol.Id).ToList();
        if (symbol.TypeName == "Interface")
            implementations = searchEngine.FindImplementations(symbol.Id).ToList();
    }

    var testRefs = refs.Where(r => r.Context != null && (r.Context.Contains("Test") || r.Context.Contains("test"))).ToList();
    var riskScore = CalculateRisk(callers.Count, callees.Count, refs.Count, derivedTypes.Count, implementations.Count);

    var callersList = callers.Take(20).Select(c => $"- {c.Symbol.FullName} (depth {c.Depth})");
    var calleesList = callees.Take(20).Select(c => $"- {c.Symbol.FullName} (depth {c.Depth})");
    var derivedList = derivedTypes.Take(10).Select(d => $"- {d.FullName}");
    var implList = implementations.Take(10).Select(i => $"- {i.FullName}");
    var testList = testRefs.Take(10).Select(t => $"- Line {t.Line}");

    return $"""
Blast Radius Analysis for: {symbol.FullName}
Kind: {symbol.Kind}
File: {symbol.FilePath}:{symbol.StartLine}

Risk Score: {riskScore}/100 ({GetRiskLevel(riskScore)})

--- Summary ---
{GenerateSummary(symbol, callers.Count, callees.Count, refs.Count, derivedTypes.Count, implementations.Count, testRefs.Count)}

--- Callers ({callers.Count}) ---
{(callersList.Any() ? string.Join("\n", callersList) : "None")}

--- Callees ({callees.Count}) ---
{(calleesList.Any() ? string.Join("\n", calleesList) : "None")}

--- References ({refs.Count}) ---

--- Derived Types ({derivedTypes.Count}) ---
{(derivedList.Any() ? string.Join("\n", derivedList) : "None")}

--- Implementations ({implementations.Count}) ---
{(implList.Any() ? string.Join("\n", implList) : "None")}

--- Test Coverage ({testRefs.Count} references) ---
{(testList.Any() ? string.Join("\n", testList) : "⚠️ No test coverage found")}
""";
}

int CalculateRisk(int callers, int callees, int refs, int derived, int impls)
{
    int score = 0;
    score += Math.Min(callers * 5, 50);
    score += Math.Min(callees * 2, 20);
    score += Math.Min(refs * 1, 15);
    score += Math.Min(derived * 10, 15);
    score += Math.Min(impls * 5, 10);
    return Math.Min(score, 100);
}

string GetRiskLevel(int score) => score switch
{
    >= 70 => "HIGH",
    >= 40 => "MEDIUM",
    >= 15 => "LOW",
    _ => "MINIMAL"
};

string GenerateSummary(SymbolEntity symbol, int callers, int callees, int refs, int derived, int impls, int tests)
{
    var parts = new List<string>();
    parts.Add($"Changing {symbol.Name} ({symbol.Kind}) would directly affect {callers} caller(s).");
    if (callees > 0) parts.Add($"It calls {callees} other method(s).");
    if (refs > 1) parts.Add($"There are {refs} total references.");
    if (derived > 0) parts.Add($"{derived} type(s) inherit from this type.");
    if (impls > 0) parts.Add($"{impls} type(s) implement this interface.");
    if (tests == 0) parts.Add("⚠️ WARNING: No test coverage found.");
    else parts.Add($"✓ {tests} test(s) reference this symbol.");
    return string.Join(" ", parts);
}

string HandleListSymbols(JsonElement? args)
{
    var kindStr = args?.TryGetProperty("kind", out var k) == true ? k.GetString() ?? "" : "";
    var nameFilter = args?.TryGetProperty("name", out var n) == true ? n.GetString() ?? "" : "";

    SymbolKind? kind = kindStr?.ToLower() switch
    {
        "class" or "type" => SymbolKind.Type,
        "method" => SymbolKind.Method,
        "property" => SymbolKind.Property,
        "field" => SymbolKind.Field,
        "event" => SymbolKind.Event,
        "interface" or "struct" or "enum" => SymbolKind.Type,
        "namespace" => SymbolKind.Namespace,
        _ => null
    };

    List<SymbolEntity> symbols;
    if (kind.HasValue)
    {
        symbols = symbolTable.GetByKind(kind.Value).ToList();
        if (kindStr?.ToLower() is "interface" or "struct" or "enum")
        {
            symbols = symbols.Where(s => s.TypeName?.Equals(kindStr, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }
    }
    else
    {
        symbols = symbolTable.GetByKind(SymbolKind.Type)
            .Concat(symbolTable.GetByKind(SymbolKind.Method))
            .Concat(symbolTable.GetByKind(SymbolKind.Property))
            .Concat(symbolTable.GetByKind(SymbolKind.Field))
            .Concat(symbolTable.GetByKind(SymbolKind.Event))
            .ToList();
    }

    if (!string.IsNullOrEmpty(nameFilter))
    {
        var lowerFilter = nameFilter.ToLower();
        symbols = symbols.Where(s => s.Name.Contains(lowerFilter, StringComparison.OrdinalIgnoreCase) ||
                                      s.FullName.Contains(lowerFilter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    symbols = symbols.OrderBy(s => s.Name).ToList();

    var lines = new List<string>();
    lines.Add($"Found {symbols.Count} symbols" + (string.IsNullOrEmpty(kindStr) ? "" : $" of kind '{kindStr}'") + (string.IsNullOrEmpty(nameFilter) ? "" : $" matching '{nameFilter}'"));
    lines.Add("");

    foreach (var s in symbols.Take(100))
    {
        lines.Add($"- {s.Name} ({s.Kind}) @ {s.FilePath}:{s.StartLine}");
    }

    if (symbols.Count > 100)
        lines.Add($"\n... and {symbols.Count - 100} more");

    return string.Join("\n", lines);
}

async Task<string> HandleIndexRepository(JsonElement args)
{
    var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
    if (string.IsNullOrWhiteSpace(path))
        return "Error: No repository path provided";

    if (!Directory.Exists(path))
        return $"Error: Directory not found: {path}";

    Console.Error.WriteLine($"[MCP] Indexing: {path}");

    var indexer = new CodeIndexer();
    var result = await indexer.IndexAsync(path, new IndexOptions());

    if (result.Success)
    {
        foreach (var symbol in indexer.SymbolTable.GetByKind(NexusCode.Domain.SymbolKind.Type))
            symbolTable.Add(symbol);
        foreach (var symbol in indexer.SymbolTable.GetByKind(NexusCode.Domain.SymbolKind.Method))
            symbolTable.Add(symbol);
        foreach (var symbol in indexer.SymbolTable.GetByKind(NexusCode.Domain.SymbolKind.Property))
            symbolTable.Add(symbol);
        foreach (var symbol in indexer.SymbolTable.GetByKind(NexusCode.Domain.SymbolKind.Field))
            symbolTable.Add(symbol);

        foreach (var node in indexer.Graph.GetNodesByKind(NexusCode.Domain.NodeKind.Class))
            graph.AddNode(node);
        foreach (var node in indexer.Graph.GetNodesByKind(NexusCode.Domain.NodeKind.Method))
            graph.AddNode(node);
        foreach (var node in indexer.Graph.GetNodesByKind(NexusCode.Domain.NodeKind.Property))
            graph.AddNode(node);
        foreach (var node in indexer.Graph.GetNodesByKind(NexusCode.Domain.NodeKind.Field))
            graph.AddNode(node);

        foreach (var edge in indexer.Graph.GetEdgesByKind(NexusCode.Domain.EdgeKind.Calls))
            graph.AddEdge(edge);
        foreach (var edge in indexer.Graph.GetEdgesByKind(NexusCode.Domain.EdgeKind.Inherits))
            graph.AddEdge(edge);
        foreach (var edge in indexer.Graph.GetEdgesByKind(NexusCode.Domain.EdgeKind.Implements))
            graph.AddEdge(edge);
        foreach (var edge in indexer.Graph.GetEdgesByKind(NexusCode.Domain.EdgeKind.Declares))
            graph.AddEdge(edge);

        Console.Error.WriteLine($"[MCP] Indexed: {result.FilesIndexed} files, {result.SymbolsExtracted} symbols, {result.GraphNodesCreated} nodes");

        return $"Indexed successfully!\nFiles: {result.FilesIndexed}\nSymbols: {result.SymbolsExtracted}\nGraph Nodes: {result.GraphNodesCreated}\nGraph Edges: {result.GraphEdgesCreated}\nDuration: {result.Duration.TotalSeconds:F2}s";
    }
    else
    {
        return $"Indexing failed: {result.Error}";
    }
}

object MakeResponse(int? id, object? result = null, object? error = null)
{
    var r = new Dictionary<string, object?> { ["jsonrpc"] = "2.0", ["id"] = id };
    if (result != null) r["result"] = result;
    if (error != null) r["error"] = error;
    return r;
}

class McpSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> RecentSymbols { get; set; } = [];
    public string? ActiveRepository { get; set; }

    public void TrackSymbol(string fullName)
    {
        if (RecentSymbols.Count >= 10) RecentSymbols.RemoveAt(0);
        RecentSymbols.Add(fullName);
    }
}
