using Microsoft.AspNetCore.Mvc;
using NexusCode.Api.Services;
using NexusCode.Roslyn;

namespace NexusCode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MultiRepoController : ControllerBase
{
    private static readonly MultiRepoManager _manager = new();
    private static readonly CrossRepoSearchEngine _crossRepoSearch = new(_manager);
    private static readonly RepoComparator _comparator = new(_manager);
    private static readonly RepoHealthAnalyzer _healthAnalyzer = new(_manager);

    [HttpPost("index")]
    public async Task<IActionResult> IndexRepositories([FromBody] IndexMultipleRequest request, CancellationToken ct)
    {
        var results = await _manager.IndexMultipleAsync(request.Paths, ct);
        return Ok(results);
    }

    [HttpGet("list")]
    public IActionResult ListRepositories()
    {
        var repos = _manager.GetAllRepositories();
        return Ok(repos.Select(r => new
        {
            r.Name,
            r.Path,
            r.IndexedAt,
            r.FileCount,
            r.SymbolCount,
            r.NodeCount,
            r.EdgeCount
        }));
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string query, [FromQuery] int maxResults = 20)
    {
        var results = _crossRepoSearch.Search(query, maxResults);
        return Ok(results.Select(r => new
        {
            r.Symbol.Name,
            r.Symbol.FullName,
            Kind = r.Symbol.Kind.ToString(),
            r.Symbol.FilePath,
            r.Score,
            r.Repository
        }));
    }

    [HttpGet("callers/{symbolName}")]
    public IActionResult FindCallers(string symbolName, [FromQuery] int maxDepth = 1)
    {
        var results = _crossRepoSearch.FindCallers(symbolName, maxDepth);
        return Ok(results.Select(r => new
        {
            r.Symbol.Name,
            r.Symbol.FullName,
            r.Repository,
            r.Relation,
            r.Score
        }));
    }

    [HttpGet("callees/{symbolName}")]
    public IActionResult FindCallees(string symbolName, [FromQuery] int maxDepth = 1)
    {
        var results = _crossRepoSearch.FindCallees(symbolName, maxDepth);
        return Ok(results.Select(r => new
        {
            r.Symbol.Name,
            r.Symbol.FullName,
            r.Repository,
            r.Relation,
            r.Score
        }));
    }

    [HttpGet("compare")]
    public IActionResult Compare([FromQuery] string repo1, [FromQuery] string repo2)
    {
        var result = _comparator.Compare(repo1, repo2);
        if (result.Error != null)
            return BadRequest(new { error = result.Error });

        return Ok(result);
    }

    [HttpGet("health/{repoName}")]
    public IActionResult GetHealth(string repoName)
    {
        var report = _healthAnalyzer.Analyze(repoName);
        if (report.Error != null)
            return NotFound(new { error = report.Error });

        return Ok(report);
    }

    [HttpDelete("{repoName}")]
    public IActionResult RemoveRepository(string repoName)
    {
        _manager.RemoveRepository(repoName);
        return Ok(new { message = $"Repository '{repoName}' removed" });
    }
}

public class IndexMultipleRequest
{
    public string[] Paths { get; set; } = [];
}
