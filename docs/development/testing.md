# Testing

## Quick Start

```bash
dotnet test NexusCode.slnx
```

**Current: 64/64 tests passing** (0 failures, 0 skipped)

---

## Test Suite Overview

| Test File | Tests | Category |
|-----------|-------|----------|
| `SymbolTableTests.cs` | 9 | Unit — SymbolTable CRUD, indexing, references |
| `SymbolTableResolveTests.cs` | 12 | Unit — Fuzzy symbol resolution, ReferenceEntity.FilePath |
| `SearchSourceTextTests.cs` | 11 | Unit — Source code text search with temp files |
| `KnowledgeGraphTests.cs` | 6 | Unit — Graph nodes, edges, incoming/outgoing traversal |
| `InMemoryVectorStoreTests.cs` | 6 | Unit — Vector store insert, search, upsert |
| `MultiRepoTests.cs` | 5 | Unit — Multi-repo manager, cross-repo search |
| `IndexerIntegrationTests.cs` | 5 | Integration — Full indexing pipeline on temp repos |
| `SearchIntegrationTests.cs` | 4 | Integration — Cross-repo symbol search |
| `ApiIntegrationTests.cs` | 7 | Integration — REST API endpoints |
| **Total** | **64** | |

---

## Unit Tests

### SymbolTableTests (`NexusCode.Tests/SymbolTableTests.cs`)

Tests core SymbolTable CRUD and indexing behavior.

| Test | What it verifies |
|------|------------------|
| `Add_And_GetById_ReturnsSymbol` | Symbol added by ID can be retrieved |
| `GetByFullName_ReturnsCorrectSymbol` | Lookup by full qualified name returns correct symbol |
| `GetByName_ReturnsMultipleSymbols` | Multiple symbols with same short name returned |
| `GetByKind_FiltersCorrectly` | Type/Method/Property filtering works |
| `Remove_DeletesSymbol` | Removed symbol no longer retrievable |
| `Update_ModifiesSymbol` | In-place update persists |
| `AddReference_And_GetReferences_Works` | References stored and retrievable by SymbolId |
| `Count_ReturnsCorrectNumber` | Count property matches actual symbol count |
| `AddReference_WithFilePath_StoresFilePath` | **[NEW]** ReferenceEntity FilePath persisted |

### SymbolTableResolveTests (`NexusCode.Tests/SymbolTableResolveTests.cs`)

Tests the fuzzy symbol resolution logic in `SymbolTable.ResolveSymbol()` and `ReferenceEntity` FilePath field.

| Test | What it verifies |
|------|------------------|
| `ResolveSymbol_ExactFullName_ReturnsSymbol` | Exact `global::X` full name match |
| `ResolveSymbol_ShortName_ReturnsExactNameMatch` | Short name matches exact Name key |
| `ResolveSymbol_SingleSymbol_ReturnsIt` | Single match returns immediately |
| `ResolveSymbol_CaseInsensitive_ReturnsMatch` | `"gameservice"` matches `global::GameService` |
| `ResolveSymbol_NoMatch_ReturnsNull` | Unknown name returns null |
| `ResolveSymbol_EmptyTable_ReturnsNull` | Empty SymbolTable returns null |
| `ResolveSymbol_PrefixFallback_ReturnsSymbol` | `"SuperLong"` prefix matches `SuperLongClassName` |
| `ResolveSymbol_ContainsFallback_ReturnsSymbol` | `"Inventory"` substring matches `InventoryViewController` |
| `ResolveSymbol_MultipleByKind_PrefersTypeOverMethod` | Same-name Type wins over Method |
| `ReferenceEntity_FilePath_CanBeSet` | **[NEW, regression]** FilePath field set/get works |
| `ReferenceEntity_FilePath_NullByDefault` | **[NEW, regression]** FilePath defaults to null (not GUID) |
| `AddReference_WithFilePath_StoresFilePath` | **[NEW, regression]** Reference with FilePath round-trips through SymbolTable |

**Regression coverage for the GUID file path bug:** The three `ReferenceEntity` tests directly verify that:
1. `FilePath` is a settable string property (not `Guid.Empty`)
2. `FilePath` defaults to null, not a GUID
3. Storing a reference with a file path preserves it through retrieval

### SearchSourceTextTests (`NexusCode.Tests/SearchSourceTextTests.cs`)

Tests `SymbolSearchEngine.SearchSourceText()` — the source code text search added in v2.5.0. Uses real temp files created in constructor and cleaned up in `Dispose()`.

