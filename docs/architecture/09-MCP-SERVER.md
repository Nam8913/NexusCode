# Nexus Code Intelligence Platform - MCP Server

## Overview

The MCP (Model Context Protocol) Server is the primary integration layer for AI agents. It exposes code intelligence capabilities as MCP tools, allowing AI coding assistants to query the codebase using a standardized protocol.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    MCP Server                            │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ JSON-RPC     │  │   Protocol   │  │   Capability │  │
│  │ Transport    │→│   Handler    │→│   Negotiator  │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Tool       │  │   Resource   │  │   Prompt     │  │
│  │   Registry   │  │   Provider   │  │   Provider   │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ Symbol Search │  │ Knowledge    │  │ Context      │  │
│  │ Engine       │  │ Graph Engine │  │ Builder      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2. MCP Protocol

### 2.1 Message Format

```json
// Request
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "find_symbol",
    "arguments": {
      "query": "PlayerController",
      "kind": "class"
    }
  }
}

// Response
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Found class PlayerController in Assets/Scripts/Player/PlayerController.cs"
      }
    ]
  }
}
```

### 2.2 Server Capabilities

```json
{
  "capabilities": {
    "tools": {},
    "resources": {},
    "prompts": {}
  }
}
```

---

## 3. MCP Tools

### 3.1 find_symbol

```
Tool: find_symbol
Description: Find a symbol by name or qualified name

Input:
{
  "query": string,          // Symbol name or partial name
  "kind": string?,          // Optional: class, method, property, field, event
  "namespace": string?,     // Optional: namespace filter
  "project": string?,       // Optional: project filter
  "maxResults": int?        // Optional: max results (default 10)
}

Output:
{
  "symbols": [
    {
      "name": string,
      "fullName": string,
      "kind": string,
      "filePath": string,
      "line": int,
      "col": int,
      "summary": string,
      "metadata": {}
    }
  ],
  "totalMatches": int
}

Internal Flow:
1. Parse query and options
2. Search SymbolSearchIndex
3. Rank results by relevance
4. Return formatted results

Error Handling:
- Empty query → return error message
- No matches → return empty list with message
- Invalid kind → return error with valid kinds
```

### 3.2 find_references

```
Tool: find_references
Description: Find all references to a symbol

Input:
{
  "symbolName": string,     // Fully qualified symbol name
  "excludeDefinition": bool?, // Optional: exclude definition (default true)
  "includeSnippets": bool?, // Optional: include code snippets (default true)
  "contextLines": int?      // Optional: lines of context (default 2)
}

Output:
{
  "references": [
    {
      "filePath": string,
      "line": int,
      "col": int,
      "snippet": string,
      "kind": string         // read, write, call, type
    }
  ],
  "totalCount": int
}

Internal Flow:
1. Resolve symbol by name
2. Get all references from ReferenceTracker
3. Load code snippets if requested
4. Return formatted references

Error Handling:
- Symbol not found → return error with suggestion
- No references → return empty list with message
```

### 3.3 find_callers

```
Tool: find_callers
Description: Find all methods that call a given method

Input:
{
  "method": string,         // Fully qualified method name
  "maxDepth": int?,         // Optional: traversal depth (default 1)
  "includeIndirect": bool?  // Optional: include indirect callers (default false)
}

Output:
{
  "callers": [
    {
      "method": {
        "name": string,
        "fullName": string,
        "filePath": string,
        "line": int
      },
      "callSite": {
        "filePath": string,
        "line": int,
        "snippet": string
      },
      "depth": int
    }
  ],
  "totalCallers": int
}

Internal Flow:
1. Resolve method symbol
2. Traverse incoming CALLS edges
3. Collect caller information
4. Optionally traverse deeper for indirect callers
5. Return formatted results

Error Handling:
- Method not found → return error
- No callers → return empty list with message
- Max depth exceeded → return results with warning
```

### 3.4 find_callees

