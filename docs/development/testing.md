# Testing

## Unit Tests

```bash
dotnet test NexusCode.slnx
```

Current: 42/42 tests passing

### Test Coverage

| Project | Tests |
|---------|-------|
| SymbolTable | 9 |
| KnowledgeGraph | 6 |
| InMemoryVectorStore | 6 |
| MultiRepo | 5 |
| Indexer Integration | 5 |
| Search Integration | 4 |
| API Integration | 7 |

## Integration Tests

Tests in `NexusCode.Tests/Integration/` test full workflows:

- IndexerIntegrationTests: Full indexing pipeline
- SearchIntegrationTests: Cross-repo search
- ApiIntegrationTests: API endpoints

## Test Fixtures

Use `SampleRepository.CreateTempRepo()` for test repositories.
