namespace GitExtras;

using LibGit2Sharp;

class FixEolProvider : IFixEolProvider
{
    private Repository repo;

    public void InitializeRepository(string directory)
    {
        this.repo = new Repository(directory);
    }

    public IEnumerable<string> GetPendingFiles()
    {
        var list = new List<string>();
        var status = this.repo.RetrieveStatus();
        foreach (var entry in status)
        {
            var qualifies = false;
            if ((entry.State & FileStatus.ModifiedInWorkdir) == FileStatus.ModifiedInWorkdir)
            {
                qualifies = true;
            }
            else if ((entry.State & FileStatus.ModifiedInIndex) == FileStatus.ModifiedInIndex)
            {
                qualifies = true;
            }
            else
            {
                ////Console.WriteLine(entry.State + " " + entry.FilePath);
            }

            if (qualifies)
            {
                if (!list.Contains(entry.FilePath))
                {
                    list.Add(entry.FilePath);
                    yield return entry.FilePath;
                }
            }
        }
    }

    public Stream? GetTipStream(string path)
    {
        var tip = this.repo.Head.Tip[path];
        if (tip == null)
        {
            return null;
        }
        
        if (!(tip.Target is Blob blob))
        {
            return null;
        }

        // read file from git database
        return blob.GetContentStream();
    }

    public Stream OpenLocalFile(string path, FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream(path, mode, access, share);
    }

    public void LocalFileCopy(string from, string to)
    {
        File.Copy(from, to);
    }
}
