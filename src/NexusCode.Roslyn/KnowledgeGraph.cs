using System.Collections.Concurrent;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class KnowledgeGraph
{
    private readonly ConcurrentDictionary<byte[], GraphNodeEntity> _nodes = new(new ByteArrayComparer());
    private readonly ConcurrentDictionary<byte[], GraphEdgeEntity> _edges = new(new ByteArrayComparer());
    private readonly ConcurrentDictionary<byte[], List<byte[]>> _outgoingEdges = new(new ByteArrayComparer());
    private readonly ConcurrentDictionary<byte[], List<byte[]>> _incomingEdges = new(new ByteArrayComparer());
    private readonly ConcurrentDictionary<NodeKind, List<byte[]>> _nodesByKind = new();
    private readonly ConcurrentDictionary<string, List<byte[]>> _nodesByFile = new();
    private readonly ConcurrentDictionary<EdgeKind, List<byte[]>> _edgesByKind = new();
    private readonly object _lock = new();

    public int NodeCount => _nodes.Count;
    public int EdgeCount => _edges.Count;

    public void AddNode(GraphNodeEntity node)
    {
        lock (_lock)
        {
            _nodes[node.Id] = node;
            AddToIndex(_nodesByKind, node.Kind, node.Id);

            if (node.Metadata.TryGetValue("FilePath", out var filePath))
                AddToIndex(_nodesByFile, filePath, node.Id);
        }
    }

    public void UpdateNode(GraphNodeEntity node)
    {
        lock (_lock)
        {
            if (_nodes.TryGetValue(node.Id, out var existing))
            {
                if (existing.Metadata.TryGetValue("FilePath", out var oldFilePath))
                    RemoveFromIndex(_nodesByFile, oldFilePath, node.Id);

                RemoveFromIndex(_nodesByKind, existing.Kind, node.Id);
            }

            _nodes[node.Id] = node;
            AddToIndex(_nodesByKind, node.Kind, node.Id);

            if (node.Metadata.TryGetValue("FilePath", out var filePath))
                AddToIndex(_nodesByFile, filePath, node.Id);
        }
    }

    public void RemoveNode(byte[] id)
    {
        GraphNodeEntity? entity = null;
        List<byte[]>? outgoing = null;
        List<byte[]>? incoming = null;

        lock (_lock)
        {
            _nodes.TryRemove(id, out entity);
            if (entity != null)
            {
                RemoveFromIndex(_nodesByKind, entity.Kind, id);
                if (entity.Metadata.TryGetValue("FilePath", out var filePath))
                    RemoveFromIndex(_nodesByFile, filePath, id);
            }

            _outgoingEdges.TryRemove(id, out outgoing);
            _incomingEdges.TryRemove(id, out incoming);
        }

        if (outgoing != null)
        {
            foreach (var edgeId in outgoing)
            {
                if (_edges.TryRemove(edgeId, out var edge))
                {
                    RemoveFromIndex(_edgesByKind, edge.Kind, edgeId);
                    RemoveFromList(_incomingEdges, edge.TargetId, edgeId);
                }
            }
        }

        if (incoming != null)
        {
            foreach (var edgeId in incoming)
            {
                if (_edges.TryRemove(edgeId, out var edge))
                {
                    RemoveFromIndex(_edgesByKind, edge.Kind, edgeId);
                    RemoveFromList(_outgoingEdges, edge.SourceId, edgeId);
                }
            }
        }
    }

    public GraphNodeEntity? GetNode(byte[] id)
    {
        return _nodes.TryGetValue(id, out var entity) ? entity : null;
    }

    public IReadOnlyList<GraphNodeEntity> GetNodesByKind(NodeKind kind)
    {
        if (_nodesByKind.TryGetValue(kind, out var ids))
        {
            return ids
                .Where(id => _nodes.ContainsKey(id))
                .Select(id => _nodes[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<GraphNodeEntity> GetNodesByFile(string filePath)
    {
        if (_nodesByFile.TryGetValue(filePath, out var ids))
        {
            return ids
                .Where(id => _nodes.ContainsKey(id))
                .Select(id => _nodes[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public void AddEdge(GraphEdgeEntity edge)
    {
        _edges[edge.Id] = edge;

        lock (_lock)
        {
            AddToIndex(_edgesByKind, edge.Kind, edge.Id);
            AddToEdgeList(_outgoingEdges, edge.SourceId, edge.Id);
            AddToEdgeList(_incomingEdges, edge.TargetId, edge.Id);
        }
    }

    public void RemoveEdge(byte[] id)
    {
        if (_edges.TryRemove(id, out var entity))
        {
            lock (_lock)
            {
                RemoveFromIndex(_edgesByKind, entity.Kind, id);
                RemoveFromList(_outgoingEdges, entity.SourceId, id);
                RemoveFromList(_incomingEdges, entity.TargetId, id);
            }
        }
    }

    public IReadOnlyList<GraphEdgeEntity> GetOutgoingEdges(byte[] nodeId)
    {
        if (_outgoingEdges.TryGetValue(nodeId, out var edgeIds))
        {
            return edgeIds
                .Where(id => _edges.ContainsKey(id))
                .Select(id => _edges[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<GraphEdgeEntity> GetIncomingEdges(byte[] nodeId)
    {
        if (_incomingEdges.TryGetValue(nodeId, out var edgeIds))
        {
            return edgeIds
                .Where(id => _edges.ContainsKey(id))
                .Select(id => _edges[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    public IReadOnlyList<GraphEdgeEntity> GetEdgesByKind(EdgeKind kind)
    {
        if (_edgesByKind.TryGetValue(kind, out var ids))
        {
            return ids
                .Where(id => _edges.ContainsKey(id))
                .Select(id => _edges[id])
                .ToList()
                .AsReadOnly();
        }
        return [];
    }

    private static void AddToIndex<T>(ConcurrentDictionary<T, List<byte[]>> index, T key, byte[] id) where T : notnull
    {
        index.AddOrUpdate(
            key,
            _ => [id],
            (_, list) =>
            {
                lock (list)
                {
                    if (!list.Any(x => x.SequenceEqual(id)))
                        list.Add(id);
                }
                return list;
            });
    }

    private static void RemoveFromIndex<T>(ConcurrentDictionary<T, List<byte[]>> index, T key, byte[] id) where T : notnull
    {
        if (index.TryGetValue(key, out var list))
        {
            lock (list)
            {
                list.RemoveAll(x => x.SequenceEqual(id));
                if (list.Count == 0)
                    index.TryRemove(key, out _);
            }
        }
    }

    private static void AddToEdgeList(ConcurrentDictionary<byte[], List<byte[]>> dict, byte[] key, byte[] edgeId)
    {
        dict.AddOrUpdate(
            key,
            _ => [edgeId],
            (_, list) =>
            {
                lock (list)
                {
                    if (!list.Any(x => x.SequenceEqual(edgeId)))
                        list.Add(edgeId);
                }
                return list;
            });
    }

    private static void RemoveFromList(ConcurrentDictionary<byte[], List<byte[]>> dict, byte[] key, byte[] edgeId)
    {
        if (dict.TryGetValue(key, out var list))
        {
            lock (list)
            {
                list.RemoveAll(x => x.SequenceEqual(edgeId));
                if (list.Count == 0)
                    dict.TryRemove(key, out _);
            }
        }
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var b in obj)
                    hash = hash * 31 + b;
                return hash;
            }
        }
    }
}
