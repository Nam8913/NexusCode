# Nexus Code Intelligence Platform - User Guide

## Quick Start

### 1. Build the project

```bash
dotnet build
```

### 2. Index a repository

```bash
# CLI
dotnet run --project src/NexusCode.Indexer -- "D:\\path\\to\\project"

# Or use API
curl -X POST http://localhost:5000/api/index/repository \
  -H "Content-Type: application/json" \
  -d '{"path": "D:\\path\\to\\project"}'
```

### 3. Search for symbols

```bash
curl "http://localhost:5000/api/search/symbol?query=PlayerController"
```

### 4. Open Web UI

```bash
dotnet run --project src/NexusCode.UI
# Open http://localhost:5001
```

---

## Features

### Dashboard
- Index repositories
- View statistics
- Check system status

### Symbol Browser
- Search all symbols
- Filter by type (class, method, property)
- View callers and callees

### Search
- Full-text symbol search
- Fuzzy matching
- Cross-repository search

### Graph Visualization
- View knowledge graph
- See relationships between symbols

### Graph RAG
- Ask questions about code
- Get AI-powered answers

### Repository Manager
- Add/remove repositories
- Re-index repositories
- View health status

---

## API Usage

### Index a repository

```bash
curl -X POST http://localhost:5000/api/index/repository \
  -H "Content-Type: application/json" \
  -d '{"path": "D:\\MyProject"}'
```

### Search symbols

```bash
# Find all classes
curl "http://localhost:5000/api/search/symbol?query=Controller&kind=Type"

# Find all methods
curl "http://localhost:5000/api/search/symbol?query=Update&kind=Method"
```

### Find callers

```bash
curl "http://localhost:5000/api/search/callers/Game.PlayerController.Attack"
```

### Find callees

```bash
curl "http://localhost:5000/api/search/callees/Game.Weapon.Fire"
```

### Graph stats

```bash
curl "http://localhost:5000/api/graph/stats"
```

---

## MCP Integration

Configure MCP server for AI agents:

```json
{
  "mcpServers": {
    "nexus": {
      "command": "dotnet",
      "args": ["run", "--project", "src/NexusCode.Api"]
    }
  }
}
```

Available MCP tools:
- `find_symbol` - Find a symbol by name
- `find_references` - Find all references
- `find_callers` - Find callers of a method
- `find_callees` - Find callees of a method
- `find_implementations` - Find interface implementations
- `find_derived_types` - Find derived types
- `search_code` - Search code content
- `get_symbol_info` - Get symbol details
- `get_graph_stats` - Get graph statistics
- `explain_architecture` - Explain codebase architecture

---

## Troubleshooting

### "No compilation found"
Make sure the project has a valid .csproj file.

### "Symbols: 0"
Try cleaning and rebuilding:
```bash
dotnet clean
dotnet build
dotnet run --project src/NexusCode.Indexer -- "path"
```

### Performance with large projects
For projects with 10K+ files, increase parallelism:
```bash
dotnet run --project src/NexusCode.Indexer -- "path" --parallelism 8
```