```
Tool: find_callees
Description: Find all methods called by a given method

Input:
{
  "method": string,         // Fully qualified method name
  "maxDepth": int?,         // Optional: traversal depth (default 1)
  "includeIndirect": bool?  // Optional: include indirect callees (default false)
}

Output:
{
  "callees": [
    {
      "method": {
        "name": string,
        "fullName": string,
        "filePath": string,
        "line": int
      },
      "callSite": {
        "filePath": string,
        "line": int,
        "snippet": string
      },
      "depth": int
    }
  ],
  "totalCallees": int
}

Internal Flow:
1. Resolve method symbol
2. Traverse outgoing CALLS edges
3. Collect callee information
4. Optionally traverse deeper for indirect callees
5. Return formatted results

Error Handling:
- Method not found → return error
- No callees → return empty list with message
```

### 3.5 find_dependencies

```
Tool: find_dependencies
Description: Find dependencies of a project

Input:
{
  "project": string,        // Project name or path
  "depth": int?,            // Optional: traversal depth (default 1)
  "includeTransitive": bool? // Optional: include transitive deps (default true)
}

Output:
{
  "dependencies": [
    {
      "name": string,
      "version": string?,
      "kind": string,       // project, package, assembly
      "dependencies": []    // nested dependencies
    }
  ],
  "totalDependencies": int
}

Internal Flow:
1. Resolve project
2. Traverse DEPENDS_ON edges
3. Collect dependency information
4. Optionally include transitive dependencies
5. Return formatted results

Error Handling:
- Project not found → return error
- No dependencies → return empty list with message
```

### 3.6 search_code

```
Tool: search_code
Description: Search code by content or pattern

Input:
{
  "query": string,          // Search query
  "kind": string?,          // Optional: file, symbol, reference
  "project": string?,       // Optional: project filter
  "namespace": string?,     // Optional: namespace filter
  "maxResults": int?        // Optional: max results (default 20)
}

Output:
{
  "results": [
    {
      "filePath": string,
      "line": int,
      "col": int,
      "snippet": string,
      "kind": string,
      "score": float
    }
  ],
  "totalResults": int
}

Internal Flow:
1. Parse query
2. Search appropriate index (symbol, reference, or file)
3. Rank results by relevance
4. Return formatted results

Error Handling:
- Empty query → return error
- No results → return empty list with message
```

### 3.7 search_graph

```
Tool: search_graph
Description: Search the knowledge graph

Input:
{
  "query": string,          // Search query or pattern
  "nodeKind": string?,      // Optional: filter by node kind
  "edgeKind": string?,      // Optional: filter by edge kind
  "maxDepth": int?,         // Optional: traversal depth (default 2)
  "maxResults": int?        // Optional: max results (default 20)
}

Output:
{
  "nodes": [
    {
      "id": string,
      "label": string,
      "kind": string,
      "metadata": {}
    }
  ],
  "edges": [
    {
      "source": string,
      "target": string,
      "kind": string
    }
  ],
  "totalNodes": int,
  "totalEdges": int
}

Internal Flow:
1. Parse query and options
2. Find matching nodes
3. Traverse graph from matching nodes
4. Collect nodes and edges
5. Return formatted results

Error Handling:
- No matching nodes → return empty graph
- Invalid node kind → return error with valid kinds
```

### 3.8 build_context

```
Tool: build_context
Description: Build context for an AI agent question

Input:
{
  "question": string,       // Natural language question
  "maxTokens": int?,        // Optional: max tokens (default 4000)
  "strategy": string?,      // Optional: symbol, graph, vector, hybrid (default hybrid)
  "includeCode": bool?,     // Optional: include code snippets (default true)
  "includeGraph": bool?     // Optional: include graph info (default true)
}

Output:
{
  "context": {
    "summary": string,
    "symbols": [
      {
        "name": string,
        "fullName": string,
        "kind": string,
        "filePath": string,
        "line": int,
        "snippet": string,
        "relevanceScore": float
      }
    ],
    "relationships": [
      {
        "from": string,
        "to": string,
        "kind": string,
        "description": string
      }
    ],
    "codeSnippets": [
      {
        "filePath": string,
        "startLine": int,
        "endLine": int,
        "content": string
      }
    ],
    "tokenCount": int
  }
}

Internal Flow:
1. Parse question
2. Extract key terms and intent
3. Symbol search for relevant symbols
4. Graph expansion for relationships
5. Vector search for similar code (optional)
6. Context compression if over token limit
7. Format context for LLM consumption
8. Return structured context

Error Handling:
- Question too vague → return partial context with suggestions
- Token limit exceeded → compress context
- No relevant symbols → return empty context with message
```

