namespace GitExtras;

public interface IFixEolProvider
{
    void InitializeRepository(string directory);
    IEnumerable<string> GetPendingFiles();
    Stream? GetTipStream(string path);
    Stream OpenLocalFile(string path, FileMode mode, FileAccess access, FileShare share);
    void LocalFileCopy(string from, string to);
}
