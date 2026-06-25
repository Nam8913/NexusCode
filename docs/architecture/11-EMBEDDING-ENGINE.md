# Nexus Code Intelligence Platform - Embedding Engine

## Overview

The Embedding Engine is a supplementary layer that generates vector embeddings for code chunks. It supports semantic search, related code discovery, and similar method detection. Embeddings are generated asynchronously via Ollama.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Embedding Engine                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Chunking   │  │   Embedding  │  │   Vector     │  │
│  │   Strategy   │→│   Generator  │→│   Store      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Embedding  │  │   Batch      │  │   Cache      │  │
│  │   Queue      │  │   Processor  │  │   Manager    │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Retry      │  │   Progress   │  │   Version    │  │
│  │   Handler    │  │   Reporter   │  │   Manager    │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Core Data Structures

```csharp
class EmbeddingJob
{
    Guid Id;
    Guid ChunkId;
    string Content;
    string Model;
    EmbeddingStatus Status;
    int RetryCount;
    DateTimeOffset CreatedAt;
    DateTimeOffset? StartedAt;
    DateTimeOffset? CompletedAt;
    string? ErrorMessage;
}

enum EmbeddingStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Retrying
}

class EmbeddingResult
{
    Guid ChunkId;
    float[] Vector;
    int Dimension;
    string Model;
    DateTimeOffset GeneratedAt;
}

class EmbeddingCache
{
    ConcurrentDictionary<string, EmbeddingResult> _cache;
    TimeSpan _expiration;
    
    GetOrAdd(contentHash: string, generator: Func<EmbeddingResult>):
        if _cache.TryGetValue(contentHash, out cached):
            if !IsExpired(cached):
                return cached
        
        result = generator()
        _cache[contentHash] = result
        return result
    
    IsExpired(result: EmbeddingResult):
        return DateTimeOffset.UtcNow - result.GeneratedAt > _expiration
}
```

---

## 3. Embedding Queue

```
class EmbeddingQueue
{
    ConcurrentQueue<EmbeddingJob> _pendingJobs;
    ConcurrentDictionary<Guid, EmbeddingJob> _activeJobs;
    List<EmbeddingJob> _completedJobs;
    
    Enqueue(chunkId: Guid, content: string, model: string):
        // Check if already embedded
        if _cache.HasEmbedding(chunkId, model):
            return
        
        job = new EmbeddingJob {
            Id = Guid.NewGuid(),
            ChunkId = chunkId,
            Content = content,
            Model = model,
            Status = EmbeddingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        }
        
        _pendingJobs.Enqueue(job)
    
    DequeueBatch(batchSize: int):
        batch = new List<EmbeddingJob>()
        
        for i in 0 to batchSize:
            if _pendingJobs.TryDequeue(out job):
                job.Status = EmbeddingStatus.Processing
                job.StartedAt = DateTimeOffset.UtcNow
                _activeJobs[job.Id] = job
                batch.Add(job)
            else:
                break
        
        return batch
    
    CompleteJob(jobId: Guid, result: EmbeddingResult):
        if _activeJobs.TryRemove(jobId, out job):
            job.Status = EmbeddingStatus.Completed
            job.CompletedAt = DateTimeOffset.UtcNow
            _completedJobs.Add(job)
            
            // Cache result
            _cache.Set(job.ChunkId, job.Model, result)
    
    FailJob(jobId: Guid, error: string):
        if _activeJobs.TryRemove(jobId, out job):
            job.ErrorMessage = error
            job.RetryCount++
            
            if job.RetryCount < MaxRetries:
                job.Status = EmbeddingStatus.Retrying
                _pendingJobs.Enqueue(job) // re-queue for retry
            else:
                job.Status = EmbeddingStatus.Failed
                _completedJobs.Add(job)
    
    GetProgress():
        return new EmbeddingProgress {
            Pending = _pendingJobs.Count,
            Active = _activeJobs.Count,
            Completed = _completedJobs.Count(j => j.Status == EmbeddingStatus.Completed),
            Failed = _completedJobs.Count(j => j.Status == EmbeddingStatus.Failed),
            Total = _pendingJobs.Count + _activeJobs.Count + _completedJobs.Count
        }
    }
```

---

## 4. Batch Processor

