using System.Diagnostics;
using TrayGitSync.Exceptions;

namespace TrayGitSync;

public class RemoteStorageGit : IRemoteStorage
{
    public event EventHandler<RemoteStorageProgressEventArgs>? OnRemoteStorageProgress;
    public event EventHandler<RemoteStorageInitializedEventArgs>? OnRemoteStorageInitialized;
    public event EventHandler<FatalErrorEventArgs>? OnFatalError;

    private void OnError(Exception ex)
    {
        OnFatalError?.Invoke(null, new FatalErrorEventArgs(ex));
    }

    private void OnInitialized(string[] repositories)
    {
        OnRemoteStorageInitialized?.Invoke(null, new RemoteStorageInitializedEventArgs(repositories));
    }

    private void OnProgress(string repositoryName, bool isComplete, string message, float percentComplete)
    {
        OnRemoteStorageProgress?.Invoke(null, new RemoteStorageProgressEventArgs(repositoryName, isComplete, message, percentComplete));
    }


    public UploadResult Upload(Configuration config)
    {
        var result = new UploadResult();

        OnInitialized(config.Repositories.Select(r => r.Name).ToArray());

        foreach (var repo in config.Repositories)
        {
            try
            {
                string path = ResolveRepoPath(repo);
        
                EnsureGitInitialized(path, repo.RemoteUrl, config);

                var status = RunGitCommand("status --porcelain", path, config);
                var unpushedStatus = RunGitCommand("log @{u}..HEAD --oneline", path, config);
                
                if (string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(unpushedStatus))
                {
                    OnProgress(repo.Name, true, "No changes to upload", 100);;
                    continue;
                }
                
                OnProgress(repo.Name, false, "Uploading changes", 0);;
                            
                var filesChanged = status.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                var unpushedCommits = unpushedStatus.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                result.TotalFilesChanged += filesChanged + unpushedCommits;
                
                var message = filesChanged > 0 ? $"{filesChanged} changed files" : "";
                if (unpushedCommits > 0)
                {
                    message = message.Length > 0 ? $"{message} and {unpushedCommits} unpushed commits" : $"{unpushedCommits} unpushed commits";
                }
                
                OnProgress(repo.Name, false, $"Adding and committing {message}", 5);
                RunGitCommand("add .", path, config);
                RunGitCommand($"commit -m \"Auto-upload from {Environment.MachineName} at {DateTime.Now}\"", path, config);
                OnProgress(repo.Name, false, $"Uploading changes", percentComplete: 15);
                var bytesPushed = ParsePushOutput(RunGitCommand("push --progress", path, config));
                result.TotalBytesPushed += bytesPushed;
                OnProgress(repo.Name, true, $"Uploaded {bytesPushed.FormatBytesAsReadableString()}", percentComplete: 100);
            }
            catch (Exception e)
            {
                OnError(e);
                throw;
            }
        }

        return result;
    }


    public void Download(Configuration config)
    {
        foreach (var repo in config.Repositories)
        {
            var path = ResolveRepoPath(repo);

            EnsureGitInitialized(path, repo.RemoteUrl, config);

            var result = RunGitCommand("pull", path, config);
            if (result.Contains("CONFLICT"))
                throw new MergeConflictException(repo.Name, path);
        }
    }

    private static string ResolveRepoPath(Repository repo)
    {
        var machine = Environment.MachineName.ToUpper().Trim();
        if (!repo.MachinePaths.TryGetValue(machine, out var localPath))
        {
            throw new RepositoryPathNotFoundException(repo.Name, machine);
        }
        return localPath;
    }

    private static void EnsureGitInitialized(string path, string remoteUrl, Configuration config)
    {
        var directoryExists = Directory.Exists(path);
        var isGitRepo = Directory.Exists(Path.Combine(path, ".git"));
        if (directoryExists && isGitRepo)
        {
            return;
        }

        if (directoryExists)
        {
            throw new InvalidRepositoryLocationException(path);
        }

        Directory.CreateDirectory(path);
        RunGitCommand("init", path, config);
        RunGitCommand($"remote add origin {remoteUrl}", path, config);
    }

    private static long ParsePushOutput(string output)
    {
        if (string.IsNullOrEmpty(output)) return 0;
        
        var match = System.Text.RegularExpressions.Regex.Match(output, @"Writing objects:.+?(\d+(?:\.\d+)?)\s+(bytes|KiB|MiB|GiB|TiB)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return 0;
        
        var value = double.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLower();
        
        return unit switch
        {
            "kib" => (long)(value * 1024),
            "mib" => (long)(value * 1024 * 1024),
            "gib" => (long)(value * 1024 * 1024 * 1024),
            "tib" => (long)(value * 1024 * 1024 * 1024 * 1024), // Not technically possible (https://github.com/git/git/blob/cb96e1697ad6e54d11fc920c95f82977f8e438f8/strbuf.c#L839)
            _ => (long)value
        };
    }
    
    private static string RunGitCommand(string args, string workingDir, Configuration config)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = workingDir
        };

        using var process = Process.Start(psi) ?? throw new GitStartException();
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new GitCommandException(args, workingDir, output);

        return output;
    }
}