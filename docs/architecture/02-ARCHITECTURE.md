# Nexus Code Intelligence Platform - Architecture Diagrams

## 1. Context Diagram

```mermaid
C4Context
    title Nexus Code Intelligence Platform - System Context

    Person(dev, "Developer", "C# / Unity developer using AI coding assistant")
    Person(ai, "AI Agent", "LLM-based coding assistant via MCP")

    System(nexus, "Nexus Code Intelligence Platform", "Roslyn-based code intelligence with Knowledge Graph, Symbol Search, MCP, and Graph RAG")

    System_Ext(repo, "Git Repository", "Source code repository to analyze")
    System_Ext(ollama, "Ollama", "Local LLM and embedding server")
    System_Ext(qdrant, "Qdrant", "Vector database for embeddings")
    System_Ext(postgres, "PostgreSQL", "Graph and metadata storage")
    System_Ext(ide, "IDE / Editor", "VS Code, Rider, Visual Studio")

    Rel(dev, nexus, "Queries codebase")
    Rel(ai, nexus, "Uses MCP tools")
    Rel(nexus, repo, "Reads source code")
    Rel(nexus, ollama, "Generates embeddings / completions")
    Rel(nexus, qdrant, "Stores / searches embeddings")
    Rel(nexus, postgres, "Stores graph / metadata")
    Rel(ide, nexus, "MCP protocol")
```

## 2. Container Diagram

```mermaid
C4Container
    title Nexus Code Intelligence Platform - Container Diagram

    Person(dev, "Developer")
    Person(ai, "AI Agent")

    Container_Boundary(nexus, "Nexus Code Intelligence Platform") {
        Container(api, "API Server", "ASP.NET Core", "REST API + WebSocket + MCP Server")
        Container(roslyn, "Roslyn Engine", ".NET 10", "Syntax/Semantic analysis, Symbol resolution")
        Container(graph, "Knowledge Graph Engine", ".NET 10", "Graph construction, traversal, querying")
        Container(symbols, "Symbol Search Engine", ".NET 10", "Symbol lookup, reference tracking")
        Container(indexer, "Repository Scanner", ".NET 10", "File scanning, incremental indexing")
        Container(embedding, "Embedding Engine", ".NET 10", "Embedding generation, queue management")
        Container(vector, "Vector Store Adapter", ".NET 10", "Qdrant/LanceDB abstraction")
        Container(context, "Context Builder", ".NET 10", "Graph RAG, prompt generation")
        Container(unity, "Unity Intelligence", ".NET 10", "Unity-specific analysis")
        Container(ui, "Web UI", "Blazor", "Visualization, dashboard")
    }

    System_Ext(repo, "Git Repos")
    System_Ext(ollama, "Ollama")
    System_Ext(qdrant, "Qdrant / LanceDB")
    System_Ext(db, "PostgreSQL / SQLite")

    Rel(dev, api, "REST API")
    Rel(ai, api, "MCP (JSON-RPC)")
    Rel(api, roslyn, "Analyze code")
    Rel(api, graph, "Query graph")
    Rel(api, symbols, "Search symbols")
    Rel(api, context, "Build context")
    Rel(roslyn, graph, "Populates graph")
    Rel(roslyn, symbols, "Populates symbol table")
    Rel(indexer, roslyn, "Triggers analysis")
    Rel(indexer, repo, "Reads files")
    Rel(graph, db, "Persists graph")
    Rel(symbols, db, "Persists symbols")
    Rel(embedding, ollama, "Generates embeddings")
    Rel(embedding, vector, "Stores embeddings")
    Rel(context, graph, "Traverses graph")
    Rel(context, symbols, "Looks up symbols")
    Rel(context, vector, "Vector search")
    Rel(unity, roslyn, "Extends analysis")
    Rel(ui, api, "REST/WS")
```

## 3. Component Diagram - Roslyn Analysis Engine

```mermaid
graph TB
    subgraph "Roslyn Analysis Engine"
        subgraph "Input Processing"
            FS[File Scanner]
            W[Workspace Builder]
            CP[Compilation Builder]
        end

        subgraph "Syntax Analysis"
            ST[Syntax Tree Parser]
            SD[Syntax Depth Analyzer]
            AT[Attribute Extractor]
        end

        subgraph "Semantic Analysis"
            SM[Semantic Model Builder]
            SR[Symbol Resolver]
            TR[Type Resolver]
            GR[Generic Resolver]
            IR[Inheritance Resolver]
        end

        subgraph "Symbol Processing"
            STE[Symbol Table Builder]
            DI[Document Indexer]
            XE[XML Doc Extractor]
        end

        subgraph "Graph Population"
            GN[Node Creator]
            GE[Edge Creator]
            GP[Graph Populator]
        end

        FS --> W --> CP
        CP --> ST --> SD
        CP --> SM
        SM --> SR --> TR --> GR --> IR
        ST --> AT
        SR --> STE
        SM --> DI
        ST --> XE
        STE --> GN
        SR --> GE
        GN --> GP
        GE --> GP
    end

    Input["Source Files"] --> FS
    GP --> Output["Knowledge Graph + Symbol Table"]
```

