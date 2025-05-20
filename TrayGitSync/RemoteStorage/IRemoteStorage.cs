namespace TrayGitSync;

public class RemoteStorageProgressEventArgs(
    string repositoryName,
    bool isComplete,
    string message,
    float percentComplete)
    : EventArgs
{
    public string RepositoryName { get; } = repositoryName;
    public bool IsComplete { get; } = isComplete;
    public string Message { get; } = message;
    public float PercentComplete { get; } = percentComplete;
}

public class RemoteStorageInitializedEventArgs(
    string[] repositories
) : EventArgs
{
    public string[] Repositories { get; } = repositories;
}

public class FatalErrorEventArgs(Exception ex) : EventArgs
{
    public Exception Exception { get; } = ex;
}

public interface IRemoteStorage
{
    public event EventHandler<RemoteStorageProgressEventArgs>? OnRemoteStorageProgress;
    public event EventHandler<RemoteStorageInitializedEventArgs>? OnRemoteStorageInitialized;
    public event EventHandler<FatalErrorEventArgs>? OnFatalError;
    abstract UploadResult Upload(Configuration config);
    abstract void Download(Configuration config);
}