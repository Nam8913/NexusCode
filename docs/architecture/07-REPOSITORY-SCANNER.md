# Nexus Code Intelligence Platform - Repository Scanner

## Overview

The Repository Scanner handles efficient scanning of large codebases (10K-1M+ files) with incremental indexing, hash-based change detection, and real-time file watching.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Repository Scanner                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ File Discovery│  │   Change     │  │   Index      │  │
│  │               │→│   Detector   │→│   Queue       │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│         ↓                ↓                  ↓            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ File System   │  │   Hash       │  │   Parallel   │  │
│  │ Watcher       │  │   Tracker    │  │   Processor  │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                          ↓                  ↓            │
│                   ┌──────────────┐  ┌──────────────┐    │
│                   │   Progress   │  │   Resumable   │    │
│                   │   Reporter   │  │   Checkpoint  │    │
│                   └──────────────┘  └──────────────┘    │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Core Data Structures

```csharp
class ScanState
{
    Guid RepositoryId;
    string RepositoryPath;
    DateTimeOffset LastFullScan;
    DateTimeOffset LastIncrementalScan;
    ScanStatus Status;
    
    // File tracking
    Dictionary<string, FileMetadata> TrackedFiles;
    
    // Scan progress
    int TotalFiles;
    int ProcessedFiles;
    int FailedFiles;
    
    // Checkpoint for resumable scanning
    ScanCheckpoint? Checkpoint;
}

struct FileMetadata
{
    string FilePath;
    string RelativePath;
    long Size;
    string ContentHash; // SHA256
    DateTimeOffset LastModified;
    DateTimeOffset LastIndexed;
    FileStatus Status;
}

class ScanCheckpoint
{
    int LastProcessedIndex;
    List<string> ProcessedFiles;
    DateTimeOffset CheckpointTime;
    Dictionary<string, object> State;
}

enum ScanStatus
{
    Pending,
    Scanning,
    Indexing,
    Complete,
    Failed,
    Paused
}

enum FileStatus
{
    New,
    Modified,
    Unchanged,
    Deleted,
    Error
}
```

---

## 3. File Discovery

### 3.1 Directory Traversal

```
class FileDiscovery
{
    scanOptions: ScanOptions
    
    DiscoverFiles(repositoryPath: string):
        results = new DiscoveryResults()
        
        // Get all C# files
        csFiles = Directory.GetFiles(repositoryPath, "*.cs", SearchOption.AllDirectories)
        
        // Filter excluded directories
        csFiles = csFiles.Where(f => !IsExcluded(f, scanOptions.ExcludePatterns))
        
        // Get project files
        csprojFiles = Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories)
        slnFiles = Directory.GetFiles(repositoryPath, "*.sln", SearchOption.AllDirectories)
        
        // Get Unity files if enabled
        if scanOptions.IncludeUnity:
            unityFiles = DiscoverUnityFiles(repositoryPath)
            results.UnityFiles = unityFiles
        
        results.SourceFiles = csFiles
        results.ProjectFiles = csprojFiles
        results.SolutionFiles = slnFiles
        results.TotalFiles = csFiles.Length + csprojFiles.Length + slnFiles.Length
        
        return results
    
    IsExcluded(filePath: string, patterns: string[]):
        foreach pattern in patterns:
            if filePath.Contains(pattern):
                return true
        
        // Always exclude common directories
        excludedDirs = [".git", "bin", "obj", "node_modules", ".vs", "packages"]
        foreach dir in excludedDirs:
            if filePath.Contains($"/{dir}/") || filePath.Contains($"\\{dir}\\"):
                return true
        
        return false
    
    DiscoverUnityFiles(repositoryPath: string):
        unityFiles = new UnityFiles()
        
        // Unity scenes
        unityFiles.Scenes = Directory.GetFiles(repositoryPath, "*.unity", SearchOption.AllDirectories)
        
        // Unity prefabs
        unityFiles.Prefabs = Directory.GetFiles(repositoryPath, "*.prefab", SearchOption.AllDirectories)
        
        // Assembly definitions
        unityFiles.AsmdefFiles = Directory.GetFiles(repositoryPath, "*.asmdef", SearchOption.AllDirectories)
        
        // ScriptableObjects
        unityFiles.ScriptableObjects = Directory.GetFiles(repositoryPath, "*.asset", SearchOption.AllDirectories)
        
        // Addressables
        unityFiles.Addressables = Directory.GetFiles(repositoryPath, "*.addressable", SearchOption.AllDirectories)
        
        return unityFiles
}
```

### 3.2 File Filtering

