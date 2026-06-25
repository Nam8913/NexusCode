# Nexus Code Intelligence Platform - Requirements

## 1. Functional Requirements

### FR-1: Repository Management
- FR-1.1: Clone and index local/remote Git repositories
- FR-1.2: Support single-repo and multi-repo intelligence
- FR-1.3: Track repository metadata (name, root path, language, size)
- FR-1.4: Support .sln and .csproj project structures
- FR-1.5: Handle monorepo with multiple solutions

### FR-2: Roslyn Analysis Engine
- FR-2.1: Parse C# source files into Syntax Trees
- FR-2.2: Build Semantic Models with full type resolution
- FR-2.3: Resolve all symbol references across the compilation
- FR-2.4: Track NuGet package dependencies
- FR-2.5: Resolve generic type parameters and constraints
- FR-2.6: Parse XML documentation comments
- FR-2.7: Detect attributes and their arguments
- FR-2.8: Support partial classes and extension methods

### FR-3: Knowledge Graph
- FR-3.1: Construct typed node graph from Roslyn analysis
- FR-3.2: Create 15+ edge types (CONTAINS, CALLS, USES, etc.)
- FR-3.3: Support bidirectional graph traversal
- FR-3.4: Persist graph to PostgreSQL/SQLite
- FR-3.5: Support incremental graph updates
- FR-3.6: Query engine with pattern matching
- FR-3.7: Subgraph extraction for context building

### FR-4: Symbol Search Engine
- FR-4.1: Find symbol by name, qualified name, or partial match
- FR-4.2: Find all references to a symbol
- FR-4.3: Find callers and callees of methods
- FR-4.4: Find interface implementations and override chains
- FR-4.5: Find derived types (inheritance hierarchy)
- FR-4.6: Search by symbol kind (class, method, property, etc.)
- FR-4.7: Fuzzy symbol matching

### FR-5: MCP Server
- FR-5.1: Implement MCP protocol (JSON-RPC over stdio)
- FR-5.2: Expose 10+ tools (find_symbol, search_graph, etc.)
- FR-5.3: Tool input/output schema validation
- FR-5.4: Streaming responses for large results
- FR-5.5: Resource exposure (symbols, graphs, contexts)
- FR-5.6: Prompt templates for AI agents

### FR-6: Embedding Engine
- FR-6.1: Generate embeddings via Ollama (nomic-embed-text, mxbai-embed-large, bge-m3)
- FR-6.2: Async queue-based processing
- FR-6.3: Batch embedding with configurable batch size
- FR-6.4: Embedding cache with hash-based invalidation
- FR-6.5: Support multiple embedding models simultaneously
- FR-6.6: Embedding versioning for model upgrades

### FR-7: Vector Store (Qdrant/LanceDB)
- FR-7.1: Store and index embeddings with metadata
- FR-7.2: Similarity search (cosine, dot product, Euclidean)
- FR-7.3: Filter by repository, project, namespace, type
- FR-7.4: Hybrid search (vector + metadata filters)
- FR-7.5: Payload-based filtering

### FR-8: Context Builder
- FR-8.1: Accept natural language questions
- FR-8.2: Symbol search → Graph expansion → Dependency expansion
- FR-8.3: Context window management with token counting
- FR-8.4: Relevance scoring and ranking
- FR-8.5: Context compression for token limits
- FR-8.6: Generate structured prompts for LLMs

### FR-9: Graph RAG
- FR-9.1: Combine symbol search + vector search + graph expansion
- FR-9.2: Multi-hop reasoning through graph traversal
- FR-9.3: Evidence collection from graph paths
- FR-9.4: Context aggregation and deduplication
- FR-9.5: Structured answer generation

### FR-10: Unity Intelligence Layer
- FR-10.1: Detect MonoBehaviour, ScriptableObject, Editor scripts
- FR-10.2: Parse SerializeField, RequireComponent, AddComponentMenu
- FR-10.3: Analyze Addressable assets references
- FR-10.4: Track Unity Events connections
- FR-10.5: Parse Assembly Definition files (.asmdef)
- FR-10.6: Build Unity-specific graph (prefab → component → script)
- FR-10.7: Scene hierarchy analysis

### FR-11: Repository Scanner
- FR-11.1: Scan 10K-1M+ files efficiently
- FR-11.2: Incremental indexing (only re-analyze changed files)
- FR-11.3: SHA256 hash tracking for change detection
- FR-11.4: File system watchers for real-time updates
- FR-11.5: Parallel file processing
- FR-11.6: Progress reporting and resumable indexing

