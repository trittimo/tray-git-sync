namespace TrayGitSync;

using System.Collections.Generic;

public class GitSyncConfig
{
    public required string RepositoryUrl { get; init; }
    public required string LocalRepoPath { get; init; }
    public required List<SubmoduleConfig> Submodules { get; init; }
}

public class SubmoduleConfig
{
    public required string Name { get; init; }
    public required string Url { get; init; }
}