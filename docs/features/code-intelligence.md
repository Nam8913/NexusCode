# Code Intelligence Feature

## Purpose

Roslyn-based semantic analysis of C# code, extracting symbols and building Knowledge Graph.

## Requirements

- Parse C# source files into Syntax Trees
- Build Semantic Models with full type resolution
- Resolve all symbol references across compilation
- Track NuGet package dependencies
- Support generic types, inheritance, interfaces
- Incremental indexing with hash-based change detection

## Design

### RoslynEngine
- Loads solutions and projects via MSBuildWorkspace alternative
- Builds CSharpCompilation directly from .csproj
- Analyzes files using SyntaxWalker and SemanticModel

### SyntaxWalker
- Visits all syntax nodes (Class, Method, Property, Field, Event)
- Extracts symbol information with full metadata
- Tracks references between symbols

### SymbolTable
- ConcurrentDictionary-based multi-index storage
- Lookup by: ID, FullName, Name, Kind, File, Container
- Thread-safe for parallel indexing

## Public APIs

```csharp
// RoslynEngine
SolutionInfo LoadSolutionAsync(string path)
ProjectInfo LoadProjectAsync(string path)
Compilation BuildCompilation(string projectPath, string[] references)
AnalysisResult AnalyzeFile(string filePath, Compilation compilation)

// SymbolTable
void Add(SymbolEntity symbol)
SymbolEntity? GetByFullName(string fullName)
IReadOnlyList<SymbolEntity> GetByName(string name)
IReadOnlyList<SymbolEntity> GetByKind(SymbolKind kind)
```

## Data Structures

- `SymbolEntity`: Full symbol metadata
- `ReferenceEntity`: Symbol reference tracking
- `AnalysisResult`: Per-file analysis output

## Current Status

✅ Complete - All C# symbol types supported

## Future Work

- Support for C# 13+ features
- Better error recovery for partial compilations