### FR-12: Visualization
- FR-12.1: Call graph visualization
- FR-12.2: Dependency graph visualization
- FR-12.3: Namespace/type hierarchy graph
- FR-12.4: Architecture overview graph
- FR-12.5: Unity component graph
- FR-12.6: Export to Mermaid format
- FR-12.7: Interactive graph (zoom, pan, filter)

### FR-13: Agent Integration
- FR-13.1: Ollama integration (primary)
- FR-13.2: OpenAI API adapter
- FR-13.3: Claude API adapter
- FR-13.4: Gemini API adapter
- FR-13.5: OpenRouter adapter
- FR-13.6: Unified completion interface

### FR-14: API Layer
- FR-14.1: REST API for all operations
- FR-14.2: WebSocket for real-time updates
- FR-14.3: OpenAPI specification
- FR-14.4: Authentication and authorization
- FR-14.5: Rate limiting

---

## 2. Non-Functional Requirements

### NFR-1: Local-First
- All core features work without internet connection
- AI models run via Ollama locally
- Data stored locally (PostgreSQL/SQLite)
- Optional cloud sync

### NFR-2: Modularity
- Each component is a separate .NET project
- Components communicate via well-defined interfaces
- Dependency injection throughout
- Plugin architecture for extensibility

### NFR-3: Testability
- Unit tests for all core logic
- Integration tests for Roslyn analysis
- End-to-end tests for MCP tools
- Test fixtures with sample repositories

### NFR-4: Observability
- Structured logging (Serilog)
- Metrics collection (OpenTelemetry)
- Distributed tracing
- Health checks

### NFR-5: Configuration
- appsettings.json based configuration
- Environment variable overrides
- Per-repository configuration files
- CLI argument support

### NFR-6: Documentation
- XML documentation on all public APIs
- Architecture decision records (ADRs)
- API documentation (OpenAPI)
- User guides

---

## 3. Scalability Requirements

### SCR-1: Repository Size
- Tier 1: 10K files - responsive (< 5s search)
- Tier 2: 100K files - acceptable (< 30s search)
- Tier 3: 1M+ files - functional (< 120s search)

### SCR-2: Concurrent Users
- Support 10+ concurrent MCP sessions
- Support 5+ concurrent indexing jobs
- Thread-safe graph operations

### SCR-3: Memory Management
- Streaming analysis for large files
- Bounded memory for graph operations
- LRU cache for frequently accessed symbols
- Configurable memory limits

### SCR-4: Storage
- Graph: O(N) where N = number of symbols
- Embeddings: O(M) where M = number of chunks
- Indexes: O(N) for fast lookup
- WAL for crash recovery

---

## 4. Performance Requirements

### PR-1: Indexing Speed
- Parse 1000 C# files/second (Tier 1)
- Build graph for 10K files in < 60 seconds
- Incremental update for single file change in < 2 seconds

### PR-2: Search Latency
- Symbol lookup: < 50ms (p95)
- Reference search: < 200ms (p95)
- Call graph traversal (3 hops): < 500ms (p95)
- Full-text search: < 500ms (p95)

### PR-3: MCP Tool Latency
- find_symbol: < 100ms
- find_references: < 300ms
- search_graph: < 500ms
- build_context: < 2s
- explain_architecture: < 5s

### PR-4: Context Generation
- Symbol-based context: < 500ms
- Graph RAG context: < 3s
- Context compression: < 1s

### PR-5: Embedding Generation
- Single embedding: < 500ms (Ollama)
- Batch of 100: < 10s
- Queue processing: 1000 embeddings/minute

---

## 5. Security Requirements

### SR-1: Data Protection
- No source code leaves local machine (default)
- Encrypted storage for sensitive configurations
- No telemetry without explicit consent
- Secure credential storage

### SR-2: Access Control
- Repository-level access permissions
- API key management for external services
- Role-based access control (optional)
- Audit logging for sensitive operations

### SR-3: Input Validation
- Sanitize all user inputs
- Validate MCP tool parameters
- Limit query complexity (max graph depth)
- Rate limiting on API endpoints

### SR-4: Dependency Security
- Regular dependency updates
- Vulnerability scanning
- Minimal dependency surface
- No hardcoded secrets

### SR-5: Network Security
- HTTPS for all external communications
- Certificate pinning for Ollama connections
- Proxy support
- Firewall-friendly (single port)