### 3.9 explain_architecture

```
Tool: explain_architecture
Description: Explain the architecture of the codebase

Input:
{
  "scope": string?,         // Optional: full, project, namespace (default full)
  "project": string?,       // Optional: specific project
  "namespace": string?,     // Optional: specific namespace
  "maxDepth": int?          // Optional: detail level (default 3)
}

Output:
{
  "architecture": {
    "overview": string,
    "projects": [
      {
        "name": string,
        "description": string,
        "dependencies": [],
        "keyTypes": []
      }
    ],
    "namespaces": [
      {
        "name": string,
        "description": string,
        "types": []
      }
    ],
    "patterns": [
      {
        "name": string,
        "description": string,
        "examples": []
      }
    ],
    "graphVisualization": string  // Mermaid diagram
  }
}

Internal Flow:
1. Analyze project structure
2. Identify key patterns (MVC, MVVM, ECS, etc.)
3. Map namespace relationships
4. Generate architecture description
5. Create graph visualization
6. Return structured explanation

Error Handling:
- No code indexed → return error with setup instructions
- Invalid scope → return error with valid scopes
```

### 3.10 trace_execution_flow

```
Tool: trace_execution_flow
Description: Trace the execution flow from a starting point

Input:
{
  "startMethod": string,    // Fully qualified method name
  "maxDepth": int?,         // Optional: max depth (default 5)
  "includeBranches": bool?, // Optional: include branching paths (default true)
  "filter": string?         // Optional: filter by namespace/project
}

Output:
{
  "flow": {
    "startMethod": {
      "name": string,
      "fullName": string,
      "filePath": string,
      "line": int
    },
    "steps": [
      {
        "method": {
          "name": string,
          "fullName": string,
          "filePath": string,
          "line": int
        },
        "callType": string,  // direct, virtual, interface
        "depth": int,
        "lineNumber": int
      }
    ],
    "branches": [
      {
        "condition": string,
        "path": []
      }
    ]
  },
  "totalSteps": int,
  "maxDepthReached": int
}

Internal Flow:
1. Resolve start method
2. BFS/DFS through CALLS edges
3. Track depth and branches
4. Collect method information
5. Return execution flow

Error Handling:
- Method not found → return error
- Max depth exceeded → return partial flow with warning
- Circular reference detected → return flow with cycle marker
```

---

## 4. MCP Resources

### 4.1 Symbol Resource

```
Resource: symbol://{fullName}
Description: Get detailed information about a symbol

Content:
{
  "name": string,
  "fullName": string,
  "kind": string,
  "filePath": string,
  "line": int,
  "col": int,
  "summary": string,
  "metadata": {},
  "references": [],
  "callers": [],
  "callees": []
}
```

### 4.2 Graph Resource

```
Resource: graph://{nodeId}
Description: Get graph information for a node

Content:
{
  "node": {
    "id": string,
    "label": string,
    "kind": string,
    "metadata": {}
  },
  "incomingEdges": [],
  "outgoingEdges": [],
  "relatedNodes": []
}
```

### 4.3 File Resource

```
Resource: file://{filePath}
Description: Get file content and metadata

Content:
{
  "filePath": string,
  "content": string,
  "language": string,
  "symbols": [],
  "dependencies": []
}
```

---

## 5. MCP Prompts

### 5.1 Code Explanation Prompt

```
Prompt: explain_code
Description: Generate a prompt for explaining code

Arguments:
{
  "code": string,           // Code to explain
  "context": string?        // Additional context
}

Generated Prompt:
"You are a C# expert. Explain the following code in detail:

{code}

{context}

Focus on:
1. What the code does
2. How it works
3. Key patterns used
4. Potential issues or improvements"
```

### 5.2 Debugging Prompt

```
Prompt: debug_code
Description: Generate a prompt for debugging code

Arguments:
{
  "code": string,           // Code with issue
  "error": string,          // Error message
  "context": string?        // Additional context
}

Generated Prompt:
"You are a C# debugging expert. Help debug the following code:

Code:
{code}

Error:
{error}

{context}

Provide:
1. Root cause analysis
2. Step-by-step debugging approach
3. Potential fixes
4. Prevention strategies"
```

