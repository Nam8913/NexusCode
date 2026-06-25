using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexusCode.Roslyn;

namespace NexusCode.Api;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddNexusHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("roslyn", () =>
            {
                try
                {
                    var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class Test {}");
                    return HealthCheckResult.Healthy("Roslyn engine operational");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Roslyn engine error", ex);
                }
            })
            .AddCheck("memory", () =>
            {
                var usedBytes = GC.GetTotalMemory(false);
                var usedMB = usedBytes / (1024.0 * 1024.0);
                var status = usedMB < 1000 ? HealthStatus.Healthy : HealthStatus.Degraded;
                return new HealthCheckResult(status, $"Memory: {usedMB:F1} MB");
            });

        return services;
    }

    public static IEndpointRouteBuilder MapNexusHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        return endpoints;
    }
}
