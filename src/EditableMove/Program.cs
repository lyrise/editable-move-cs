using System.Runtime.Serialization;
using Cocona;

class Program
{
    private const string WorkingDirectoryName = ".emv";

    static void Main(string[] args)
    {
        CoconaLiteApp.Run<Program>(args);
    }

    public void Find([Option('d')] bool containDirectory = false, [Option('f')] bool containFile = true, [Option('r')] int maxRecursionDepth = int.MaxValue)
    {
        var currentDir = Directory.GetCurrentDirectory();

        var oldList = new List<string>();
        var newList = new List<string>();

        var enumerationOptions = new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = maxRecursionDepth };

        if (containDirectory)
        {
            foreach (var dir in Directory.EnumerateDirectories(currentDir, "*", enumerationOptions))
            {
                var relativePath = dir[(currentDir.Length + 1)..];
                if (relativePath.StartsWith(WorkingDirectoryName)) continue;

                oldList.Add(relativePath);
                newList.Add(relativePath);
            }
        }

        if (containFile)
        {
            foreach (var dir in Directory.EnumerateFiles(currentDir, "*", enumerationOptions))
            {
                var relativePath = dir[(currentDir.Length + 1)..];
                if (relativePath.StartsWith(WorkingDirectoryName + Path.DirectorySeparatorChar)) continue;

                oldList.Add(relativePath);
                newList.Add(relativePath);
            }
        }

        oldList.Sort();
        newList.Sort();

        this.SaveConfig(new Config() { OldPathList = oldList.ToArray(), NewPathList = newList.ToArray() });
    }

    public void Move([Option('u')] bool undo)
    {
        var currentDir = Directory.GetCurrentDirectory();

        var config = this.LoadConfig();

        if (!undo)
        {
            this.Rename(currentDir, config?.OldPathList ?? Array.Empty<string>(), config?.NewPathList ?? Array.Empty<string>());
        }
        else
        {
            this.Rename(currentDir, config?.NewPathList ?? Array.Empty<string>(), config?.OldPathList ?? Array.Empty<string>());
        }
    }

    public void Clean()
    {
        var currentDir = Directory.GetCurrentDirectory();
        Directory.Delete(Path.Combine(currentDir, WorkingDirectoryName), true);
    }

    private record Config
    {
        public string[]? OldPathList;
        public string[]? NewPathList;
    }

    private Config LoadConfig()
    {
        var currentDir = Directory.GetCurrentDirectory();

        using var oldReader = new StreamReader(Path.Combine(currentDir, WorkingDirectoryName, "old"));
        using var newReader = new StreamReader(Path.Combine(currentDir, WorkingDirectoryName, "new"));

        var oldPathList = oldReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var newPathList = newReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();

        return new Config() { OldPathList = oldPathList, NewPathList = newPathList };
    }

    private void SaveConfig(Config config)
    {
        var currentDir = Directory.GetCurrentDirectory();

        Directory.CreateDirectory(Path.Combine(currentDir, WorkingDirectoryName));

        using var oldWriter = new StreamWriter(Path.Combine(currentDir, WorkingDirectoryName, "old"), false);
        using var newWriter = new StreamWriter(Path.Combine(currentDir, WorkingDirectoryName, "new"), false);
        oldWriter.NewLine = "\n";
        newWriter.NewLine = "\n";

        foreach (var path in config?.OldPathList ?? Array.Empty<string>())
        {
            oldWriter.WriteLine(path);
        }

        foreach (var path in config?.NewPathList ?? Array.Empty<string>())
        {
            newWriter.WriteLine(path);
        }
    }

    private void Rename(string directory, string[] oldList, string[] newList)
    {
        if (directory == string.Empty || !Directory.Exists(directory)) throw new EditableMoveException("directory is not found");
        if (oldList.Length != newList.Length) throw new EditableMoveException("old and new line count is not equal");

        this.ValidateDuplicate(oldList);
        this.ValidateDuplicate(newList);

        for (int i = 0; i < oldList.Length; i++)
        {
            var oldPath = Path.Combine(directory, oldList[i]);
            var newPath = Path.Combine(directory, newList[i]);

            if (oldPath == newPath) continue;

            if (File.Exists(oldPath))
            {
                var newDir = Path.GetDirectoryName(newPath);
                if (newDir is not null && !Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                }

                File.Move(oldPath, newPath);
            }
            else if (Directory.Exists(oldPath))
            {
                var newDir = Path.GetDirectoryName(newPath);
                if (newDir is not null && !Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                }

                Directory.Move(oldPath, newPath);
            }
        }
    }

    private void ValidateDuplicate(string[] pathList)
    {
        var hastSet = new HashSet<string>();

        foreach (var p in pathList)
        {
            if (!hastSet.Add(p)) throw new EditableMoveException($"Duplicate paths ({p})");
        }
    }
}

[Serializable]
public class EditableMoveException : Exception
{
    public EditableMoveException() { }
    public EditableMoveException(string message) : base(message) { }
    public EditableMoveException(string message, Exception inner) : base(message, inner) { }
    protected EditableMoveException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
