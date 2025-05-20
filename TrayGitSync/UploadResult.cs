namespace TrayGitSync;

public class UploadResult
{
    public int TotalFilesChanged { get; set; }
    public long TotalBytesPushed { get; set; }
    
    public override string ToString()
    {
        return TotalFilesChanged switch
        {
            0 => "No files changed",
            _ => $"Upload complete. {TotalFilesChanged} files changed ({TotalBytesPushed.FormatBytesAsReadableString()})"
        };
    }
}