using NexusCode.Domain;
using NexusCode.Roslyn;
using Xunit;

namespace NexusCode.Tests;

public class KnowledgeGraphTests
{
    [Fact]
    public void AddNode_And_GetNode_ReturnsNode()
    {
        var graph = new KnowledgeGraph();
        var node = CreateNode("Test.Class", NodeKind.Class);

        graph.AddNode(node);

        var result = graph.GetNode(node.Id);
        Assert.NotNull(result);
        Assert.Equal("Test.Class", result!.FullName);
    }

    [Fact]
    public void AddEdge_CreatesRelationship()
    {
        var graph = new KnowledgeGraph();
        var source = CreateNode("A", NodeKind.Class);
        var target = CreateNode("B", NodeKind.Class);

        graph.AddNode(source);
        graph.AddNode(target);

        var edge = CreateEdge(source.Id, target.Id, EdgeKind.Inherits);
        graph.AddEdge(edge);

        var outgoing = graph.GetOutgoingEdges(source.Id);
        Assert.Single(outgoing);
        Assert.Equal(EdgeKind.Inherits, outgoing[0].Kind);
    }

    [Fact]
    public void GetIncomingEdges_FindsCallers()
    {
        var graph = new KnowledgeGraph();
        var caller = CreateNode("Caller", NodeKind.Method);
        var callee = CreateNode("Callee", NodeKind.Method);

        graph.AddNode(caller);
        graph.AddNode(callee);

        graph.AddEdge(CreateEdge(caller.Id, callee.Id, EdgeKind.Calls));

        var incoming = graph.GetIncomingEdges(callee.Id);
        Assert.Single(incoming);
        Assert.True(caller.Id.SequenceEqual(incoming[0].SourceId));
    }

    [Fact]
    public void RemoveNode_RemovesConnectedEdges()
    {
        var graph = new KnowledgeGraph();
        var node = CreateNode("ToRemove", NodeKind.Class);
        var other = CreateNode("Other", NodeKind.Class);

        graph.AddNode(node);
        graph.AddNode(other);
        graph.AddEdge(CreateEdge(node.Id, other.Id, EdgeKind.Calls));
        graph.AddEdge(CreateEdge(other.Id, node.Id, EdgeKind.Calls));

        graph.RemoveNode(node.Id);

        Assert.Null(graph.GetNode(node.Id));
        Assert.Empty(graph.GetOutgoingEdges(other.Id));
    }

    [Fact]
    public void GetNodesByKind_FiltersCorrectly()
    {
        var graph = new KnowledgeGraph();
        graph.AddNode(CreateNode("C1", NodeKind.Class));
        graph.AddNode(CreateNode("C2", NodeKind.Class));
        graph.AddNode(CreateNode("M1", NodeKind.Method));

        var classes = graph.GetNodesByKind(NodeKind.Class);
        var methods = graph.GetNodesByKind(NodeKind.Method);

        Assert.Equal(2, classes.Count);
        Assert.Single(methods);
    }

    [Fact]
    public void NodeCount_And_EdgeCount_AreCorrect()
    {
        var graph = new KnowledgeGraph();
        Assert.Equal(0, graph.NodeCount);
        Assert.Equal(0, graph.EdgeCount);

        var n1 = CreateNode("A", NodeKind.Class);
        var n2 = CreateNode("B", NodeKind.Class);
        graph.AddNode(n1);
        graph.AddNode(n2);
        graph.AddEdge(CreateEdge(n1.Id, n2.Id, EdgeKind.Calls));

        Assert.Equal(2, graph.NodeCount);
        Assert.Equal(1, graph.EdgeCount);
    }

    private static GraphNodeEntity CreateNode(string fullName, NodeKind kind)
    {
        return new GraphNodeEntity
        {
            Id = Guid.NewGuid().ToByteArray(),
            FullName = fullName,
            Label = fullName.Split('.').Last(),
            Kind = kind
        };
    }

    private static GraphEdgeEntity CreateEdge(byte[] source, byte[] target, EdgeKind kind)
    {
        return new GraphEdgeEntity
        {
            Id = Guid.NewGuid().ToByteArray(),
            SourceId = source,
            TargetId = target,
            Kind = kind
        };
    }
}
