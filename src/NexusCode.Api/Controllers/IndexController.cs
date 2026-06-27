using Microsoft.AspNetCore.Mvc;
using NexusCode.Api.Services;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly NexusIndexService _indexService;

    public IndexController(NexusIndexService indexService)
    {
        _indexService = indexService;
    }

    [HttpPost("repository")]
    public async Task<IActionResult> IndexRepository([FromBody] IndexRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new { error = "Repository path is required" });

        if (!Directory.Exists(request.Path))
            return NotFound(new { error = $"Directory not found: {request.Path}" });

        var result = await _indexService.IndexRepositoryAsync(request.Path, progress: null, ct);

        if (!result.Success)
            return StatusCode(500, new { error = result.Error, result });

        return Ok(result);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            indexed = _indexService.IsIndexed,
            symbols = _indexService.SymbolTable.Count,
            graphNodes = _indexService.Graph.NodeCount,
            graphEdges = _indexService.Graph.EdgeCount
        });
    }
}

public class IndexRequest
{
    public string Path { get; set; } = string.Empty;
}
