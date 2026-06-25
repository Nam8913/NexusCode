using Microsoft.AspNetCore.Mvc;
using NexusCode.Api.Services;

namespace NexusCode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly NexusIndexService _indexService;

    public RagController(NexusIndexService indexService)
    {
        _indexService = indexService;
    }

    [HttpPost("ask")]
    public IActionResult Ask([FromBody] RagRequest request)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required" });

        var result = _indexService.GraphRAG(request.Question);

        return Ok(new
        {
            question = result.Question,
            evidenceCount = result.Evidence.Count,
            evidence = result.Evidence.Select(e => new
            {
                symbol = new { e.Symbol.Name, e.Symbol.FullName, Kind = e.Symbol.Kind.ToString(), e.Symbol.FilePath },
                score = e.Score,
                source = e.Source
            }),
            tokenCount = result.TokenCount,
            prompt = result.Prompt
        });
    }
}

public class RagRequest
{
    public string Question { get; set; } = string.Empty;
}