## 4. Component Diagram - Knowledge Graph Engine

```mermaid
graph TB
    subgraph "Knowledge Graph Engine"
        subgraph "Graph Construction"
            NC[Node Creator]
            EC[Edge Creator]
            GC[Graph Builder]
        end

        subgraph "Graph Storage"
            GM[Graph Mapper]
            GP[Graph Persistence]
            GL[Graph Loader]
        end

        subgraph "Graph Query Engine"
            GE[Graph Traverser]
            GPAT[Pattern Matcher]
            GS[Subgraph Extractor]
            GSC[Graph Scorer]
        end

        subgraph "Graph Operations"
            GF[Find Path]
            GBFS[BFS Traversal]
            GDFS[DFS Traversal]
            GCYCLE[Cycle Detector]
        end

        NC --> GC
        EC --> GC
        GC --> GM --> GP
        GL --> GM

        GE --> GPAT
        GE --> GS
        GE --> GSC
        GF --> GE
        GBFS --> GE
        GDFS --> GE
        GCYCLE --> GE
    end

    Roslyn["Roslyn Engine"] --> NC
    Roslyn --> EC
    GP --> DB[PostgreSQL/SQLite]
    DB --> GL
```

## 5. Component Diagram - MCP Server

```mermaid
graph TB
    subgraph "MCP Server"
        subgraph "Protocol Layer"
            MR[Message Router]
            VD[Version Negotiator]
            CP[Capability Provider]
        end

        subgraph "Tool Registry"
            TR[Tool Registry]
            TI[Tool Input Validator]
            TO[Tool Output Formatter]
        end

        subgraph "MCP Tools"
            T1[find_symbol]
            T2[find_references]
            T3[find_callers]
            T4[find_callees]
            T5[find_dependencies]
            T6[search_code]
            T7[search_graph]
            T8[build_context]
            T9[explain_architecture]
            T10[trace_execution_flow]
        end

        subgraph "Resource Provider"
            RP[Resource Manager]
            RS[Symbol Resources]
            RG[Graph Resources]
        end

        subgraph "Prompt Templates"
            PP[Prompt Provider]
            PT[Template Engine]
        end

        MR --> VD --> CP
        MR --> TR
        TR --> TI --> T1 & T2 & T3 & T4 & T5 & T6 & T7 & T8 & T9 & T10
        T1 & T2 & T3 & T4 & T5 & T6 & T7 & T8 & T9 & T10 --> TO

        T1 --> SymbolSearch[Symbol Search Engine]
        T2 --> SymbolSearch
        T3 --> Graph[Knowledge Graph]
        T4 --> Graph
        T5 --> Graph
        T6 --> Search[Search Engine]
        T7 --> Graph
        T8 --> ContextBuilder[Context Builder]
        T9 --> Graph
        T10 --> Graph

        MR --> RP --> RS & RG
        MR --> PP --> PT
    end

    AI["AI Agent"] -->|JSON-RPC| MR
```

## 6. Data Flow Diagram - Complete Pipeline

```mermaid
graph LR
    subgraph "Indexing Pipeline"
        A[Git Repository] -->|Clone/Pull| B[File Scanner]
        B -->|File List| C[Change Detector]
        C -->|New/Modified Files| D[Roslyn Analyzer]
        D -->|Syntax + Semantic| E[Symbol Table Builder]
        D -->|Symbol Nodes + Edges| F[Knowledge Graph Builder]
        D -->|Chunks| G[Embedding Generator]
        E -->|Symbols| H[(PostgreSQL)]
        F -->|Graph| H
        G -->|Vectors| I[(Qdrant)]
    end

    subgraph "Query Pipeline"
        J[AI Agent / User] -->|Question| K[Query Parser]
        K --> L[Symbol Search]
        L -->|Symbols| M[Graph Expansion]
        L -->|Vectors| N[Vector Search]
        M -->|Subgraph| O[Context Aggregator]
        N -->|Similar Code| O
        O -->|Context| P[Context Compressor]
        P -->|Compressed| Q[Prompt Builder]
        Q -->|Prompt| R[LLM via Ollama]
        R -->|Answer| J
    end

    E -.->|Symbol lookup| L
    H -.->|Graph traversal| M
    I -.->|Similarity search| N
```

