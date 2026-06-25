# Nexus Code Intelligence Platform - Architecture Index

## Document Structure

| # | Document | Description |
|---|----------|-------------|
| 01 | [Requirements](01-REQUIREMENTS.md) | Functional, Non-Functional, Scalability, Performance, Security Requirements |
| 02 | [Architecture Diagrams](02-ARCHITECTURE.md) | Context, Container, Component, Data Flow Diagrams (Mermaid) |
| 03 | [Domain Model](03-DOMAIN-MODEL.md) | Entity definitions, fields, relationships, lifecycle |
| 04 | [Roslyn Engine](04-ROSLYN-ENGINE.md) | Syntax/Semantic analysis, Symbol resolution, Incremental analysis |
| 05 | [Knowledge Graph](05-KNOWLEDGE-GRAPH.md) | Graph schema, Storage, Traversal, Query engine |
| 06 | [Symbol Search](06-SYMBOL-SEARCH.md) | Symbol lookup, References, Callers/Callees, Implementations |
| 07 | [Repository Scanner](07-REPOSITORY-SCANNER.md) | File discovery, Change detection, Parallel processing |
| 08 | [Unity Intelligence](08-UNITY-INTELLIGENCE.md) | MonoBehaviour, Serialization, Assembly definitions |
| 09 | [MCP Server](09-MCP-SERVER.md) | Protocol, Tools, Resources, Prompts |
| 10 | [Context Builder](10-CONTEXT-BUILDER.md) | Question parsing, Graph expansion, Compression |
| 11 | [Embedding Engine](11-EMBEDDING-ENGINE.md) | Ollama integration, Queue, Batching, Caching |
| 12 | [Chunking/Qdrant/GraphRAG](12-CHUNKING-QDRANT-GRAPHRAG.md) | Chunking strategy, Vector store, Graph RAG pipeline |
| 15-17 | [Agent/Database/Visualization](15-17-AGENT-DATABASE-VISUALIZATION.md) | Agent adapters, SQL schema, Graph visualization |
| 18-20 | [Monitoring/Projects/Roadmap](18-20-MONITORING-PROJECTS-ROADMAP.md) | Metrics, Project structure, Development roadmap |
| 99 | [Audit](99-AUDIT.md) | Architecture audit, Bottlenecks, Comparison, Recommendations |

## Architecture Overview

```
Code → Roslyn Analysis → Knowledge Graph → Symbol Search → MCP → Embeddings → Graph RAG
```

## Key Design Principles

1. **Graph First**: Knowledge Graph is the central component
2. **Roslyn First**: Deep semantic understanding via Roslyn
3. **Symbol First**: Symbol-based navigation and search
4. **MCP First**: Native MCP protocol for AI integration
5. **Embeddings Second**: Supplementary semantic search
6. **Graph RAG Third**: Combined graph + vector for best results

## Technology Stack

- **Backend**: .NET 10, C#, ASP.NET Core
- **Analysis**: Roslyn
- **Storage**: PostgreSQL, SQLite
- **Vector DB**: Qdrant, LanceDB
- **AI**: Ollama (nomic-embed-text, mxbai-embed-large, bge-m3)
- **Frontend**: Blazor
- **Graph**: Custom Graph Engine