```csharp
class ScanOptions
{
    string[] ExcludePatterns { get; set; } = [
        ".git", "bin", "obj", "node_modules", ".vs", "packages",
        "Library", "Temp", "Logs", "UserSettings"
    ];
    
    string[] IncludeExtensions { get; set; } = [".cs", ".csproj", ".sln"];
    
    bool IncludeGenerated { get; set; } = false;
    bool IncludeUnity { get; set; } = true;
    long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
    int MaxDirectoryDepth { get; set; } = 20;
}
```

---

## 4. Change Detection

### 4.1 Hash-Based Detection

```
class ChangeDetector
{
    scanState: ScanState
    
    DetectChanges(discoveredFiles: string[]):
        changeSet = new ChangeSet()
        
        // Get current file metadata
        currentFiles = new Dictionary<string, FileMetadata>()
        
        foreach filePath in discoveredFiles:
            metadata = GetFileMetadata(filePath)
            currentFiles[filePath] = metadata
        
        // Check for new files
        foreach filePath in currentFiles.Keys:
            if !scanState.TrackedFiles.ContainsKey(filePath):
                changeSet.NewFiles.Add(filePath)
        
        // Check for deleted files
        foreach filePath in scanState.TrackedFiles.Keys:
            if !currentFiles.ContainsKey(filePath):
                changeSet.DeletedFiles.Add(filePath)
        
        // Check for modified files
        foreach filePath in currentFiles.Keys:
            if scanState.TrackedFiles.ContainsKey(filePath):
                oldMetadata = scanState.TrackedFiles[filePath]
                newMetadata = currentFiles[filePath]
                
                if newMetadata.ContentHash != oldMetadata.ContentHash:
                    changeSet.ModifiedFiles.Add(filePath)
        
        changeSet.HasChanges = changeSet.NewFiles.Count > 0 ||
                               changeSet.DeletedFiles.Count > 0 ||
                               changeSet.ModifiedFiles.Count > 0
        
        return changeSet
    
    GetFileMetadata(filePath: string):
        info = new FileInfo(filePath)
        
        return new FileMetadata {
            FilePath: filePath,
            RelativePath: GetRelativePath(filePath, scanState.RepositoryPath),
            Size: info.Length,
            ContentHash: ComputeHash(filePath),
            LastModified: info.LastWriteTimeUtc,
            LastIndexed: DateTimeOffset.MinValue,
            Status: FileStatus.New
        }
    
    ComputeHash(filePath: string):
        using var stream = File.OpenRead(filePath)
        using var sha256 = SHA256.Create()
        var hash = sha256.ComputeHash(stream)
        return Convert.ToBase64String(hash)
}
```

### 4.2 Change Set

```csharp
class ChangeSet
{
    List<string> NewFiles { get; set; } = new();
    List<string> ModifiedFiles { get; set; } = new();
    List<string> DeletedFiles { get; set; } = new();
    bool HasChanges { get; set; }
    
    int TotalChanges => NewFiles.Count + ModifiedFiles.Count + DeletedFiles.Count;
}
```

---

## 5. Index Queue

```
class IndexQueue
{
    ConcurrentQueue<IndexJob> _queue;
    ConcurrentDictionary<string, IndexJob> _activeJobs;
    IndexJob[] _completedJobs;
    
    Enqueue(filePath: string, changeType: ChangeType):
        job = new IndexJob {
            Id = Guid.NewGuid(),
            FilePath: filePath,
            ChangeType: changeType,
            Priority: CalculatePriority(changeType),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = IndexJobStatus.Pending
        }
        
        _queue.Enqueue(job)
        _activeJobs[filePath] = job
    
    CalculatePriority(changeType: ChangeType):
        switch changeType:
            case ChangeType.Deleted => return 1 // highest priority
            case ChangeType.Modified => return 2
            case ChangeType.New => return 3
            default => return 4
    
    DequeueBatch(batchSize: int):
        batch = new List<IndexJob>()
        
        for i in 0 to batchSize:
            if _queue.TryDequeue(out job):
                job.Status = IndexJobStatus.Processing
                batch.Add(job)
            else:
                break
        
        return batch
    
    CompleteJob(jobId: Guid, success: bool):
        if _activeJobs.Values.TryFirst(j => j.Id == jobId, out job):
            job.Status = success ? IndexJobStatus.Completed : IndexJobStatus.Failed
            job.CompletedAt = DateTimeOffset.UtcNow
            
            _activeJobs.TryRemove(job.FilePath, out _)
            _completedJobs.Add(job)
    
    GetProgress():
        return new IndexProgress {
            Total = _activeJobs.Count + _completedJobs.Length,
            Completed = _completedJobs.Length,
            Failed = _completedJobs.Count(j => j.Status == IndexJobStatus.Failed),
            Pending = _queue.Count,
            Active = _activeJobs.Count
        }
}
```

