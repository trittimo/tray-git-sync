namespace TrayGitSync.Exceptions;

public class GitSyncException(string message) : Exception(message);

public class GitCommandException(string command, string workingDir, string output)
    : GitSyncException($"Git command '{command}' failed in '{workingDir}': {output}");

public class MergeConflictException(string repoName, string path)
    : GitSyncException($"Merge conflict in repo '{repoName}' at '{path}'");

public class RepositoryPathNotFoundException(string repoName, string machine)
    : GitSyncException($"Repository '{repoName}' does not have a local path specified on this machine ({machine})");

public class InvalidRepositoryLocationException(string path)
    : GitSyncException($"Path '{path}' exists, but it is not a git repository");

public class GitStartException() : GitSyncException("Unable to start git process");

public class MissingIdRsaException(string machine) : GitSyncException($"Missing id_rsa file for machine '{machine}'");