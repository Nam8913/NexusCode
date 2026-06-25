# Embedding Engine Feature

## Purpose

Vector embedding generation via Ollama for semantic search.

## Design

### Components
- OllamaClient: HTTP client for Ollama API
- EmbeddingEngine: Embedding generation with caching
- BatchEmbeddingQueue: Concurrent queue for batch processing
- IVectorStore: Interface for Qdrant/InMemory adapters

### Supported Models
- nomic-embed-text (768 dimensions)
- mxbai-embed-large (1024 dimensions)
- bge-m3 (1024 dimensions)

## Current Status

✅ Complete - Qdrant + InMemory adapters