---

## 6. Parallel Processor

```
class ParallelIndexProcessor
{
    indexQueue: IndexQueue
    roslynEngine: RoslynEngine
    knowledgeGraph: KnowledgeGraph
    symbolTable: SymbolTable
    
    ProcessFiles(changeSet: ChangeSet, maxParallelism: int):
        // Enqueue all changes
        foreach file in changeSet.DeletedFiles:
            indexQueue.Enqueue(file, ChangeType.Deleted)
        
        foreach file in changeSet.ModifiedFiles:
            indexQueue.Enqueue(file, ChangeType.Modified)
        
        foreach file in changeSet.NewFiles:
            indexQueue.Enqueue(file, ChangeType.New)
        
        // Process in parallel
        var options = new ParallelOptions {
            MaxDegreeOfParallelism = maxParallelism
        }
        
        var cancellationTokenSource = new CancellationTokenSource()
        
        Parallel.ForEach(
            GetNextJob(),
            options,
            cancellationTokenSource.Token,
            job => ProcessJob(job)
        )
    
    GetNextJob():
        // Yield jobs as they become available
        while indexQueue.HasPendingJobs():
            batch = indexQueue.DequeueBatch(10)
            foreach job in batch:
                yield return job
    
    ProcessJob(job: IndexJob):
        try
            switch job.ChangeType:
                case ChangeType.Deleted:
                    ProcessDeletedFile(job.FilePath)
                
                case ChangeType.Modified:
                    ProcessModifiedFile(job.FilePath)
                
                case ChangeType.New:
                    ProcessNewFile(job.FilePath)
            
            indexQueue.CompleteJob(job.Id, success: true)
        
        catch Exception ex
            logger.Error($"Failed to process {job.FilePath}: {ex.Message}")
            indexQueue.CompleteJob(job.Id, success: false)
    
    ProcessDeletedFile(filePath: string):
        // Remove from symbol table
        symbols = symbolTable.GetSymbolsByFile(filePath)
        foreach symbol in symbols:
            symbolTable.Remove(symbol.Id)
        
        // Remove from knowledge graph
        nodes = knowledgeGraph.GetNodesByFile(filePath)
        foreach node in nodes:
            knowledgeGraph.RemoveNode(node.Id)
        
        // Remove from index
        scanState.TrackedFiles.Remove(filePath)
    
    ProcessModifiedFile(filePath: string):
        // Re-analyze file
        result = roslynEngine.AnalyzeFile(filePath)
        
        // Update symbol table
        oldSymbols = symbolTable.GetSymbolsByFile(filePath)
        newSymbols = result.Symbols
        
        // Diff and update
        DiffAndUpdate(oldSymbols, newSymbols)
        
        // Update knowledge graph
        oldNodes = knowledgeGraph.GetNodesByFile(filePath)
        newNodes = result.GraphNodes
        
        DiffAndUpdateGraph(oldNodes, newNodes)
        
        // Update tracked files
        scanState.TrackedFiles[filePath] = GetFileMetadata(filePath)
    
    ProcessNewFile(filePath: string):
        // Full analysis
        result = roslynEngine.AnalyzeFile(filePath)
        
        // Add to symbol table
        foreach symbol in result.Symbols:
            symbolTable.Add(symbol)
        
        // Add to knowledge graph
        foreach node in result.GraphNodes:
            knowledgeGraph.AddNode(node)
        
        foreach edge in result.GraphEdges:
            knowledgeGraph.AddEdge(edge)
        
        // Add to tracked files
        scanState.TrackedFiles[filePath] = GetFileMetadata(filePath)
    
    DiffAndUpdate(oldSymbols: SymbolInfo[], newSymbols: SymbolInfo[]):
        oldDict = oldSymbols.ToDictionary(s => s.Id)
        newDict = newSymbols.ToDictionary(s => s.Id)
        
        // Find added symbols
        foreach symbol in newSymbols:
            if !oldDict.ContainsKey(symbol.Id):
                symbolTable.Add(symbol)
        
        // Find removed symbols
        foreach symbol in oldSymbols:
            if !newDict.ContainsKey(symbol.Id):
                symbolTable.Remove(symbol.Id)
        
        // Find modified symbols
        foreach symbol in newSymbols:
            if oldDict.TryGetValue(symbol.Id, out oldSymbol):
                if symbol.CodeHash != oldSymbol.CodeHash:
                    symbolTable.Update(symbol)
```

---

## 7. File Watcher

