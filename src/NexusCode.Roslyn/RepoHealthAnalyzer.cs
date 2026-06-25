using NexusCode.Domain;

namespace NexusCode.Roslyn;

public sealed class RepoHealthAnalyzer
{
    private readonly MultiRepoManager _manager;

    public RepoHealthAnalyzer(MultiRepoManager manager)
    {
        _manager = manager;
    }

    public HealthReport Analyze(string repoName)
    {
        var repo = _manager.GetRepository(repoName);
        if (repo == null)
        {
            return new HealthReport { Score = 0, Error = "Repository not found" };
        }

        var report = new HealthReport
        {
            RepoName = repoName,
            TotalSymbols = repo.SymbolCount,
            TotalNodes = repo.NodeCount,
            TotalEdges = repo.EdgeCount
        };

        report.ClassCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Type).Count(s => s.TypeName == "Class");
        report.MethodCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Method).Count;
        report.PropertyCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Property).Count;
        report.FieldCount = repo.SymbolTable.GetByKind(Domain.SymbolKind.Field).Count;

        report.GraphDensity = report.TotalNodes > 1
            ? (double)report.TotalEdges / (report.TotalNodes * (report.TotalNodes - 1) / 2)
            : 0;

        report.AverageEdgesPerNode = report.TotalNodes > 0
            ? (double)report.TotalEdges / report.TotalNodes
            : 0;

        DetectLargeClasses(repo, report);
        DetectCircularDependencies(repo, report);
        DetectGodClasses(repo, report);

        report.Score = CalculateScore(report);

        return report;
    }

    private void DetectLargeClasses(RepoIndex repo, HealthReport report)
    {
        var classes = repo.SymbolTable.GetByKind(Domain.SymbolKind.Type)
            .Where(s => s.TypeName == "Class");

        foreach (var cls in classes)
        {
            var members = repo.SymbolTable.GetByContainer(cls.Id);
            if (members.Count > 30)
            {
                report.Warnings.Add($"Large class: {cls.Name} has {members.Count} members");
            }
        }
    }

    private void DetectCircularDependencies(RepoIndex repo, HealthReport report)
    {
        var visited = new HashSet<byte[]>();
        var inStack = new HashSet<byte[]>();

        foreach (var node in repo.Graph.GetNodesByKind(NodeKind.Class))
        {
            if (DetectCycleDFS(node.Id, repo.Graph, visited, inStack))
            {
                report.Warnings.Add($"Potential circular dependency detected involving: {node.Label}");
                break;
            }
        }
    }

    private bool DetectCycleDFS(byte[] nodeId, KnowledgeGraph graph, HashSet<byte[]> visited, HashSet<byte[]> inStack)
    {
        if (inStack.Any(x => x.SequenceEqual(nodeId)))
            return true;

        if (visited.Any(x => x.SequenceEqual(nodeId)))
            return false;

        visited.Add(nodeId);
        inStack.Add(nodeId);

        var edges = graph.GetOutgoingEdges(nodeId);
        foreach (var edge in edges)
        {
            if (edge.Kind == EdgeKind.Inherits || edge.Kind == EdgeKind.Implements || edge.Kind == EdgeKind.DependsOn)
            {
                if (DetectCycleDFS(edge.TargetId, graph, visited, inStack))
                    return true;
            }
        }

        inStack.RemoveWhere(x => x.SequenceEqual(nodeId));
        return false;
    }

    private void DetectGodClasses(RepoIndex repo, HealthReport report)
    {
        var classes = repo.SymbolTable.GetByKind(Domain.SymbolKind.Type)
            .Where(s => s.TypeName == "Class");

        foreach (var cls in classes)
        {
            int outgoingEdges = repo.Graph.GetOutgoingEdges(cls.Id.ToByteArray())
                .Count(e => e.Kind == EdgeKind.Calls || e.Kind == EdgeKind.Uses);

            if (outgoingEdges > 20)
            {
                report.Warnings.Add($"God class detected: {cls.Name} has {outgoingEdges} outgoing dependencies");
            }
        }
    }

    private double CalculateScore(HealthReport report)
    {
        double score = 100;

        foreach (var warning in report.Warnings)
        {
            if (warning.Contains("Large class")) score -= 5;
            else if (warning.Contains("circular")) score -= 15;
            else if (warning.Contains("God class")) score -= 10;
            else score -= 3;
        }

        if (report.GraphDensity > 0.5)
        {
            report.Suggestions.Add("High graph density - consider reducing coupling");
            score -= 5;
        }

        if (report.TotalSymbols > 10000)
        {
            report.Suggestions.Add("Large codebase - consider modularization");
        }

        return Math.Max(0, Math.Min(100, score));
    }
}

public class HealthReport
{
    public string RepoName { get; set; } = string.Empty;
    public double Score { get; set; }
    public int TotalSymbols { get; set; }
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
    public int ClassCount { get; set; }
    public int MethodCount { get; set; }
    public int PropertyCount { get; set; }
    public int FieldCount { get; set; }
    public double GraphDensity { get; set; }
    public double AverageEdgesPerNode { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Suggestions { get; set; } = [];
    public string? Error { get; set; }
}
