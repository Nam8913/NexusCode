# Nexus Code Intelligence Platform - Developer Guide

## Architecture Overview

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

### Projects

| Project | Purpose |
|---------|---------|
| `NexusCode.Domain` | Entities, Enums, Interfaces |
| `NexusCode.Roslyn` | Core analysis engine |
| `NexusCode.Indexer` | CLI indexing tool |
| `NexusCode.Mcp` | MCP Server (10 tools) |
| `NexusCode.Embedding` | Ollama embedding |
| `NexusCode.VectorStore` | Qdrant/InMemory adapters |
| `NexusCode.Context` | Context builder |
| `NexusCode.Api` | REST API |
| `NexusCode.Database` | SQLite persistence |
| `NexusCode.UI` | Blazor frontend |
| `NexusCode.Tests` | Unit + integration tests |

---

## Development Setup

### Prerequisites
- .NET 10 SDK
- Ollama (optional, for embeddings)
- Qdrant (optional, for vector storage)

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run API
```bash
dotnet run --project src/NexusCode.Api
```

### Run UI
```bash
dotnet run --project src/NexusCode.UI
```

---

## Adding New Features

### 1. Create new file in appropriate project
### 2. Add using directives
### 3. Write unit tests
### 4. Update documentation

---

## Testing

### Unit Tests
```bash
dotnet test src/NexusCode.Tests
```

### Integration Tests
Tests in `NexusCode.Tests/Integration/` test full workflows.

### Test Fixtures
Use `SampleRepository.CreateTempRepo()` for test repositories.

---

## Code Style

- Use C# 12 features where appropriate
- Follow existing naming conventions
- Add XML documentation for public APIs
- Keep methods focused and small

---

## Key Classes

### SymbolTable
Multi-index symbol storage with lookup by name, kind, file.

### KnowledgeGraph
In-memory graph with adjacency lists for fast traversal.

### SymbolSearchEngine
Symbol search with fuzzy matching via trigram index.

### RoslynEngine
Roslyn-based code analysis without MSBuild dependency.

---

## Contributing

1. Fork the repository
2. Create feature branch
3. Make changes
4. Add tests
5. Run `dotnet test`
6. Submit pull request
