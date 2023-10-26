
namespace UnitTests;

using GitExtras;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Ending = GitExtras.FixEol.Ending;
using FileStats = GitExtras.FixEol.FileStats;

public class FixEolTests
{
    private const int lines = 3;
    private const string baseLine = "az";
    
    [Fact]
    public void DocFullCr_StillIsCr()
    {
        var path = "myfile.cs";
        var original = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Cr).AddEmpty(Ending.Cr).Done();
        var local = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Cr).AddEmpty(Ending.Cr).Done();
        using var provider = new Provider().Add(path, original, local);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, false, false);
        Assert.Equal(FileStats.Mac(lines+1), result.OriginalStats);
        Assert.Equal(FileStats.Mac(lines+1), result.LocalStats);
        Assert.False(result.CanBeFixed);
    }

    [Fact]
    public void DocFullCrLf_StillIsCrLf()
    {
        var path = "myfile.cs";
        var original = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.CrLf).Done();
        var local = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.CrLf).Done();
        using var provider = new Provider().Add(path, original, local);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, false, false);
        Assert.Equal(FileStats.Windows(lines), result.OriginalStats);
        Assert.Equal(FileStats.Windows(lines), result.LocalStats);
        Assert.False(result.CanBeFixed);
    }

    [Fact]
    public void DocFullLf_StillIsLf()
    {
        var path = "myfile.cs";
        var original = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Lf).Done();
        var local = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Lf).Done();
        using var provider = new Provider().Add(path, original, local);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, false, false);
        Assert.Equal(FileStats.Unix(lines), result.OriginalStats);
        Assert.Equal(FileStats.Unix(lines), result.LocalStats);
        Assert.False(result.CanBeFixed);
    }

    [Fact]
    public void DocIsMixed_StillMixedTheSame()
    {
        // TODO: stats can be same but ends can have been changed!
        var path = "myfile.cs";
        var original = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Lf).Add(lines, Ending.Cr).Add(lines, Ending.CrLf).Done();
        var local = new DocumentGenerator(Encoding.UTF8).Add(lines, Ending.Lf).Add(lines, Ending.Cr).Add(lines, Ending.CrLf).Done();
        using var provider = new Provider().Add(path, original, local);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, false, false);
        Assert.False(result.CanBeFixed);
    }

    [Fact]
    public void DocFullCrLf_IsNowLf()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).AddEmpty(Ending.CrLf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Lf).AddEmpty(Ending.Lf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Windows(lines+1), result.OriginalStats);
        Assert.Equal(FileStats.Unix(lines+1), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullCrLf_IsNowCr()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Windows(lines), result.OriginalStats);
        Assert.Equal(FileStats.Mac(lines), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullCrLf_IsNowMixed()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).Add(lines, Ending.CrLf).Add(lines, Ending.CrLf).Add(lines, Ending.CrLf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).Add(lines, Ending.Cr).Add(lines, Ending.Lf).Add(lines, Ending.CrLf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Windows(4*lines), result.OriginalStats);
        Assert.Equal(new FileStats(lines, 2*lines, lines, 0), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullLf_IsNowCr()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Unix(lines), result.OriginalStats);
        Assert.Equal(FileStats.Mac(lines), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullLf_IsNowCrLf()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Unix(lines), result.OriginalStats);
        Assert.Equal(FileStats.Windows(lines), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullLf_IsNowMixed()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Add(lines, Ending.Lf).Add(lines, Ending.Lf).Add(lines, Ending.Lf).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Add(lines, Ending.CrLf).Add(lines, Ending.Cr).Add(lines, Ending.Lf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(FileStats.Unix(4*lines), result.OriginalStats);
        Assert.Equal(new FileStats(2 * lines, lines, lines, 0), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullCr_IsNowLf()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullCr_IsNowCrLf()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.CrLf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    [Fact]
    public void DocFullCr_IsNowMixed()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Add(lines, Ending.Cr).Add(lines, Ending.Cr).Add(lines, Ending.Cr).Done();
        var local = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Add(lines, Ending.CrLf).Add(lines, Ending.Lf).Add(lines, Ending.Cr).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    // TODO: already mixed
    
    [Fact]
    public void DocWasMixed_IsNowLf()
    {
        var path = "myfile.cs";
        var encoding = Encoding.UTF8;
        var original = new DocumentGenerator(encoding).Add(lines, Ending.Cr).Add(lines, Ending.CrLf).Add(lines, Ending.Lf).AddEmpty(Ending.CrLf).Done();
        var local    = new DocumentGenerator(encoding).Add(lines, Ending.Lf).Add(lines, Ending.Lf  ).Add(lines, Ending.Lf).AddEmpty(Ending.Lf).Done();
        var updatedFile = new ResetMemoryStream();
        using var provider = new Provider().Add(path, original, local, updatedFile);
        var target = FixEol.Create(provider);
        var result = target.ExecuteFile(path, true, false);
        Assert.Equal(new FileStats(lines, lines+1, lines, 0), result.OriginalStats);
        Assert.Equal(FileStats.Unix(3*lines+1), result.LocalStats);
        Assert.True(result.CanBeFixed);
        
        // verify new file
        updatedFile.Position = 0L;
        var newStats = FixEol.GetStats(updatedFile, encoding);

        // this helped find a bug. there was a strange char is added during fix
        // Expected: LF:3;CRLF:3;CR:3
        // Actual:   LF:3;CRLF:3;CR:3;XX:1
        ////Assert.Equal(new FileStats(lines, lines, lines, 0), newStats);
        Assert.Equal(BitConverter.ToString(original.ToArray()), BitConverter.ToString(updatedFile.ToArray()));
    }

    public class GetStatsMethod
    {
        [Fact]
        public void LastLineEmpty()
        {
            var contents = "xxxxx\n\n";
            var encoding = Encoding.UTF8;
            var stream = new MemoryStream(encoding.GetBytes(contents));
            var result = FixEol.GetStats(stream, encoding);
            Assert.Equal(FileStats.Unix(2), result);
        }

        [Fact]
        public void LastLineNotEmpty()
        {
            var contents = "xxxxx\nnnn";
            var encoding = Encoding.UTF8;
            var stream = new MemoryStream(encoding.GetBytes(contents));
            var result = FixEol.GetStats(stream, encoding);
            Assert.Equal(FileStats.Unix(1), result);
        }
    }
    
    sealed class Provider : IFixEolProvider, IDisposable
    {
        private readonly Dictionary<string, Tuple<Stream, Stream, Stream>> entries = new ();

        public void InitializeRepository(string directory)
        {
        }

        public IEnumerable<string> GetPendingFiles()
        {
            throw new NotSupportedException();
        }

        public Stream GetTipStream(string path)
        {
            var entry = this.entries[path];
            return entry.Item1;
        }

        public Stream OpenLocalFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            var entry = this.entries[path];
            return access == FileAccess.Write ? entry.Item3 : entry.Item2;
        }

        public void LocalFileCopy(string from, string to)
        {
            throw new NotSupportedException();
        }

        public Provider Add(string path, MemoryStream git, MemoryStream local)
        {
            this.entries.Add(path, new Tuple<Stream, Stream, Stream>(git, local, null));
            return this;
        }

        public Provider Add(string path, MemoryStream git, MemoryStream local, Stream updated)
        {
            this.entries.Add(path, new Tuple<Stream, Stream, Stream>(git, local, updated));
            return this;
        }

        public void Dispose()
        {
            foreach (var entry in entries)
            {
                if (entry.Value.Item1 is ResetMemoryStream reset1)
                {
                    reset1.RealDispose();
                }

                if (entry.Value.Item2 is ResetMemoryStream reset2)
                {
                    reset2.RealDispose();
                }

                if (entry.Value.Item3 is ResetMemoryStream reset3)
                {
                    reset3.RealDispose();
                }
            }
        }
    }

    class DocumentGenerator
    {
        private readonly MemoryStream file = new MemoryStream();
        private StreamWriter writer;
        private bool isDone;
        public Encoding Encoding { get; }

        public DocumentGenerator(Encoding encoding)
        {
            this.Encoding = encoding;
            this.writer = new StreamWriter(this.file, this.Encoding, 1024, leaveOpen: true);
        }

        public DocumentGenerator Add(int lines, Ending ending)
        {
            this.CheckIsNotDone();

            for (int i = 0; i < lines; i++)
            {
                ////if ((i % 5) == 0)
                ////{
                ////    this.WriteEnding(ending);
                ////}
                ////else
                {
                    this.writer.Write(baseLine);
                    this.WriteEnding(ending);
                }
            }

            return this;
        }

        public DocumentGenerator AddEmpty(Ending ending)
        {
            this.CheckIsNotDone();
            this.WriteEnding(ending);
            return this;
        }

        public MemoryStream Done()
        {
            this.CheckIsNotDone();

            this.writer.Flush();
            this.writer.Close();
            this.isDone = true;
            this.file.Position = 0L;
            return this.file;
        }

        private void CheckIsNotDone()
        {
            if (this.isDone)
            {
                throw new InvalidOperationException();
            }
        }

        private void WriteEnding(Ending ending)
        {
            switch (ending)
            {
                case Ending.CrLf:
                    this.writer.Write("\r\n");
                    break;
                case Ending.Cr:
                    this.writer.Write("\r");
                    break;
                case Ending.Lf:
                    this.writer.Write("\n");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ending), ending, null);
            }
        }
    }

    class ResetMemoryStream : MemoryStream
    {
        public override void Close()
        {
            this.Position = 0L;
        }

        protected override void Dispose(bool disposing)
        {
            this.Position = 0L;
        }

        public void RealDispose()
        {
            base.Dispose(true);
        }
    }
}
