# Nexus Code Intelligence Platform

## Purpose

Roslyn-based code intelligence platform for C# and Unity projects with Knowledge Graph, Symbol Search, MCP Integration, and Graph RAG.

## Documentation Map

| Directory | Purpose |
|-----------|---------|
| `architecture/` | Technical design, ADRs, system architecture |
| `development/` | Setup guides, coding standards, build instructions |
| `features/` | Feature-specific documentation |
| `roadmap/` | Planning: current, backlog, completed |
| `reports/` | Progress logs, changelogs, known issues |

## Quick Links

- [Architecture Overview](architecture/overview.md)
- [System Design](architecture/system-design.md)
- [API Design](architecture/api-design.md)
- [Development Setup](development/setup.md)
- [Build Instructions](development/build.md)
- [Feature Index](features/feature-index.md)
- [Current Roadmap](roadmap/current.md)
- [Progress Log](reports/progress.md)

## Technology Stack

- **Backend**: .NET 10, C#, ASP.NET Core
- **Analysis**: Roslyn
- **Storage**: SQLite, In-Memory Graph
- **Vector DB**: Qdrant, InMemory
- **AI**: Ollama (nomic-embed-text, mxbai-embed-large, bge-m3)
- **Frontend**: NexusGraph (React + Sigma.js)
- **Protocol**: MCP (Model Context Protocol)