```
class EmbeddingBatchProcessor
{
    embeddingQueue: EmbeddingQueue
    ollamaClient: OllamaClient
    vectorStore: IVectorStore
    cache: EmbeddingCache
    
    async ProcessBatches(batchSize: int, maxConcurrent: int):
        var semaphore = new SemaphoreSlim(maxConcurrent)
        
        while embeddingQueue.HasPendingJobs():
            batch = embeddingQueue.DequeueBatch(batchSize)
            
            if batch.Count == 0:
                await Task.Delay(100) // wait for new jobs
                continue
            
            // Process batch in parallel
            var tasks = batch.Select(job => ProcessJob(job, semaphore))
            await Task.WhenAll(tasks)
    
    async ProcessJob(job: EmbeddingJob, semaphore: SemaphoreSlim):
        await semaphore.WaitAsync()
        
        try:
            // Check cache first
            cached = cache.Get(job.ChunkId, job.Model)
            if cached != null:
                embeddingQueue.CompleteJob(job.Id, cached)
                return
            
            // Generate embedding via Ollama
            result = await GenerateEmbedding(job.Content, job.Model)
            
            if result != null:
                // Store in vector store
                await vectorStore.Upsert("code_chunks", new VectorPoint {
                    Id = job.ChunkId.ToString(),
                    Vector = result.Vector,
                    Payload = new Dictionary<string, object> {
                        ["chunkId"] = job.ChunkId,
                        ["model"] = job.Model,
                        ["contentHash"] = ComputeHash(job.Content)
                    }
                })
                
                // Cache
                cache.Set(job.ChunkId, job.Model, result)
                
                // Mark complete
                embeddingQueue.CompleteJob(job.Id, result)
            else:
                embeddingQueue.FailJob(job.Id, "Failed to generate embedding")
        
        catch Exception ex:
            embeddingQueue.FailJob(job.Id, ex.Message)
        
        finally:
            semaphore.Release()
    
    async GenerateEmbedding(content: string, model: string):
        try:
            response = await ollamaClient.Embed(new EmbedRequest {
                Model = model,
                Input = content
            })
            
            if response?.Embeddings != null && response.Embeddings.Count > 0:
                return new EmbeddingResult {
                    Vector = response.Embeddings[0],
                    Dimension = response.Embeddings[0].Length,
                    Model = model,
                    GeneratedAt = DateTimeOffset.UtcNow
                }
        
        catch Exception ex:
            logger.Error($"Embedding generation failed: {ex.Message}")
        
        return null
    }
```

---

## 5. Ollama Client

```
class OllamaClient
{
    HttpClient _httpClient;
    string _baseUrl;
    
    async Embed(request: EmbedRequest):
        url = $"{_baseUrl}/api/embed"
        
        response = await _httpClient.PostAsJsonAsync(url, new {
            model = request.Model,
            input = request.Input
        })
        
        response.EnsureSuccessStatusCode()
        
        return await response.Content.ReadFromJsonAsync<EmbedResponse>()
    
    async Chat(request: ChatRequest):
        url = $"{_baseUrl}/api/chat"
        
        response = await _httpClient.PostAsJsonAsync(url, new {
            model = request.Model,
            messages = request.Messages,
            stream = false
        })
        
        response.EnsureSuccessStatusCode()
        
        return await response.Content.ReadFromJsonAsync<ChatResponse>()
    
    async ListModels():
        url = $"{_baseUrl}/api/tags"
        
        response = await _httpClient.GetAsync(url)
        response.EnsureSuccessStatusCode()
        
        return await response.Content.ReadFromJsonAsync<ModelsResponse>()
}

class EmbedRequest
{
    string Model;
    string[] Input;
}

class EmbedResponse
{
    float[][] Embeddings;
    int[][]? EmbeddingsInt8;
}
```

---

## 6. Cache Manager

```
class EmbeddingCacheManager
{
    ConcurrentDictionary<string, CachedEmbedding> _cache;
    TimeSpan _defaultExpiration;
    
    Get(chunkId: Guid, model: string):
        key = GetKey(chunkId, model)
        
        if _cache.TryGetValue(key, out cached):
            if !IsExpired(cached):
                return cached.Vector
            else:
                _cache.TryRemove(key, out _)
        
        return null
    
    Set(chunkId: Guid, model: string, vector: float[]):
        key = GetKey(chunkId, model)
        
        _cache[key] = new CachedEmbedding {
            Vector = vector,
            GeneratedAt = DateTimeOffset.UtcNow,
            Expiration = _defaultExpiration
        }
    
    GetKey(chunkId: Guid, model: string):
        return $"{chunkId}:{model}"
    
    IsExpired(cached: CachedEmbedding):
        return DateTimeOffset.UtcNow - cached.GeneratedAt > cached.Expiration
    
    Clear():
        _cache.Clear()
    
    GetStats():
        return new CacheStats {
            TotalEntries = _cache.Count,
            ValidEntries = _cache.Count(kvp => !IsExpired(kvp.Value)),
            ExpiredEntries = _cache.Count(kvp => IsExpired(kvp.Value))
        }
    }
```