## 7. Data Flow Diagram - Incremental Indexing

```mermaid
sequenceDiagram
    participant FW as File Watcher
    participant CD as Change Detector
    participant Q as Index Queue
    participant RA as Roslyn Analyzer
    participant ST as Symbol Table
    participant KG as Knowledge Graph
    participant EE as Embedding Engine
    participant VS as Vector Store

    FW->>CD: File changed event
    CD->>CD: Compute SHA256 hash
    CD->>CD: Compare with stored hash
    alt File changed
        CD->>Q: Enqueue for re-analysis
    else File deleted
        CD->>ST: Remove symbols
        CD->>KG: Remove nodes/edges
        CD->>VS: Remove embeddings
    else File unchanged
        CD-->>FW: Skip
    end

    Q->>RA: Analyze file
    RA->>ST: Update symbol table
    RA->>KG: Update graph nodes/edges
    RA->>EE: Queue new chunks
    EE->>VS: Store embeddings
    ST->>ST: Mark indexing complete
```

## 8. Data Flow Diagram - MCP Tool Execution

```mermaid
sequenceDiagram
    participant AI as AI Agent
    participant MCP as MCP Server
    participant TR as Tool Router
    participant TS as Tool Service
    participant SS as Symbol Search
    participant KG as Knowledge Graph
    participant CB as Context Builder

    AI->>MCP: tools/call {name: "find_references", arguments: {...}}
    MCP->>TR: Route to tool
    TR->>TR: Validate input schema
    TR->>TS: Execute find_references
    TS->>SS: Lookup symbol by name
    SS-->>TS: Symbol + ID
    TS->>KG: Traverse REFERENCES edges
    KG-->>TS: Reference list
    TS->>TS: Format results
    TS-->>MCP: Tool result
    MCP-->>AI: {content: [...], isError: false}
```

## 9. Deployment Architecture

```mermaid
graph TB
    subgraph "Local Machine"
        subgraph "Nexus Platform"
            API[ASP.NET Core API]
            MCP[MCP Server]
            UI[Blazor UI]
        end

        subgraph "Analysis Layer"
            ROSLYN[Roslyn Engine]
            GRAPH[Knowledge Graph]
            SYMBOLS[Symbol Search]
            INDEXER[Repository Scanner]
            UNITY[Unity Intelligence]
        end

        subgraph "Storage Layer"
            PG[(PostgreSQL)]
            SQLITE[(SQLite)]
            QDRANT[(Qdrant)]
        end

        subgraph "AI Layer"
            OLLAMA[Ollama Server]
        end

        subgraph "Vector Layer"
            EMB[Embedding Engine]
        end
    end

    API --> MCP
    API --> ROSLYN
    API --> GRAPH
    API --> SYMBOLS
    API --> UNITY
    INDEXER --> ROSLYN
    INDEXER --> GRAPH
    INDEXER --> SYMBOLS
    INDEXER --> EMB
    GRAPH --> PG
    GRAPH --> SQLITE
    SYMBOLS --> PG
    SYMBOLS --> SQLITE
    EMB --> OLLAMA
    EMB --> QDRANT
    API --> UI
```

## 10. Layered Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        UI[Blazor UI]
        API[REST API]
        MCP[MCP Server]
    end

    subgraph "Application Layer"
        CTX[Context Builder]
        RAG[Graph RAG]
        AGENT[Agent Integration]
    end

    subgraph "Domain Layer"
        KG[Knowledge Graph]
        SS[Symbol Search]
        VE[Visualization Engine]
    end

    subgraph "Infrastructure Layer"
        ROSLYN[Roslyn Engine]
        EMB[Embedding Engine]
        VS[Vector Store]
        DB[Database]
        FS[File System]
    end

    subgraph "Core Layer"
        DOMAIN[Domain Models]
        EVENTS[Domain Events]
        INTERFACES[Core Interfaces]
    end

    UI --> CTX
    API --> CTX
    MCP --> CTX
    CTX --> KG
    CTX --> SS
    RAG --> KG
    RAG --> SS
    RAG --> VS
    AGENT --> CTX
    KG --> ROSLYN
    SS --> ROSLYN
    VE --> KG
    EMB --> DB
    VS --> DB
    ROSLYN --> FS
    DOMAIN --> INTERFACES
    EVENTS --> INTERFACES
```