```
class RepositoryFileWatcher
{
    FileSystemWatcher _watcher;
    IndexQueue _indexQueue;
    Debouncer _debouncer;
    
    StartWatching(repositoryPath: string):
        _watcher = new FileSystemWatcher(repositoryPath)
        _watcher.IncludeSubdirectories = true
        _watcher.Filter = "*.cs"
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        
        _watcher.Changed += OnFileChanged
        _watcher.Created += OnFileCreated
        _watcher.Deleted += OnFileDeleted
        _watcher.Renamed += OnFileRenamed
        
        _watcher.EnableRaisingEvents = true
    
    OnFileChanged(sender: object, args: FileSystemEventArgs):
        // Debounce rapid changes (e.g., auto-save)
        _debouncer.Debounce(args.FullPath, TimeSpan.FromMilliseconds(500), () =>
        {
            _indexQueue.Enqueue(args.FullPath, ChangeType.Modified)
        })
    
    OnFileCreated(sender: object, args: FileSystemEventArgs):
        _indexQueue.Enqueue(args.FullPath, ChangeType.New)
    
    OnFileDeleted(sender: object, args: FileSystemEventArgs):
        _indexQueue.Enqueue(args.FullPath, ChangeType.Deleted)
    
    OnFileRenamed(sender: object, args: RenamedEventArgs):
        _indexQueue.Enqueue(args.OldFullPath, ChangeType.Deleted)
        _indexQueue.Enqueue(args.FullPath, ChangeType.New)
    
    StopWatching():
        _watcher.EnableRaisingEvents = false
        _watcher.Dispose()
}

class Debouncer
{
    ConcurrentDictionary<string, Timer> _timers;
    
    Debounce(key: string, delay: TimeSpan, action: Action):
        if _timers.TryGetValue(key, out existingTimer):
            existingTimer.Dispose()
        
        var timer = new Timer(_ =>
        {
            action();
            _timers.TryRemove(key, out _);
        }, null, delay, Timeout.InfiniteTimeSpan)
        
        _timers[key] = timer
}
```

---

## 8. Progress Reporting

```
class ScanProgressReporter
{
    Progress<ScanProgress> _progress;
    
    ReportProgress(state: ScanState):
        progress = new ScanProgress {
            RepositoryName = state.RepositoryPath,
            Status = state.Status,
            TotalFiles = state.TotalFiles,
            ProcessedFiles = state.ProcessedFiles,
            FailedFiles = state.FailedFiles,
            PercentComplete = state.TotalFiles > 0 
                ? (double)state.ProcessedFiles / state.TotalFiles * 100 
                : 0,
            Elapsed = DateTimeOffset.UtcNow - state.StartTime,
            EstimatedRemaining = CalculateEstimatedRemaining(state)
        }
        
        _progress.Report(progress)
    
    CalculateEstimatedRemaining(state: ScanState):
        if state.ProcessedFiles == 0:
            return TimeSpan.MaxValue
        
        elapsed = DateTimeOffset.UtcNow - state.StartTime
        rate = state.ProcessedFiles / elapsed.TotalSeconds
        remaining = state.TotalFiles - state.ProcessedFiles
        
        return TimeSpan.FromSeconds(remaining / rate)
    }
}

class ScanProgress
{
    string RepositoryName;
    ScanStatus Status;
    int TotalFiles;
    int ProcessedFiles;
    int FailedFiles;
    double PercentComplete;
    TimeSpan Elapsed;
    TimeSpan EstimatedRemaining;
    
    string StatusMessage => Status switch {
        ScanStatus.Pending => "Waiting to start...",
        ScanStatus.Scanning => $"Scanning files... {ProcessedFiles}/{TotalFiles}",
        ScanStatus.Indexing => $"Indexing... {ProcessedFiles}/{TotalFiles}",
        ScanStatus.Complete => $"Complete! {ProcessedFiles} files indexed",
        ScanStatus.Failed => $"Failed after {ProcessedFiles} files",
        ScanStatus.Paused => "Paused"
    };
}
```

---

## 9. Complexity Analysis

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|-----------------|
| Full Scan | O(F) where F = total files | O(F) |
| Change Detection | O(F) | O(F) |
| Hash Computation | O(S) where S = file size | O(1) |
| Incremental Index | O(C) where C = changed files | O(C) |
| Parallel Processing | O(F/P) where P = processors | O(F) |
| File Watching | O(1) per event | O(1) |

---

## 10. Performance Characteristics

| Metric | Tier 1 (10K files) | Tier 2 (100K files) | Tier 3 (1M files) |
|--------|-------------------|--------------------|--------------------|
| Full Scan | < 30s | < 5min | < 30min |
| Change Detection | < 1s | < 5s | < 30s |
| Incremental Index (1 file) | < 2s | < 2s | < 2s |
| Memory Usage | < 500MB | < 2GB | < 8GB |
| Concurrent Watchers | 10+ | 5+ | 3+ |
