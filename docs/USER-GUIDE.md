# Nexus Code Intelligence Platform - User Guide

## Quick Start

### 1. Build the project

```bash
dotnet build
```

### 2. Index a repository

**Option A: CLI**
```bash
dotnet run --project src/NexusCode.Indexer -- "D:\path\to\project"
```

**Option B: API**
```bash
curl -X POST http://localhost:5000/api/index/repository \
  -H "Content-Type: application/json" \
  -d '{"path": "D:\\path\\to\\project"}'
```

**Option C: MCP Tool** (for AI agents)
```
index_repository with path: "D:\path\to\project"
```

### 3. Open NexusGraph (Web UI)

```bash
cd NexusGraph
npm install
npm run dev
# Open http://localhost:3000
```

---

## Running the System

### Start API Server

```bash
dotnet run --project src/NexusCode.Api
# API available at http://localhost:5000
# Swagger at http://localhost:5000/swagger
```

### Start NexusGraph (Frontend)

```bash
cd NexusGraph
npm run dev
# Frontend at http://localhost:3000
```

### Start MCP Server (for AI agents)

```bash
dotnet run --project src/NexusCode.Mcp
# MCP server runs via stdio
```

---

## Features

### Dashboard (`/`)
- Index repositories by path
- View indexing statistics
- Check system status

### Graph Visualization (`/graph`)
- Interactive force-directed graph
- Node/edge type filters (sidebar)
- Search and highlight nodes
- Click node for details
- Fit-to-screen button

### Symbol Browser (`/symbols`)
- Search symbols by name
- Filter by type (class, method, property, field)
- View callers and callees

### Search (`/search`)
- Full-text symbol search
- Fuzzy matching
- Click results for details

### Graph RAG (`/rag`)
- Ask questions about codebase
- Get AI-powered context
- View evidence and generated prompt

### About (`/about`)
- Project information
- Technology stack

---

## API Endpoints

### Index

```bash
# Index a repository
curl -X POST http://localhost:5000/api/index/repository \
  -H "Content-Type: application/json" \
  -d '{"path": "D:\\MyProject"}'

# Check status
curl http://localhost:5000/api/index/status
```

### Search

```bash
# Search symbols
curl "http://localhost:5000/api/search/symbol?query=Controller&kind=Type"

# Find callers
curl "http://localhost:5000/api/search/callers/MyApp.PlayerController.Attack"

# Find callees
curl "http://localhost:5000/api/search/callees/MyApp.Weapon.Fire"

# Find implementations
curl "http://localhost:5000/api/search/implementations/MyApp.IDamageable"

# Find derived types
curl "http://localhost:5000/api/search/derived/MyApp.BaseEnemy"
```

### Graph

```bash
# Graph statistics
curl http://localhost:5000/api/graph/stats

# Export full graph (for NexusGraph)
curl http://localhost:5000/api/graph/export

# Nodes by kind
curl http://localhost:5000/api/graph/nodes/Class

# Edges by kind
curl http://localhost:5000/api/graph/edges/Calls
```

### RAG

```bash
# Ask a question
curl -X POST http://localhost:5000/api/rag/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "How does weapon firing work?"}'
```

### Health

```bash
curl http://localhost:5000/health
```

---

## MCP Integration

### Configuration

```json
{
  "mcp": {
    "NexusCode": {
      "type": "local",
      "command": ["D:\\NexusCode\\src\\NexusCode.Mcp\\publish\\NexusCode.Mcp.exe"]
    }
  }
}
```

**Claude Desktop:**
```json
{
  "mcpServers": {
    "NexusCode": {
      "command": "D:\\NexusCode\\src\\NexusCode.Mcp\\publish\\NexusCode.Mcp.exe"
    }
  }
}
```

### Available MCP Tools (12)

| Tool | Description | Example Usage |
|------|-------------|---------------|
| `index_repository` | Index a C# repository | `path: "D:\\MyProject"` |
| `find_symbol` | Find symbol by name | `query: "PlayerController"` |
| `find_references` | Find all references | `symbolName: "global::MyApp.Weapon.Fire"` |
| `find_callers` | Find methods calling a method | `method: "global::MyApp.Weapon.Fire"` |
| `find_callees` | Find methods called by a method | `method: "global::MyApp.PlayerController.Attack"` |
| `find_implementations` | Find interface implementations | `interfaceName: "global::MyApp.IDamageable"` |
| `find_derived_types` | Find derived types | `typeName: "global::MyApp.BaseEnemy"` |
| `search_code` | Search code by query | `query: "Weapon"` |
| `get_symbol_info` | Get detailed symbol info | `symbolName: "global::MyApp.Weapon"` |
| `explain_architecture` | Explain codebase architecture | `{}` |
| `blast_radius` | Analyze impact of changing a symbol | `symbolName: "global::MyApp.Weapon.Fire"` |
| `pop_symbols` | List symbols by kind and name filter | `kind: "Class", name: "Controller"` |

### Building MCP Server

```bash
dotnet publish src/NexusCode.Mcp -c Release -o src/NexusCode.Mcp/publish
```

---

## NexusGraph (Web Frontend)

### Setup

```bash
cd NexusGraph
npm install
npm run dev
```

### Pages

| Page | URL | Features |
|------|-----|----------|
| Dashboard | `/` | Index repos, view stats |
| Graph | `/graph` | Interactive graph, filters, search |
| Symbols | `/symbols` | Browse symbols, callers/callees |
| Search | `/search` | Symbol search |
| Graph RAG | `/rag` | Ask questions about code |
| About | `/about` | Project info |

### Graph Features

- **Node Types:** Class (blue), Interface (purple), Method (green), Property (yellow), Field (red)
- **Edge Types:** Contains, Calls, Inherits, Implements, Overrides
- **Filters:** Toggle node/edge visibility in sidebar
- **Search:** Highlight matching nodes
- **Click:** View node details
- **Fit to Screen:** Reset zoom with ⊡ button

---

## Troubleshooting

### "No compilation found"
Ensure the project has a valid `.csproj` file with `<TargetFramework>`.

### "Symbols: 0"
Clean and rebuild:
```bash
dotnet clean
dotnet build
dotnet run --project src/NexusCode.Indexer -- "path"
```

### MCP Server timeout
1. Build the MCP server first:
```bash
dotnet publish src/NexusCode.Mcp -c Release -o src/NexusCode.Mcp/publish
```
2. Use the published .exe in config:
```json
"command": ["D:\\NexusCode\\src\\NexusCode.Mcp\\publish\\NexusCode.Mcp.exe"]
```

### NexusGraph not loading
1. Ensure API is running on port 5000
2. Check browser console for errors
3. Verify proxy config in `vite.config.ts`

### Large project performance
For projects with 10K+ files:
```bash
dotnet run --project src/NexusCode.Indexer -- "path" --parallelism 8
```

---

## System Requirements

- .NET 10 SDK
- Node.js 18+ (for NexusGraph)
- Ollama (optional, for embeddings)
- Qdrant (optional, for vector storage)
