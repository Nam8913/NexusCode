# Nexus Code Intelligence Platform - API Documentation

## Base URL

```
http://localhost:5000
```

## Endpoints

### Health Check

```
GET /health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2026-06-25T21:00:00Z"
}
```

---

### Index Repository

```
POST /api/index/repository
Content-Type: application/json
```

Request:
```json
{
  "path": "D:\\MyProject"
}
```

Response:
```json
{
  "success": true,
  "filesIndexed": 150,
  "symbolsExtracted": 1200,
  "graphNodesCreated": 1150,
  "graphEdgesCreated": 2300,
  "duration": "00:00:03.5000000"
}
```

### Index Status

```
GET /api/index/status
```

Response:
```json
{
  "indexed": true,
  "symbols": 1200,
  "graphNodes": 1150,
  "graphEdges": 2300
}
```

---

### Search Symbols

```
GET /api/search/symbol?query={query}&kind={kind}&maxResults={max}
```

Parameters:
- `query` (required): Search query
- `kind` (optional): Type filter (class, method, property, field)
- `maxResults` (optional): Max results (default 20)

Response:
```json
[
  {
    "name": "PlayerController",
    "fullName": "Game.PlayerController",
    "kind": "Type",
    "filePath": "Assets/Scripts/Player.cs",
    "startLine": 10,
    "score": 1.0
  }
]
```

### Find Callers

```
GET /api/search/callers/{symbolName}?maxDepth={depth}
```

### Find Callees

```
GET /api/search/callees/{symbolName}?maxDepth={depth}
```

### Find Implementations

```
GET /api/search/implementations/{interfaceName}
```

### Find Derived Types

```
GET /api/search/derived/{typeName}
```

---

### Graph Statistics

```
GET /api/graph/stats
```

### Graph Nodes by Kind

```
GET /api/graph/nodes/{kind}
```

### Graph Edges by Kind

```
GET /api/graph/edges/{kind}
```

### Mermaid Diagram

```
GET /api/graph/mermaid
```

---

### Multi-Repo: Index Multiple

```
POST /api/multi-repo/index
Content-Type: application/json
```

Request:
```json
{
  "paths": ["D:\\Repo1", "D:\\Repo2"]
}
```

### Multi-Repo: List

```
GET /api/multi-repo/list
```

### Multi-Repo: Search

```
GET /api/multi-repo/search?query={query}
```

### Multi-Repo: Compare

```
GET /api/multi-repo/compare?repo1={name1}&repo2={name2}
```

### Multi-Repo: Health

```
GET /api/multi-repo/health/{repoName}
```

---

## Swagger UI

Access Swagger UI at:

```
http://localhost:5000/swagger
```

## MCP Tools (12 tools available)

The NexusCode MCP server provides 12 tools for AI agents:

| Tool | Description | Parameters |
|------|-------------|------------|
| `index_repository` | Index a C# repository | `path` |
| `find_symbol` | Find symbol by name | `query`, `kind` |
| `find_references` | Find all references | `symbolName` |
| `find_callers` | Find callers of a method | `method` |
| `find_callees` | Find callees of a method | `method` |
| `find_implementations` | Find interface implementations | `interfaceName` |
| `find_derived_types` | Find derived types | `typeName` |
| `search_code` | Search code | `query` |
| `get_symbol_info` | Get symbol details | `symbolName` |
| `explain_architecture` | Explain architecture | - |
| `blast_radius` | Analyze change impact | `symbolName`, `depth` |
| `pop_symbols` | List symbols by kind/name | `kind`, `name` |

### MCP Configuration

```json
{
  "mcp": {
    "NexusCode": {
      "command": "D:\\NexusCode\\publish\\NexusCode.Mcp.exe"
    }
  }
}
```

## Error Responses

All error responses follow this format:

```json
{
  "error": "Error message"
}
```

Status codes:
- `200`: Success
- `400`: Bad request
- `404`: Not found
- `500`: Internal server error
