namespace GitExtras;

using LibGit2Sharp;
using Spectre.Console.Cli;
using System.Text;

/// <summary>
/// This command tries to restore mixed line endings in git working dire. 
/// </summary>
public class FixEol : Command<FixEol.Settings>
{
    private readonly IFixEolProvider provider;
    private static char cr = '\r', lf = '\n';

    public FixEol()
    {
        this.provider = new FixEolProvider();
    }

    private FixEol(IFixEolProvider provider)
    {
        this.provider = provider;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        public string[] Paths { get; set; }

        [CommandOption("-e|--execute")]
        public bool Execute { get; set; }

        [CommandOption("-b|--backup")]
        public bool Backup { get; set; }
    }
    
    public static FixEol Create(IFixEolProvider provider)
    {
        return new FixEol(provider);
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        this.provider.InitializeRepository(Environment.CurrentDirectory);

        // no path specified? ask pending file from git
        var paths = settings.Paths?.ToList() ?? new List<string>();
        if (paths.Count == 0)
        {
            paths.AddRange(this.provider.GetPendingFiles());
        }

        // handle each file
        int problemFiles = 0, fixedFiles = 0;
        foreach (var path in paths)
        {
            var result = ExecuteFile(path, settings.Execute, settings.Backup);
            problemFiles += result.CanBeFixed ? 1 : 0;
            fixedFiles += result.Proceed ? 1 : 0;
        }

        return (problemFiles - fixedFiles) == 0 ? 0 : 11;
    }

