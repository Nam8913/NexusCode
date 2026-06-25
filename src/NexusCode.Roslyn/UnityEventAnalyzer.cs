using Microsoft.CodeAnalysis;
using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class UnityEventAnalyzer
{
    public UnityEventInfo AnalyzeType(INamedTypeSymbol symbol)
    {
        var info = new UnityEventInfo
        {
            TypeName = symbol.Name,
            FullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        };

        info.UnityEvents = FindUnityEvents(symbol);
        info.SerializableEvents = FindSerializableEvents(symbol);

        return info;
    }

    public List<GraphEdgeEntity> BuildEventEdges(UnityEventInfo eventInfo, Dictionary<string, byte[]> scriptNameToId)
    {
        var edges = new List<GraphEdgeEntity>();
        var sourceId = CreateEventSourceId(eventInfo.FullName);

        foreach (var evt in eventInfo.UnityEvents)
        {
            foreach (var listener in evt.Listeners)
            {
                if (scriptNameToId.TryGetValue(listener.TargetScript, out var targetId))
                {
                    edges.Add(new GraphEdgeEntity
                    {
                        Id = ComputeEdgeId(sourceId, targetId, EdgeKind.UnityEvent),
                        SourceId = sourceId,
                        TargetId = targetId,
                        Kind = EdgeKind.UnityEvent,
                        Metadata = new Dictionary<string, string>
                        {
                            ["EventName"] = evt.Name,
                            ["MethodName"] = listener.MethodName,
                            ["TargetScript"] = listener.TargetScript
                        }
                    });
                }
            }
        }

        return edges;
    }

    private List<UnityEventField> FindUnityEvents(INamedTypeSymbol symbol)
    {
        var events = new List<UnityEventField>();

        foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (IsUnityEventType(field.Type))
            {
                events.Add(new UnityEventField
                {
                    Name = field.Name,
                    TypeName = field.Type.ToDisplayString(),
                    Listeners = []
                });
            }
        }

        foreach (var prop in symbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (IsUnityEventType(prop.Type))
            {
                events.Add(new UnityEventField
                {
                    Name = prop.Name,
                    TypeName = prop.Type.ToDisplayString(),
                    Listeners = []
                });
            }
        }

        return events;
    }

    private List<SerializableEventField> FindSerializableEvents(INamedTypeSymbol symbol)
    {
        var events = new List<SerializableEventField>();

        foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (HasAttribute(field, "UnityEngine.SerializeField") && IsEventLikeType(field.Type))
            {
                events.Add(new SerializableEventField
                {
                    Name = field.Name,
                    TypeName = field.Type.ToDisplayString()
                });
            }
        }

        return events;
    }

    private bool IsUnityEventType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.Contains("UnityEvent") ||
               typeName.Contains("UnityAction") ||
               typeName.StartsWith("UnityEngine.Events.");
    }

    private bool IsEventLikeType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.Contains("Event") || typeName.Contains("Action") || typeName.Contains("Func");
    }

    private bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == attributeName);
    }

    private static byte[] CreateEventSourceId(string fullName)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"UnityEvent:{fullName}"));
        return hash[..16];
    }

    private static byte[] ComputeEdgeId(byte[] source, byte[] target, EdgeKind kind)
    {
        var combined = new byte[source.Length + target.Length + 4];
        source.CopyTo(combined, 0);
        target.CopyTo(combined, source.Length);
        BitConverter.GetBytes((int)kind).CopyTo(combined, source.Length + target.Length);
        return System.Security.Cryptography.MD5.HashData(combined);
    }
}

public class UnityEventInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<UnityEventField> UnityEvents { get; set; } = [];
    public List<SerializableEventField> SerializableEvents { get; set; } = [];
}

public class UnityEventField
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public List<UnityEventListener> Listeners { get; set; } = [];
}

public class UnityEventListener
{
    public string TargetScript { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
}

public class SerializableEventField
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}