| Test | What it verifies |
|------|------------------|
| `SearchSourceText_FindsMatchingContent` | Finds "Initialize" in source file, correct line number |
| `SearchSourceText_CaseInsensitive` | "MYFIELD" matches lowercase source text |
| `SearchSourceText_EmptyQuery_ReturnsEmpty` | Empty string returns empty list |
| `SearchSourceText_NullQuery_ReturnsEmpty` | Null string returns empty list (no exception) |
| `SearchSourceText_NoMatch_ReturnsEmpty` | Non-existent query returns empty list |
| `SearchSourceText_MultipleMatches_ReturnsAll` | `": A"` finds both inheritance references |
| `SearchSourceText_MaxResultsLimitsOutput` | `maxResults=5` caps output at 5 items |
| `SearchSourceText_ScoreHigherForStartOfLine` | Line starting with query gets Score 1.0 vs 0.8 |
| `SearchSourceText_NonexistentFile_SkipsGracefully` | Missing file doesn't crash, returns empty |
| `SearchSourceText_IncludesMethodSymbols` | Method symbols (not just Types) trigger file reads |

**Edge cases covered:** null input, empty input, missing files, boundary limits, scoring differentiation.

### KnowledgeGraphTests (`NexusCode.Tests/KnowledgeGraphTests.cs`)

| Test | What it verifies |
|------|------------------|
| `AddNode_And_GetNode_ReturnsNode` | Node add/get roundtrip |
| `AddEdge_CreatesRelationship` | Edge creation and outgoing traversal |
| `GetIncomingEdges_FindsCallers` | Incoming edge lookup for call graph |
| `RemoveNode_RemovesConnectedEdges` | Node deletion cascades to edges |
| `GetNodesByKind_FiltersCorrectly` | Kind-based filtering |
| `NodeCount_And_EdgeCount_AreCorrect` | Counter properties match actual counts |

### InMemoryVectorStoreTests (`NexusCode.Tests/InMemoryVectorStoreTests.cs`)

| Test | What it verifies |
|------|------------------|
| Basic insert/search/upsert/delete operations | Vector store CRUD |
| Similarity search returns ranked results | Cosine similarity ranking |

### MultiRepoTests (`NexusCode.Tests/MultiRepoTests.cs`)

| Test | What it verifies |
|------|------------------|
| Add/remove/list repositories | Multi-repo manager lifecycle |
| Cross-repo search merges results | Search across indexed repos |

---

## Integration Tests

### IndexerIntegrationTests (`NexusCode.Tests/Integration/IndexerIntegrationTests.cs`)

Full indexing pipeline on `SampleRepository.CreateTempRepo()`.

| Test | What it verifies |
|------|------------------|
| Index a C# project | Files parsed, symbols extracted |
| Knowledge graph populated | Nodes and edges created |
| SymbolTable contains expected symbols | Correct count and kinds |
| Edge kinds match expected relationships | Calls, Inherits, Implements, Declares |
| Cleanup temp repo after test | No disk pollution |

### SearchIntegrationTests (`NexusCode.Tests/Integration/SearchIntegrationTests.cs`)

Cross-repo symbol search.

| Test | What it verifies |
|------|------------------|
| Search finds indexed symbols | Query returns results |
| Cross-repo deduplication | Same symbol in multiple repos appears once |
| Search with kind filter | Type vs Method filtering |
| Empty query returns empty | Graceful handling |

### ApiIntegrationTests (`NexusCode.Tests/Integration/ApiIntegrationTests.cs`)

REST API endpoint tests.

| Test | What it verifies |
|------|------------------|
| GET /api/symbols returns list | Symbol listing endpoint |
| POST /api/index triggers indexing | Index trigger endpoint |
| GET /api/graph returns nodes | Graph export endpoint |
| POST /api/rag/ask returns answer | RAG endpoint |
| Error handling on bad input | 400/500 responses |

---

## Test Fixtures

### SampleRepository (`NexusCode.Tests/Fixtures/SampleRepository.cs`)

- `CreateTempRepo()` — Creates a temp directory with sample C# files for integration testing
- Automatically cleaned up after tests

---

## Known Issues

| Issue | Severity | Notes |
|-------|----------|-------|
| `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 vulnerability | Warning | Known CVE, does not affect tests |
| `SearchSourceText` tests depend on real filesystem | Low | Temp files cleaned via `IDisposable`, Windows paths |
| MCP handler `ResolveSymbolByName` not unit-tested | Low | Lives in top-level statements (`Program.cs`); integration-tested via MCP tool calls |

---

## Running Tests

```bash
# All tests
dotnet test NexusCode.slnx

# Specific test class
dotnet test --filter "FullyQualifiedName~SymbolTableResolveTests"

# Specific test
dotnet test --filter "ResolveSymbol_CaseInsensitive_ReturnsMatch"

# With detailed output
dotnet test --verbosity normal

# Build + test
dotnet test src/NexusCode.Tests/NexusCode.Tests.csproj
```

---

## Adding New Tests

1. Unit tests go in `src/NexusCode.Tests/<Feature>Tests.cs`
2. Integration tests go in `src/NexusCode.Tests/Integration/<Feature>Tests.cs`
3. Use `SampleRepository.CreateTempRepo()` for file-system dependent tests
4. Implement `IDisposable` for temp resource cleanup
5. Follow xUnit conventions: `[Fact]` for single tests, `[Theory]` + `[InlineData]` for parameterized
6. Assert on observable behavior, not implementation details
7. Run full suite before committing
