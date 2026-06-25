# API Design

## Base URL

```
http://localhost:5000
```

## Endpoints

### Index
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/index/repository | Index a repository |
| GET | /api/index/status | Get indexing status |

### Search
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/search/symbol | Search symbols |
| GET | /api/search/callers/{name} | Find callers |
| GET | /api/search/callees/{name} | Find callees |
| GET | /api/search/implementations/{name} | Find implementations |
| GET | /api/search/derived/{name} | Find derived types |

### Graph
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/graph/stats | Graph statistics |
| GET | /api/graph/export | Export full graph as JSON |
| GET | /api/graph/nodes/{kind} | Nodes by kind |
| GET | /api/graph/edges/{kind} | Edges by kind |
| GET | /api/graph/mermaid | Mermaid diagram |

### RAG
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/rag/ask | Graph RAG question |

### Multi-Repo
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/multi-repo/index | Index multiple repos |
| GET | /api/multi-repo/list | List repositories |
| GET | /api/multi-repo/search | Cross-repo search |
| GET | /api/multi-repo/compare | Compare repos |
| GET | /api/multi-repo/health/{name} | Repository health |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /health | Health check |
| GET | /swagger | Swagger UI |
