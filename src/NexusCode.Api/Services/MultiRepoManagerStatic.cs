using NexusCode.Roslyn;

namespace NexusCode.Api.Services;

public static class MultiRepoManagerStatic
{
    private static readonly Lazy<MultiRepoManager> _instance = new(() => new MultiRepoManager());
    public static MultiRepoManager Instance => _instance.Value;
}