    public FileOperation ExecuteFile(string path, bool execute, bool backup)
    {
        var result = new FileOperation(path);
        var encoding = Encoding.UTF8;
        var preference = Ending.Lf;
        var explicitPathsOptions = new ExplicitPathsOptions();
        var compareOptions = new CompareOptions();
        compareOptions.Algorithm = DiffAlgorithm.Patience;
        compareOptions.Similarity = SimilarityOptions.Default;
        
        ////var compare = this.repo.Diff.Compare<Patch>(new string[] { path, }, false, explicitPathsOptions, compareOptions);
        var originalLines = new List<string>();
        result.IsFoundInGit = false;
        var tipStream = this.provider.GetTipStream(path);
        if (tipStream != null)
        {
            result.IsFoundInGit = true;
        }
        else
        {
            return result;
        }

        // read file from git database
        using (tipStream)
        using (var reader = new StreamReader(tipStream, encoding))
        {
            SpecialReadLines(reader, originalLines);
        }

        // read local file
        var newLines = new List<string>();
        using (var file = this.provider.OpenLocalFile(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new StreamReader(file, encoding))
        {
            SpecialReadLines(reader, newLines);
        }

        // compare line endings
        var originalStats = GetStats(originalLines);
        result.OriginalStats = originalStats;
        var newStats = GetStats(newLines);
        result.LocalStats = newStats;

        ////if (!originalStats.IsMixed && !newStats.IsMixed)
        ////{
        ////    preference = GetPreference(originalStats, preference);
        ////}

        // regenerate file in-memory
        var newContents = new StringBuilder();
        var derivedIndex = -1;
        int foundIndex;
        string foundLine;
        for (int i = 0; i < newLines.Count; i++)
        {
            var myLine = newLines[i];
            var myType = GetLineType(myLine);

            if (SearchLine(myLine, derivedIndex + 1, originalLines, out foundIndex, out foundLine))
            {
                var foundType = GetLineType(foundLine);
                derivedIndex = foundIndex;
                Append(myLine, myType, foundType, newContents);
            }
            else
            {
                derivedIndex++;
                Append(myLine, myType, preference, newContents);
            }
        }
        
        // analyze the new file
        var newNewLines = new List<string>();
        SpecialReadLines(new StringReader(newContents.ToString()), newNewLines);
        var newNewStats = GetStats(newNewLines);
        result.FixedStats = newNewStats;
        
        Console.WriteLine(path);
        Console.WriteLine("-----------");
        var diff = newStats.Diff(newNewStats);

        if (diff.AsoluteTotal > 0)
        {
            result.CanBeFixed = true;
        }
        else
        {
            return result;
        }

        Console.WriteLine("Detect: HEAD:" + originalStats.Total + ";" + originalStats + " => local:" + newStats.Total + ";" + newStats + " => fixed:" + newNewStats.Total + ";" + newNewStats + " (" + diff + ")");

        // write fixed file on disk, if desired
        if (execute)
        {
            result.Proceed = true;
            if (backup)
            {
                this.provider.LocalFileCopy(path, path + ".backup");
            }
            
            using (var file = this.provider.OpenLocalFile(path, FileMode.Open, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file, encoding))
            {
                foreach (var line in newNewLines)
                {
                    writer.Write(line);
                }
                
                writer.Flush();
                file.SetLength(file.Position);
                file.Flush();
            }

            Console.WriteLine("File fixed!");
        }
        
        Console.WriteLine();
        Console.WriteLine();
        return result;
    }

    public static FileStats GetStats(Stream stream, Encoding encoding)
    {
        var newLines = new List<string>();
        using (var reader = new StreamReader(stream, encoding))
        {
            SpecialReadLines(reader, newLines);
        }

        return GetStats(newLines);
    }
    
    /// <summary>
    /// finds the most used ending.
    /// </summary>
    /// <param name="stats"></param>
    /// <param name="fallback">when undecided, the fallback value</param>
    /// <returns></returns>
    private Ending GetPreference(FileStats stats, Ending fallback)
    {
        var endings = new Ending[] { Ending.Lf, Ending.CrLf, Ending.Cr, Ending.Other, };
        var numbers = new int[] { stats.UnixLines, stats.WindowsLines, stats.MacLines, stats.OtherLines, };
        var max = -1;
        for (int i = 0; i < numbers.Length; i++)
        {
            if (numbers[i] > max)
            {
                max = numbers[i];
            }
        }

        for (int i = 0; i < numbers.Length; i++)
        {
            if (numbers[i] == max)
            {
                return endings[i];
            }
        }

        return fallback;
    }

    /// <summary>
    /// finds the ending from the specified line
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private static Ending GetLineType(string line)
    {
        if (line.Length == 0)
        {
            return Ending.Other;
        }

        if (line.Length >= 2)
        {
            if (line[line.Length - 2] == cr && line[line.Length - 1] == lf)
            {
                return Ending.CrLf;
            }
        }

        if (line[line.Length-1] == cr)
        {
            return Ending.Cr;
        }
        else if (line[line.Length - 1] == lf)
        {
            return Ending.Lf;
        }

        return Ending.Other;
    }

    /// <summary>
    /// helps debug
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    private static string DisplayEnd(StringBuilder builder)
    {
        return builder.ToString().Substring(builder.Length - 32);
    }

    /// <summary>
    /// appends a line and an ending to the specified <see cref="StringBuilder"/>
    /// </summary>
    /// <param name="line">the line to append</param>
    /// <param name="lineType">the ending of the line to append</param>
    /// <param name="newType">the desired ending</param>
    /// <param name="writer"></param>
    private void Append(string line, Ending lineType, Ending newType, StringBuilder writer)
    {
        if (lineType == newType)
        {
            writer.Append(line);
            return;
        }

        if (lineType == Ending.CrLf)
        {
            writer.Append(line, 0, line.Length - 2);
        }
        else if (lineType == Ending.Cr || lineType == Ending.Lf)
        {
            writer.Append(line, 0, line.Length - 1);
        }

        if (newType == Ending.CrLf)
        {
            writer.Append(cr);
            writer.Append(lf);
        }
        else if (newType == Ending.Cr)
        {
            writer.Append(cr);
        }
        else if (newType == Ending.Lf)
        {
            writer.Append(lf);
        }
    }

    /// <summary>
    /// searches for the same line at the approximate index
    /// </summary>
    /// <param name="searchLine">the line contents to find</param>
    /// <param name="derivedIndex">the approximate location of the line to find</param>
    /// <param name="originalLines">the lines to search into</param>
    /// <param name="foundIndex">the found line index</param>
    /// <param name="foundLine">the found line</param>
    /// <returns></returns>
    private bool SearchLine(string searchLine, int derivedIndex, List<string> originalLines, out int foundIndex, out string foundLine)
    {
        var lineTrimmed = searchLine.Trim();

        if (originalLines.Count > derivedIndex)
        {
            var trimmed = originalLines[derivedIndex].Trim();
            if (string.Equals(lineTrimmed, trimmed))
            {
                foundIndex = derivedIndex;
                foundLine = originalLines[derivedIndex];
                return true;
            }
        }
        
        var delta = 10;
        var start = Math.Max(derivedIndex - delta, 0);
        var end = Math.Min(derivedIndex + delta, originalLines.Count-1);

        for (int i = start; i <= end; i++)
        {
            var trimmed = originalLines[i].Trim();
            if (string.Equals(lineTrimmed, trimmed))
            {
                foundIndex = i;
                foundLine = originalLines[i];
                return true;
            }
        }

        foundIndex = -1;
        foundLine = null;
        return false;
    }

    /// <summary>
    /// reads a stream line-by-line, extracting the line ending
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="lines"></param>
    private static void SpecialReadLines(TextReader reader, List<string> lines)
    {
        int value, previousValue = 0;
        var builder = new StringBuilder();
        while ((value = reader.Read()) >= 0)
        {
            var current = (char)value;
            if (previousValue == cr && value == lf)
            {
                builder.Append(current);
                lines.Add(builder.ToString());
                builder.Clear();
            }
            else if (previousValue != cr && value == lf)
            {
                builder.Append(current);
                lines.Add(builder.ToString());
                builder.Clear();
            }
            else if (previousValue == cr)
            {
                lines.Add(builder.ToString());
                builder.Clear();
                builder.Append(current);
            }
            else
            {
                builder.Append(current);
            }

            previousValue = value;
        }

        if (builder.Length > 0)
        {
            lines.Add(builder.ToString());
        }
    }

    /// <summary>
    /// counts the endings in the specified lines
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private static FileStats GetStats(List<string> lines)
    {
        int unix = 0, windows = 0, mac = 0, other = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var type = GetLineType(line);
            switch (type)
            {
                case Ending.CrLf:
                    windows++;
                    break;
                case Ending.Cr:
                    mac++;
                    break;
                case Ending.Lf:
                    unix++;
                    break;
                default:
                    if ((i+1) < lines.Count)
                    {
                        other++;
                    }
                    break;
            }
        }

        return new FileStats(unix, windows, mac, other);
    }
    
