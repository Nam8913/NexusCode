using Microsoft.AspNetCore.Mvc;
using NexusCode.Api.Services;
using NexusCode.Domain;

namespace NexusCode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphController : ControllerBase
{
    private readonly NexusIndexService _indexService;

    public GraphController(NexusIndexService indexService)
    {
        _indexService = indexService;
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var stats = new
        {
            nodes = _indexService.Graph.NodeCount,
            edges = _indexService.Graph.EdgeCount,
            symbols = _indexService.SymbolTable.Count,
            nodeKinds = Enum.GetValues<NodeKind>()
                .Select(k => new { kind = k.ToString(), count = _indexService.Graph.GetNodesByKind(k).Count })
                .Where(x => x.count > 0)
                .OrderByDescending(x => x.count)
        };

        return Ok(stats);
    }

    [HttpGet("nodes/{kind}")]
    public IActionResult GetNodesByKind(string kind)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        if (!Enum.TryParse<NodeKind>(kind, true, out var nodeKind))
            return BadRequest(new { error = $"Invalid node kind: {kind}" });

        var nodes = _indexService.Graph.GetNodesByKind(nodeKind);
        return Ok(nodes.Select(n => new { n.Label, n.FullName, Kind = n.Kind.ToString(), n.Metadata }));
    }

    [HttpGet("edges/{kind}")]
    public IActionResult GetEdgesByKind(string kind)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        if (!Enum.TryParse<EdgeKind>(kind, true, out var edgeKind))
            return BadRequest(new { error = $"Invalid edge kind: {kind}" });

        var edges = _indexService.Graph.GetEdgesByKind(edgeKind);
        return Ok(edges.Select(e => new
        {
            Source = Convert.ToBase64String(e.SourceId),
            Target = Convert.ToBase64String(e.TargetId),
            e.Kind,
            e.Weight
        }));
    }

    [HttpGet("export")]
    public IActionResult Export()
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var nodes = new List<object>();
        foreach (var kind in Enum.GetValues<NodeKind>())
        {
            var kindNodes = _indexService.Graph.GetNodesByKind(kind);
            foreach (var n in kindNodes)
            {
                nodes.Add(new
                {
                    id = Convert.ToBase64String(n.Id),
                    label = n.Label,
                    kind = n.Kind.ToString(),
                    color = ColorConfig.GetNodeColor(n.Kind.ToString()),
                    size = n.Kind == NodeKind.Class || n.Kind == NodeKind.Interface ? 10 : 5,
                    metadata = n.Metadata
                });
            }
        }

        var edges = new List<object>();
        foreach (var kind in Enum.GetValues<EdgeKind>())
        {
            var kindEdges = _indexService.Graph.GetEdgesByKind(kind);
            foreach (var e in kindEdges)
            {
                var sourceNode = _indexService.Graph.GetNode(e.SourceId);
                var targetNode = _indexService.Graph.GetNode(e.TargetId);
                edges.Add(new
                {
                    id = Convert.ToBase64String(e.Id),
                    source = Convert.ToBase64String(e.SourceId),
                    target = Convert.ToBase64String(e.TargetId),
                    sourceLabel = sourceNode?.Label ?? sourceNode?.FullName ?? $"External({e.Kind})",
                    targetLabel = targetNode?.Label ?? targetNode?.FullName ?? $"External({e.Kind})",
                    kind = e.Kind.ToString(),
                    color = ColorConfig.GetEdgeColor(e.Kind.ToString())
                });
            }
        }

        return Ok(new { nodes, edges });
    }

    [HttpGet("mermaid")]
    public IActionResult GetMermaidDiagram([FromQuery] string? type = null)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("graph TD");

        var nodes = _indexService.Graph.GetNodesByKind(NodeKind.Class)
            .Concat(_indexService.Graph.GetNodesByKind(NodeKind.Interface))
            .Concat(_indexService.Graph.GetNodesByKind(NodeKind.Method))
            .Take(50);

        foreach (var node in nodes)
        {
            var id = Convert.ToBase64String(node.Id)[..8];
            sb.AppendLine($"    {id}[\"{node.Label}\"]");
        }

        var edges = _indexService.Graph.GetEdgesByKind(EdgeKind.Inherits)
            .Concat(_indexService.Graph.GetEdgesByKind(EdgeKind.Implements))
            .Concat(_indexService.Graph.GetEdgesByKind(EdgeKind.Calls))
            .Take(100);

        foreach (var edge in edges)
        {
            var source = Convert.ToBase64String(edge.SourceId)[..8];
            var target = Convert.ToBase64String(edge.TargetId)[..8];
            sb.AppendLine($"    {source} --> {target}");
        }

        return Ok(new { mermaid = sb.ToString() });
    }
}
