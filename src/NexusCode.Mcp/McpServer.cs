using System.Text.Json;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Mcp;

public sealed class McpServer
{
    private readonly SymbolSearchEngine _searchEngine;
    private readonly SymbolTable _symbolTable;
    private readonly KnowledgeGraph _graph;
    private readonly Dictionary<string, McpTool> _tools = new();

    public McpServer(SymbolSearchEngine searchEngine, SymbolTable symbolTable, KnowledgeGraph graph)
    {
        _searchEngine = searchEngine;
        _symbolTable = symbolTable;
        _graph = graph;
        RegisterTools();
    }

    private void RegisterTools()
    {
        _tools["find_symbol"] = new McpTool
        {
            Name = "find_symbol",
            Description = "Find a symbol by name or qualified name",
            InputSchema = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Symbol name" },
                ["kind"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Filter: class, method, property, field" }
            },
            Handler = HandleFindSymbol
        };

        _tools["find_references"] = new McpTool
        {
            Name = "find_references",
            Description = "Find all references to a symbol",
            InputSchema = new Dictionary<string, object>
            {
                ["symbolName"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified symbol name" }
            },
            Handler = HandleFindReferences
        };

        _tools["find_callers"] = new McpTool
        {
            Name = "find_callers",
            Description = "Find all methods that call a given method",
            InputSchema = new Dictionary<string, object>
            {
                ["method"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified method name" },
                ["maxDepth"] = new Dictionary<string, string> { ["type"] = "integer", ["description"] = "Traversal depth" }
            },
            Handler = HandleFindCallers
        };

        _tools["find_callees"] = new McpTool
        {
            Name = "find_callees",
            Description = "Find all methods called by a given method",
            InputSchema = new Dictionary<string, object>
            {
                ["method"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified method name" },
                ["maxDepth"] = new Dictionary<string, string> { ["type"] = "integer", ["description"] = "Traversal depth" }
            },
            Handler = HandleFindCallees
        };

        _tools["find_implementations"] = new McpTool
        {
            Name = "find_implementations",
            Description = "Find all types implementing an interface",
            InputSchema = new Dictionary<string, object>
            {
                ["interfaceName"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified interface name" }
            },
            Handler = HandleFindImplementations
        };

        _tools["find_derived_types"] = new McpTool
        {
            Name = "find_derived_types",
            Description = "Find all types inheriting from a type",
            InputSchema = new Dictionary<string, object>
            {
                ["typeName"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified type name" }
            },
            Handler = HandleFindDerivedTypes
        };

        _tools["search_code"] = new McpTool
        {
            Name = "search_code",
            Description = "Search code by content or pattern",
            InputSchema = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Search query" },
                ["maxResults"] = new Dictionary<string, string> { ["type"] = "integer", ["description"] = "Max results" }
            },
            Handler = HandleSearchCode
        };

        _tools["get_symbol_info"] = new McpTool
        {
            Name = "get_symbol_info",
            Description = "Get detailed information about a symbol",
            InputSchema = new Dictionary<string, object>
            {
                ["symbolName"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified symbol name" }
            },
            Handler = HandleGetSymbolInfo
        };

        // _tools["get_graph_stats"] = new McpTool
        // {
        //     Name = "get_graph_stats",
        //     Description = "Get knowledge graph statistics",
        //     InputSchema = new Dictionary<string, object>(),
        //     Handler = HandleGetGraphStats
        // };

        _tools["explain_architecture"] = new McpTool
        {
            Name = "explain_architecture",
            Description = "Explain the architecture of the codebase",
            InputSchema = new Dictionary<string, object>(),
            Handler = HandleExplainArchitecture
        };

        _tools["blast_radius"] = new McpTool
        {
            Name = "blast_radius",
            Description = "Analyze blast radius - find all code affected if a symbol is changed (callers, callees, references, tests)",
            InputSchema = new Dictionary<string, object>
            {
                ["symbolName"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Fully qualified symbol name to analyze" },
                ["depth"] = new Dictionary<string, string> { ["type"] = "integer", ["description"] = "Traversal depth (default: 2)" }
            },
            Handler = HandleBlastRadius
        };

        _tools["list_symbols"] = new McpTool
        {
            Name = "list_symbols",
            Description = "List all symbols filtered by kind and optional name search",
            InputSchema = new Dictionary<string, object>
            {
                ["kind"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Filter: Class, Method, Property, Field, Event, Interface, Struct, Enum, Namespace (or empty for all)" },
                ["name"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Optional name filter - find symbols containing this text (empty = list all)" }
            },
            Handler = HandleListSymbols
        };
    }

    public async Task<McpResponse?> HandleRequest(McpRequest request)
    {
        try
        {
            // Handle notifications (no id = no response needed)
            if (request.Id == null && request.Method == "notifications/initialized")
            {
                return null; // No response needed for notifications
            }

            if (request.Method == "tools/list")
            {
                return CreateToolsListResponse();
            }

            if (request.Method == "tools/call")
            {
                var toolName = request.Params?.GetProperty("name").GetString();
                var arguments = request.Params?.GetProperty("arguments");

                if (toolName == null || !_tools.TryGetValue(toolName, out var tool))
                {
                    return CreateErrorResponse(-32602, $"Tool not found: {toolName}");
                }

                var result = tool.Handler(arguments);
                return new McpResponse
                {
                    Result = new McpResult
                    {
                        Content = new[] { new McpContent { Type = "text", Text = result } }
                    }
                };
            }

            return CreateErrorResponse(-32601, $"Method not found: {request.Method}");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(-32603, ex.Message);
        }
    }

    private string HandleFindSymbol(JsonElement? args)
    {
        var query = args?.GetProperty("query").GetString() ?? "";
        var kindStr = args?.GetProperty("kind").GetString();

        SymbolKind? kind = kindStr?.ToLower() switch
        {
            "class" => SymbolKind.Type,
            "method" => SymbolKind.Method,
            "property" => SymbolKind.Property,
            "field" => SymbolKind.Field,
            _ => null
        };

        var results = _searchEngine.FindSymbol(query, new SearchOptions { KindFilter = kind });

        var output = new { symbols = results.Select(r => new { r.Symbol.Name, r.Symbol.FullName, Kind = r.Symbol.Kind.ToString(), r.Symbol.FilePath, r.Symbol.StartLine, r.Score, r.MatchType }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleFindReferences(JsonElement? args)
    {
        var symbolName = args?.GetProperty("symbolName").GetString() ?? "";
        var symbol = _symbolTable.GetByFullName(symbolName);

        if (symbol == null)
            return JsonSerializer.Serialize(new { error = $"Symbol not found: {symbolName}" });

        var references = _searchEngine.FindReferences(symbol.Id);

        var output = new { references = references.Select(r => new { r.Line, r.Column, Kind = r.Kind.ToString(), r.Context }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleFindCallers(JsonElement? args)
    {
        var methodName = args?.GetProperty("method").GetString() ?? "";
        var maxDepth = args?.GetProperty("maxDepth").GetInt32() ?? 1;

        var method = _symbolTable.GetByFullName(methodName);
        if (method == null)
            return JsonSerializer.Serialize(new { error = $"Method not found: {methodName}" });

        var callers = _searchEngine.FindCallers(method.Id, maxDepth);

        var output = new { callers = callers.Select(c => new { c.Symbol.Name, c.Symbol.FullName, c.Symbol.FilePath, c.Depth }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleFindCallees(JsonElement? args)
    {
        var methodName = args?.GetProperty("method").GetString() ?? "";
        var maxDepth = args?.GetProperty("maxDepth").GetInt32() ?? 1;

        var method = _symbolTable.GetByFullName(methodName);
        if (method == null)
            return JsonSerializer.Serialize(new { error = $"Method not found: {methodName}" });

        var callees = _searchEngine.FindCallees(method.Id, maxDepth);

        var output = new { callees = callees.Select(c => new { c.Symbol.Name, c.Symbol.FullName, c.Symbol.FilePath, c.Depth }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleFindImplementations(JsonElement? args)
    {
        var interfaceName = args?.GetProperty("interfaceName").GetString() ?? "";
        var iface = _symbolTable.GetByFullName(interfaceName);

        if (iface == null)
            return JsonSerializer.Serialize(new { error = $"Interface not found: {interfaceName}" });

        var implementations = _searchEngine.FindImplementations(iface.Id);

        var output = new { implementations = implementations.Select(i => new { i.Name, i.FullName, i.FilePath }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleFindDerivedTypes(JsonElement? args)
    {
        var typeName = args?.GetProperty("typeName").GetString() ?? "";
        var type = _symbolTable.GetByFullName(typeName);

        if (type == null)
            return JsonSerializer.Serialize(new { error = $"Type not found: {typeName}" });

        var derived = _searchEngine.FindDerivedTypes(type.Id);

        var output = new { derivedTypes = derived.Select(d => new { d.Name, d.FullName, d.FilePath }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleSearchCode(JsonElement? args)
    {
        var query = args?.GetProperty("query").GetString() ?? "";
        var maxResults = args?.GetProperty("maxResults").GetInt32() ?? 20;

        var results = _searchEngine.FindSymbol(query, new SearchOptions { MaxResults = maxResults });

        var output = new { results = results.Select(r => new { r.Symbol.Name, r.Symbol.FullName, Kind = r.Symbol.Kind.ToString(), r.Symbol.FilePath, r.Score }) };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleGetSymbolInfo(JsonElement? args)
    {
        var symbolName = args?.GetProperty("symbolName").GetString() ?? "";
        var symbol = _symbolTable.GetByFullName(symbolName);

        if (symbol == null)
            return JsonSerializer.Serialize(new { error = $"Symbol not found: {symbolName}" });

        var callers = _searchEngine.FindCallers(symbol.Id, 1);
        var callees = _searchEngine.FindCallees(symbol.Id, 1);
        var references = _searchEngine.FindReferences(symbol.Id);

        var output = new
        {
            symbol = new { symbol.Name, symbol.FullName, Kind = symbol.Kind.ToString(), symbol.FilePath, symbol.StartLine, symbol.EndLine, symbol.AccessModifier, symbol.IsStatic, symbol.IsAbstract, symbol.IsVirtual, symbol.IsOverride, symbol.IsAsync, symbol.ReturnType, symbol.Metadata },
            callers = callers.Select(c => new { c.Symbol.Name, c.Symbol.FullName }),
            callees = callees.Select(c => new { c.Symbol.Name, c.Symbol.FullName }),
            referenceCount = references.Count
        };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleGetGraphStats(JsonElement? args)
    {
        var output = new
        {
            nodes = _graph.NodeCount,
            edges = _graph.EdgeCount,
            symbols = _symbolTable.Count,
            nodeKinds = Enum.GetValues<NodeKind>().ToDictionary(k => k.ToString(), k => _graph.GetNodesByKind(k).Count)
        };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleExplainArchitecture(JsonElement? args)
    {
        var classes = _symbolTable.GetByKind(SymbolKind.Type).Count(s => s.TypeName == "Class");
        var interfaces = _symbolTable.GetByKind(SymbolKind.Type).Count(s => s.TypeName == "Interface");
        var methods = _symbolTable.GetByKind(SymbolKind.Method).Count;
        var properties = _symbolTable.GetByKind(SymbolKind.Property).Count;

        var namespaces = _symbolTable.GetByKind(SymbolKind.Namespace).Select(n => n.Name).Distinct().ToList();

        var output = new
        {
            summary = $"The codebase contains {classes} classes, {interfaces} interfaces, {methods} methods, and {properties} properties across {namespaces.Count} namespaces.",
            namespaces,
            totalSymbols = _symbolTable.Count,
            graphNodes = _graph.NodeCount,
            graphEdges = _graph.EdgeCount
        };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private string HandleBlastRadius(JsonElement? args)
    {
        var symbolName = args?.GetProperty("symbolName").GetString() ?? "";
        var depth = args?.GetProperty("depth").GetInt32() ?? 2;

        var symbol = _symbolTable.GetByFullName(symbolName);
        if (symbol == null)
            return JsonSerializer.Serialize(new { error = $"Symbol not found: {symbolName}" });

        // Find callers (what calls this symbol)
        var callers = _searchEngine.FindCallers(symbol.Id, depth).ToList();

        // Find callees (what this symbol calls)
        var callees = _searchEngine.FindCallees(symbol.Id, depth).ToList();

        // Find references
        var references = _searchEngine.FindReferences(symbol.Id);

        // Find derived types if it's a class/interface
        var derivedTypes = new List<SymbolEntity>();
        if (symbol.Kind == SymbolKind.Type)
        {
            derivedTypes = _searchEngine.FindDerivedTypes(symbol.Id).ToList();
        }

        // Find implementations if it's an interface
        var implementations = new List<SymbolEntity>();
        if (symbol.Kind == SymbolKind.Type && symbol.TypeName == "Interface")
        {
            implementations = _searchEngine.FindImplementations(symbol.Id).ToList();
        }

        // Find test files that reference this symbol
        var testReferences = references.Where(r => r.Context != null && (r.Context.Contains("Test") || r.Context.Contains("test"))).ToList();

        // Calculate risk score
        var riskScore = CalculateRiskScore(callers.Count, callees.Count, references.Count, derivedTypes.Count, implementations.Count);

        var output = new
        {
            symbol = new
            {
                name = symbol.Name,
                fullName = symbol.FullName,
                kind = symbol.Kind.ToString(),
                filePath = symbol.FilePath,
                startLine = symbol.StartLine,
                endLine = symbol.EndLine
            },
            blastRadius = new
            {
                riskScore,
                riskLevel = GetRiskLevel(riskScore),
                summary = GenerateBlastRadiusSummary(symbol, callers.Count, callees.Count, references.Count, derivedTypes.Count, implementations.Count, testReferences.Count)
            },
            callers = callers.Select(c => new
            {
                name = c.Symbol.Name,
                fullName = c.Symbol.FullName,
                filePath = c.Symbol.FilePath,
                depth = c.Depth
            }).ToList(),
            callees = callees.Select(c => new
            {
                name = c.Symbol.Name,
                fullName = c.Symbol.FullName,
                filePath = c.Symbol.FilePath,
                depth = c.Depth
            }).ToList(),
            references = references.Select(r => new
            {
                line = r.Line,
                column = r.Column,
                kind = r.Kind.ToString(),
                context = r.Context
            }).ToList(),
            derivedTypes = derivedTypes.Select(d => new
            {
                name = d.Name,
                fullName = d.FullName,
                filePath = d.FilePath
            }).ToList(),
            implementations = implementations.Select(i => new
            {
                name = i.Name,
                fullName = i.FullName,
                filePath = i.FilePath
            }).ToList(),
            tests = testReferences.Select(t => new
            {
                line = t.Line,
                column = t.Column,
                context = t.Context
            }).ToList(),
            impact = new
            {
                directCallers = callers.Count(c => c.Depth == 1),
                indirectCallers = callers.Count(c => c.Depth > 1),
                directCallees = callees.Count(c => c.Depth == 1),
                indirectCallees = callees.Count(c => c.Depth > 1),
                totalReferences = references.Count,
                totalDerivedTypes = derivedTypes.Count,
                totalImplementations = implementations.Count,
                testCoverage = testReferences.Count > 0 ? "Covered" : "Not covered"
            }
        };
        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    private static int CalculateRiskScore(int callers, int callees, int references, int derivedTypes, int implementations)
    {
        int score = 0;
        score += Math.Min(callers * 5, 50);      // Max 50 points from callers
        score += Math.Min(callees * 2, 20);       // Max 20 points from callees
        score += Math.Min(references * 1, 15);    // Max 15 points from references
        score += Math.Min(derivedTypes * 10, 15); // Max 15 points from derived types
        score += Math.Min(implementations * 5, 10); // Max 10 points from implementations
        return Math.Min(score, 100);
    }

    private static string GetRiskLevel(int score)
    {
        return score switch
        {
            >= 70 => "HIGH",
            >= 40 => "MEDIUM",
            >= 15 => "LOW",
            _ => "MINIMAL"
        };
    }

    private static string GenerateBlastRadiusSummary(SymbolEntity symbol, int callers, int callees, int references, int derivedTypes, int implementations, int tests)
    {
        var parts = new List<string>();

        parts.Add($"Changing {symbol.Name} ({symbol.Kind}) would directly affect {callers} caller(s).");

        if (callees > 0)
            parts.Add($"It calls {callees} other method(s) that may need updates.");

        if (references > 1)
            parts.Add($"There are {references} total references to this symbol.");

        if (derivedTypes > 0)
            parts.Add($"{derivedTypes} type(s) inherit from this type and may be impacted.");

        if (implementations > 0)
            parts.Add($"{implementations} type(s) implement this interface.");

        if (tests == 0)
            parts.Add("⚠️ WARNING: No test coverage found for this symbol.");
        else
            parts.Add($"✓ {tests} test(s) reference this symbol.");

        return string.Join(" ", parts);
    }

    private string HandleListSymbols(JsonElement? args)
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
            "interface" => SymbolKind.Type,
            "struct" => SymbolKind.Type,
            "enum" => SymbolKind.Type,
            "namespace" => SymbolKind.Namespace,
            _ => null
        };

        List<SymbolEntity> symbols;
        if (kind.HasValue)
        {
            symbols = _symbolTable.GetByKind(kind.Value).ToList();
            // Special filter for interface/struct/enum
            if (kindStr?.ToLower() is "interface" or "struct" or "enum")
            {
                symbols = symbols.Where(s => s.TypeName?.Equals(kindStr, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }
        }
        else
        {
            symbols = _symbolTable.GetByKind(SymbolKind.Type)
                .Concat(_symbolTable.GetByKind(SymbolKind.Method))
                .Concat(_symbolTable.GetByKind(SymbolKind.Property))
                .Concat(_symbolTable.GetByKind(SymbolKind.Field))
                .Concat(_symbolTable.GetByKind(SymbolKind.Event))
                .ToList();
        }

        // Apply name filter if provided
        if (!string.IsNullOrEmpty(nameFilter))
        {
            var lowerFilter = nameFilter.ToLower();
            symbols = symbols.Where(s => s.Name.Contains(lowerFilter, StringComparison.OrdinalIgnoreCase) ||
                                          s.FullName.Contains(lowerFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Sort by name
        symbols = symbols.OrderBy(s => s.Name).ToList();

        var output = new
        {
            totalCount = symbols.Count,
            kind = kindStr,
            nameFilter = nameFilter,
            symbols = symbols.Take(100).Select(s => new
            {
                name = s.Name,
                fullName = s.FullName,
                kind = s.Kind.ToString(),
                typeName = s.TypeName,
                filePath = s.FilePath,
                line = s.StartLine
            }).ToList()
        };

        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }

    public McpResponse CreateInitializeResponse()
    {
        return new McpResponse
        {
            Result = new McpResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpCapabilities
                {
                    Tools = new McpToolsCapability()
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "NexusCode",
                    Version = "1.0.0"
                }
            }
        };
    }

    private McpResponse CreateToolsListResponse()
    {
        return new McpResponse
        {
            Result = new McpResult
            {
                Tools = _tools.Values.Select(t => new McpToolInfo
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = t.InputSchema
                }).ToList()
            }
        };
    }

    private static McpResponse CreateErrorResponse(int code, string message)
    {
        return new McpResponse
        {
            Error = new McpError
            {
                Code = code,
                Message = message
            }
        };
    }
}

public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> InputSchema { get; set; } = new();
    public Func<JsonElement?, string> Handler { get; set; } = _ => "{}";
}

public class McpRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int? Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public class McpResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int? Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public McpResult? Result { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpResult
{
    [System.Text.Json.Serialization.JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }
    public McpCapabilities? Capabilities { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("serverInfo")]
    public McpServerInfo? ServerInfo { get; set; }
    public IEnumerable<McpContent>? Content { get; set; }
    public IEnumerable<McpToolInfo>? Tools { get; set; }
}

public class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class McpCapabilities
{
    public McpToolsCapability? Tools { get; set; }
}

public class McpToolsCapability { }

public class McpServerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class McpContent
{
    public string Type { get; set; } = "text";
    public string Text { get; set; } = string.Empty;
}

public class McpToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> InputSchema { get; set; } = new();
}