    public enum Ending
    {
        Other,
        CrLf,
        Cr,
        Lf,
    }

    public struct FileStats
    {
        public FileStats(int unixLines, int windowsLines, int macLines, int otherLines)
        {
            this.UnixLines = unixLines;
            this.WindowsLines = windowsLines;
            this.MacLines = macLines;
            this.OtherLines = otherLines;
        }

        public int UnixLines { get; }
        public int WindowsLines { get; }
        public int MacLines { get; }
        public int OtherLines { get; }

        public static FileStats Unix(int lines)
        {
            return new FileStats(lines, 0, 0, 0);
        }
        
        public static FileStats Windows(int lines)
        {
            return new FileStats(0, lines, 0, 0);
        }

        public static FileStats Mac(int lines)
        {
            return new FileStats(0, 0, lines, 0);
        }

        public int Total { get => this.UnixLines + this.WindowsLines + this.MacLines + this.OtherLines; }

        public int AsoluteTotal { get => Math.Abs(this.UnixLines) + Math.Abs(this.WindowsLines) + Math.Abs(this.MacLines) + Math.Abs(this.OtherLines); }

        public bool IsMixed
        {
            get
            {
                int status = 0;
                if (this.UnixLines > 0)
                {
                    status++;
                }

                if (this.WindowsLines > 0)
                {
                    status++;
                }

                if (this.MacLines > 0)
                {
                    status++;
                }

                if (this.OtherLines > 0)
                {
                    status++;
                }

                return status > 1;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var sep = string.Empty;
            
            if (this.UnixLines != 0)
            {
                builder.Append(sep);
                builder.Append("LF:");
                builder.Append(this.UnixLines);
                sep = ";";
            }

            if (this.WindowsLines != 0)
            {
                builder.Append(sep);
                builder.Append("CRLF:");
                builder.Append(this.WindowsLines);
                sep = ";";
            }

            if (this.MacLines != 0)
            {
                builder.Append(sep);
                builder.Append("CR:");
                builder.Append(this.MacLines);
                sep = ";";
            }

            if (this.OtherLines != 0)
            {
                builder.Append(sep);
                builder.Append("XX:");
                builder.Append(this.OtherLines);
                sep = ";";
            }

            return builder.ToString();
        }

        public FileStats Diff(FileStats source)
        {
            return new FileStats(
                source.UnixLines - this.UnixLines,
                source.WindowsLines - this.WindowsLines,
                source.MacLines - this.MacLines,
                source.OtherLines - this.OtherLines);
        }
    }

    public class FileOperation
    {
        public string Path { get; }
        public bool IsFoundInGit { get; set; }
        public FileStats OriginalStats { get; set; }
        public FileStats LocalStats { get; set; }
        public FileStats FixedStats { get; set; }
        public bool Proceed { get; set; }
        public bool CanBeFixed { get; set; }

        public FileOperation(string path)
        {
            this.Path = path;
        }
    }
}
