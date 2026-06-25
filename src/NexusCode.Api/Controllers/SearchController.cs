using Microsoft.AspNetCore.Mvc;
using NexusCode.Api.Services;
using NexusCode.Domain;
using NexusCode.Roslyn;

namespace NexusCode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly NexusIndexService _indexService;

    public SearchController(NexusIndexService indexService)
    {
        _indexService = indexService;
    }

    [HttpGet("symbol")]
    public IActionResult SearchSymbol([FromQuery] string query, [FromQuery] string? kind = null, [FromQuery] int maxResults = 20)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        SymbolKind? kindFilter = kind?.ToLower() switch
        {
            "class" => SymbolKind.Type,
            "method" => SymbolKind.Method,
            "property" => SymbolKind.Property,
            "field" => SymbolKind.Field,
            _ => null
        };

        var results = _indexService.SearchEngine.FindSymbol(query, new SearchOptions { MaxResults = maxResults, KindFilter = kindFilter });
        return Ok(results.Select(r => new { r.Symbol.Name, r.Symbol.FullName, Kind = r.Symbol.Kind.ToString(), r.Symbol.FilePath, r.Symbol.StartLine, r.Score }));
    }

    [HttpGet("callers/{symbolName}")]
    public IActionResult FindCallers(string symbolName, [FromQuery] int maxDepth = 1)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var symbol = _indexService.SymbolTable.GetByFullName(symbolName);
        if (symbol == null)
            return NotFound(new { error = $"Symbol not found: {symbolName}" });

        var callers = _indexService.SearchEngine.FindCallers(symbol.Id, maxDepth);
        return Ok(callers.Select(c => new { c.Symbol.Name, c.Symbol.FullName, c.Symbol.FilePath, c.Depth }));
    }

    [HttpGet("callees/{symbolName}")]
    public IActionResult FindCallees(string symbolName, [FromQuery] int maxDepth = 1)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var symbol = _indexService.SymbolTable.GetByFullName(symbolName);
        if (symbol == null)
            return NotFound(new { error = $"Symbol not found: {symbolName}" });

        var callees = _indexService.SearchEngine.FindCallees(symbol.Id, maxDepth);
        return Ok(callees.Select(c => new { c.Symbol.Name, c.Symbol.FullName, c.Symbol.FilePath, c.Depth }));
    }

    [HttpGet("implementations/{interfaceName}")]
    public IActionResult FindImplementations(string interfaceName)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var iface = _indexService.SymbolTable.GetByFullName(interfaceName);
        if (iface == null)
            return NotFound(new { error = $"Interface not found: {interfaceName}" });

        var implementations = _indexService.SearchEngine.FindImplementations(iface.Id);
        return Ok(implementations.Select(i => new { i.Name, i.FullName, i.FilePath }));
    }

    [HttpGet("derived/{typeName}")]
    public IActionResult FindDerivedTypes(string typeName)
    {
        if (!_indexService.IsIndexed)
            return BadRequest(new { error = "Repository not indexed" });

        var type = _indexService.SymbolTable.GetByFullName(typeName);
        if (type == null)
            return NotFound(new { error = $"Type not found: {typeName}" });

        var derived = _indexService.SearchEngine.FindDerivedTypes(type.Id);
        return Ok(derived.Select(d => new { d.Name, d.FullName, d.FilePath }));
    }
}
