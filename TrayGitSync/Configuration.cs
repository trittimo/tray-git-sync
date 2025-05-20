namespace TrayGitSync;

using System.Collections.Generic;

public class Configuration
{
    public required List<Repository> Repositories { get; init; }
}

public class Repository
{
    public required string Name { get; init; }
    public required string RemoteUrl { get; init; }
    public required Dictionary<string, string> MachinePaths { get; init; } 
}