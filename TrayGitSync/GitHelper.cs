using System.Diagnostics;

namespace TrayGitSync;

public static class GitHelper
{
    public static void Upload(GitSyncConfig config)
    {
        InitRepoAndSubmodules(config);

        foreach (var sub in config.Submodules)
        {
            var subPath = Path.Combine(config.LocalRepoPath, sub.Name);

            RunGitCommand("add .", subPath);
            RunGitCommand($"commit -m \"Submodule update: {DateTime.Now}\"", subPath);
            RunGitCommand("push origin main", subPath);
        }

        RunGitCommand("add .", config.LocalRepoPath);
        RunGitCommand($"commit -m \"Main repo submodule updates at {DateTime.Now}\"", config.LocalRepoPath);
        RunGitCommand("push origin main", config.LocalRepoPath);
    }

    public static void Download(GitSyncConfig config)
    {
        InitRepoAndSubmodules(config);

        var output = RunGitCommand("pull origin main", config.LocalRepoPath);
        if (output.Contains("CONFLICT"))
            throw new Exception("Main repo merge conflict detected.");

        RunGitCommand("submodule update --init --recursive", config.LocalRepoPath);

        foreach (var sub in config.Submodules)
        {
            var subPath = Path.Combine(config.LocalRepoPath, sub.Name);
            var subOut = RunGitCommand("pull origin main", subPath);
            if (subOut.Contains("CONFLICT"))
                throw new Exception($"Merge conflict in submodule {sub.Name}. Aborting.");
        }
    }

    private static void InitRepoAndSubmodules(GitSyncConfig config)
    {
        if (!Directory.Exists(config.LocalRepoPath))
        {
            RunGitCommand($"clone {config.RepositoryUrl} \"{config.LocalRepoPath}\" --recurse-submodules", Directory.GetParent(config.LocalRepoPath).FullName);
        }

        foreach (var sub in config.Submodules)
        {
            var subPath = Path.Combine(config.LocalRepoPath, sub.Name);

            if (!Directory.Exists(subPath))
            {
                RunGitCommand($"submodule add {sub.Url} {sub.Name}", config.LocalRepoPath);
                RunGitCommand("submodule update --init --recursive", config.LocalRepoPath);
            }
        }
    }

    private static string RunGitCommand(string args, string workingDir)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi) ?? throw new Exception("Unable to run git");
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Git error in {workingDir}: {output}");

        return output;
    }
}