### 5.3 Refactoring Prompt

```
Prompt: refactor_code
Description: Generate a prompt for refactoring code

Arguments:
{
  "code": string,           // Code to refactor
  "goal": string,           // Refactoring goal
  "constraints": string?    // Constraints to follow
}

Generated Prompt:
"You are a C# refactoring expert. Refactor the following code:

Code:
{code}

Goal: {goal}

{constraints}

Provide:
1. Refactored code
2. Explanation of changes
3. Trade-offs considered
4. Testing recommendations"
```

---

## 6. Protocol Handler

```
class McpProtocolHandler
{
    toolRegistry: ToolRegistry
    resourceProvider: ResourceProvider
    promptProvider: PromptProvider
    
    HandleRequest(request: McpRequest):
        switch request.Method:
            case "initialize":
                return HandleInitialize(request)
            
            case "tools/list":
                return HandleListTools(request)
            
            case "tools/call":
                return HandleCallTool(request)
            
            case "resources/list":
                return HandleListResources(request)
            
            case "resources/read":
                return HandleReadResource(request)
            
            case "prompts/list":
                return HandleListPrompts(request)
            
            case "prompts/get":
                return HandleGetPrompt(request)
            
            default:
                return CreateErrorResponse(-32601, "Method not found")
    
    HandleInitialize(request: McpRequest):
        return new McpResponse {
            Result = new InitializeResult {
                ProtocolVersion = "2024-11-05",
                Capabilities = new ServerCapabilities {
                    Tools = new ToolsCapability(),
                    Resources = new ResourcesCapability(),
                    Prompts = new PromptsCapability()
                },
                ServerInfo = new ServerInfo {
                    Name = "nexus-code-intelligence",
                    Version = "1.0.0"
                }
            }
        }
    
    HandleCallTool(request: McpRequest):
        toolName = request.Params["name"] as string
        arguments = request.Params["arguments"] as Dictionary<string, object>
        
        // Validate tool exists
        tool = toolRegistry.GetTool(toolName)
        if tool == null:
            return CreateErrorResponse(-32602, $"Tool not found: {toolName}")
        
        // Validate input
        validationResult = tool.ValidateInput(arguments)
        if !validationResult.IsValid:
            return CreateErrorResponse(-32602, validationResult.ErrorMessage)
        
        // Execute tool
        try:
            result = await tool.Execute(arguments)
            return new McpResponse {
                Result = new CallToolResult {
                    Content = [
                        new TextContent { Text = JsonSerializer.Serialize(result) }
                    ]
                }
            }
        catch Exception ex:
            return CreateErrorResponse(-32603, ex.Message)
    
    HandleListTools(request: McpRequest):
        tools = toolRegistry.GetAllTools()
        
        return new McpResponse {
            Result = new ListToolsResult {
                Tools = tools.Select(t => new Tool {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = t.InputSchema
                }).ToList()
            }
        }
```

---

## 7. Error Handling

```csharp
class McpErrorHandler
{
    static McpResponse CreateErrorResponse(int code, string message, object? data = null)
    {
        return new McpResponse
        {
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }
    
    // Standard MCP error codes
    static class ErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
```

---

## 8. Streaming Support

```
class McpStreamingHandler
{
    async StreamToolResult(toolName: string, arguments: Dictionary<string, object>):
        // For large results, stream in chunks
        tool = toolRegistry.GetTool(toolName)
        
        await foreach chunk in tool.ExecuteStreaming(arguments):
            yield return new McpResponse {
                Result = new CallToolResult {
                    Content = [
                        new TextContent { Text = JsonSerializer.Serialize(chunk) }
                    ],
                    IsPartial = true
                }
            }
        
        // Final response
        yield return new McpResponse {
            Result = new CallToolResult {
                Content = [
                    new TextContent { Text = "{}" }
                ],
                IsPartial = false
            }
        }
}
```

---

## 9. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Tool Registration | O(1) | O(T) where T = tools |
| Tool Lookup | O(1) | O(1) |
| Input Validation | O(S) where S = schema size | O(1) |
| Tool Execution | Depends on tool | Depends on tool |
| Resource Read | O(1) | O(R) where R = resource size |
| Streaming | O(N) where N = chunks | O(1) per chunk |
