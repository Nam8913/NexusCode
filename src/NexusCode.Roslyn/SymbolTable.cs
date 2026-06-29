using System.Collections.Concurrent;
using NexusCode.Domain;
using DomainSymbolKind = NexusCode.Domain.SymbolKind;

namespace NexusCode.Roslyn;

public sealed class SymbolTable
{
    private readonly ConcurrentDictionary<Guid, SymbolEntity> _symbolsById = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _symbolsByFullName = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _symbolsByName = new();
    private readonly ConcurrentDictionary<DomainSymbolKind, List<Guid>> _symbolsByKind = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _symbolsByFile = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _symbolsByContainer = new();
    private readonly ConcurrentDictionary<Guid, List<ReferenceEntity>> _references = new();
    private readonly object _lock = new();

    public int Count => _symbolsById.Count;

    public void Add(SymbolEntity symbol)
    {
        _symbolsById[symbol.Id] = symbol;

        lock (_lock)
        {
            AddToIndex(_symbolsByFullName, symbol.FullName, symbol.Id);
            AddToIndex(_symbolsByName, symbol.Name, symbol.Id);
            AddToIndex(_symbolsByKind, symbol.Kind, symbol.Id);

            if (!string.IsNullOrEmpty(symbol.FilePath))
                AddToIndex(_symbolsByFile, symbol.FilePath, symbol.Id);

            if (symbol.ContainerId.HasValue)
                AddToIndex(_symbolsByContainer, symbol.ContainerId.Value, symbol.Id);
        }
    }

    public void Update(SymbolEntity symbol)
    {
        if (_symbolsById.TryGetValue(symbol.Id, out var existing))
        {
            RemoveFromIndexes(existing);
        }

        _symbolsById[symbol.Id] = symbol;

        lock (_lock)
        {
            AddToIndex(_symbolsByFullName, symbol.FullName, symbol.Id);
            AddToIndex(_symbolsByName, symbol.Name, symbol.Id);
            AddToIndex(_symbolsByKind, symbol.Kind, symbol.Id);

            if (!string.IsNullOrEmpty(symbol.FilePath))
                AddToIndex(_symbolsByFile, symbol.FilePath, symbol.Id);

            if (symbol.ContainerId.HasValue)
                AddToIndex(_symbolsByContainer, symbol.ContainerId.Value, symbol.Id);
        }
    }

    public void Remove(Guid id)
    {
        if (_symbolsById.TryRemove(id, out var entity))
        {
            RemoveFromIndexes(entity);
        }
    }

    public SymbolEntity? GetById(Guid id)
    {
        return _symbolsById.TryGetValue(id, out var entity) ? entity : null;
    }

    public SymbolEntity? GetByFullName(string fullName)
    {
        if (_symbolsByFullName.TryGetValue(fullName, out var ids) && ids.Count > 0)
        {
            return _symbolsById.TryGetValue(ids[0], out var entity) ? entity : null;
        }
        return null;
    }

    public SymbolEntity? ResolveSymbol(string name)
    {
        var byFull = GetByFullName(name);
        if (byFull != null) return byFull;

        var byName = GetByName(name);
        if (byName.Count == 1) return byName[0];
        if (byName.Count > 1)
        {
            SymbolEntity? best = null;
            foreach (var s in byName)
            {
                if (s.Kind == SymbolKind.Type) return s;
                if (s.Kind == SymbolKind.Method && best == null) best = s;
            }
            return best ?? byName[0];
        }

        SymbolEntity? fallback = null;
        foreach (var s in _symbolsById.Values)
        {
            if (s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return s;
            if (fallback == null && s.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) fallback = s;
            if (fallback == null && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) fallback = s;
        }
        return fallback;
    }

    public IReadOnlyList<SymbolEntity> GetByName(string name)
    {
        if (_symbolsByName.TryGetValue(name, out var ids))
        {
            return ids
                .Where(id => _symbolsById.ContainsKey(id))
                .Select(id => _symbolsById[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<SymbolEntity> GetByKind(DomainSymbolKind kind)
    {
        if (_symbolsByKind.TryGetValue(kind, out var ids))
        {
            return ids
                .Where(id => _symbolsById.ContainsKey(id))
                .Select(id => _symbolsById[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<SymbolEntity> GetByFile(string filePath)
    {
        if (_symbolsByFile.TryGetValue(filePath, out var ids))
        {
            return ids
                .Where(id => _symbolsById.ContainsKey(id))
                .Select(id => _symbolsById[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<SymbolEntity> GetByContainer(Guid containerId)
    {
        if (_symbolsByContainer.TryGetValue(containerId, out var ids))
        {
            return ids
                .Where(id => _symbolsById.ContainsKey(id))
                .Select(id => _symbolsById[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<ReferenceEntity> GetReferences(Guid symbolId)
    {
        if (_references.TryGetValue(symbolId, out var refs))
        {
            return refs.AsReadOnly();
        }
        return [];
    }

    public void AddReference(ReferenceEntity reference)
    {
        _references.AddOrUpdate(
            reference.SymbolId,
            _ => [reference],
            (_, list) =>
            {
                lock (_lock)
                {
                    list.Add(reference);
                }
                return list;
            });
    }

    private void RemoveFromIndexes(SymbolEntity entity)
    {
        lock (_lock)
        {
            RemoveFromIndex(_symbolsByFullName, entity.FullName, entity.Id);
            RemoveFromIndex(_symbolsByName, entity.Name, entity.Id);
            RemoveFromIndex(_symbolsByKind, entity.Kind, entity.Id);

            if (!string.IsNullOrEmpty(entity.FilePath))
                RemoveFromIndex(_symbolsByFile, entity.FilePath, entity.Id);

            if (entity.ContainerId.HasValue)
                RemoveFromIndex(_symbolsByContainer, entity.ContainerId.Value, entity.Id);
        }
    }

    private static void AddToIndex<T>(ConcurrentDictionary<T, List<Guid>> index, T key, Guid id) where T : notnull
    {
        index.AddOrUpdate(
            key,
            _ => [id],
            (_, list) =>
            {
                if (!list.Contains(id))
                    list.Add(id);
                return list;
            });
    }

    private static void RemoveFromIndex<T>(ConcurrentDictionary<T, List<Guid>> index, T key, Guid id) where T : notnull
    {
        if (index.TryGetValue(key, out var list))
        {
            list.Remove(id);
            if (list.Count == 0)
                index.TryRemove(key, out _);
        }
    }
}