---

## 7. Version Manager

```
class EmbeddingVersionManager
{
    // Track embedding versions for model upgrades
    Dictionary<string, EmbeddingVersion> _versions;
    
    RegisterVersion(model: string, dimension: int):
        _versions[model] = new EmbeddingVersion {
            Model = model,
            Dimension = dimension,
            CreatedAt = DateTimeOffset.UtcNow,
            ChunkCount = 0
        }
    
    NeedsRegeneration(chunkId: Guid, currentModel: string):
        // Check if chunk was embedded with a different model version
        storedVersion = GetStoredVersion(chunkId)
        
        if storedVersion == null:
            return true // not embedded yet
        
        if storedVersion.Model != currentModel:
            return true // model changed
        
        if storedVersion.Dimension != GetModelDimension(currentModel):
            return true // dimension changed
        
        return false
    
    UpdateVersion(chunkId: Guid, model: string):
        if _versions.ContainsKey(model):
            _versions[model].ChunkCount++
    
    GetModelDimension(model: string):
        return model switch {
            "nomic-embed-text" => 768,
            "mxbai-embed-large" => 1024,
            "bge-m3" => 1024,
            _ => 768
        }
    }
```

---

## 8. Retry Handler

```
class EmbeddingRetryHandler
{
    int MaxRetries = 3;
    TimeSpan[] RetryDelays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15)
    ];
    
    async ExecuteWithRetry<T>(Func<Task<T>> operation, string operationName):
        for attempt in 0 to MaxRetries:
            try:
                return await operation()
            
            catch RateLimitException:
                if attempt < MaxRetries - 1:
                    delay = RetryDelays[attempt]
                    logger.Warning($"Rate limited on {operationName}, waiting {delay}")
                    await Task.Delay(delay)
                else:
                    throw
            
            catch TimeoutException:
                if attempt < MaxRetries - 1:
                    delay = RetryDelays[attempt]
                    logger.Warning($"Timeout on {operationName}, retrying in {delay}")
                    await Task.Delay(delay)
                else:
                    throw
            
            catch Exception ex:
                if attempt < MaxRetries - 1 && IsTransient(ex):
                    delay = RetryDelays[attempt]
                    logger.Warning($"Transient error on {operationName}: {ex.Message}, retrying in {delay}")
                    await Task.Delay(delay)
                else:
                    throw
        
        throw new MaxRetriesExceededException(operationName, MaxRetries)
    
    IsTransient(ex: Exception):
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is IOException
    }
```

---

## 9. Configuration

```csharp
class EmbeddingConfig
{
    // Ollama settings
    string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    string DefaultModel { get; set; } = "nomic-embed-text";
    
    // Available models
    Dictionary<string, ModelConfig> Models { get; set; } = new()
    {
        ["nomic-embed-text"] = new ModelConfig
        {
            Dimension = 768,
            MaxTokens = 8192,
            SupportsBatch = true
        },
        ["mxbai-embed-large"] = new ModelConfig
        {
            Dimension = 1024,
            MaxTokens = 512,
            SupportsBatch = false
        },
        ["bge-m3"] = new ModelConfig
        {
            Dimension = 1024,
            MaxTokens = 8192,
            SupportsBatch = true
        }
    };
    
    // Queue settings
    int BatchSize { get; set; } = 32;
    int MaxConcurrent { get; set; } = 4;
    int MaxRetries { get; set; } = 3;
    
    // Cache settings
    TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);
    int MaxCacheSize { get; set; } = 100000;
}

class ModelConfig
{
    int Dimension;
    int MaxTokens;
    bool SupportsBatch;
}
```

---

## 10. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Queue Enqueue | O(1) | O(1) |
| Queue Dequeue | O(1) | O(1) |
| Batch Processing | O(B * E) where B = batch size, E = embedding time | O(B * D) where D = dimension |
| Cache Lookup | O(1) | O(1) |
| Cache Store | O(1) | O(D) |
| Vector Store Upsert | O(D) | O(D) |
| Retry with Backoff | O(R * T) where R = retries, T = wait time | O(1) |
