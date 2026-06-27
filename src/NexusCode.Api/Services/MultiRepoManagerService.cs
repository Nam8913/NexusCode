using NexusCode.Roslyn;

namespace NexusCode.Api.Services;

public sealed class MultiRepoManagerService
{
    private readonly MultiRepoManager _manager = new();

    public MultiRepoManager Manager => _manager;
}
