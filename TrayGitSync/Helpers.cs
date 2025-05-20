namespace TrayGitSync;

public static class Helpers
{
    public static string FormatBytesAsReadableString(this long bytes)
    {
        return bytes switch
        {
            >= 1_099_511_627_776 => $"{bytes / 1_099_511_627_776.0:0.##} TB",
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:0.##} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:0.##} MB",
            >= 1024 => $"{bytes / 1024.0:0.##} KB",
            _ => $"{bytes} B"
        };
    }
}