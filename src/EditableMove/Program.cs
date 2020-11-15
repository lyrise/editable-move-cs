using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using Cocona;

class Program
{
    private const string WorkingDirectoryName = ".emv";

    static void Main(string[] args)
    {
        CoconaLiteApp.Run<Program>(args);
    }

    public void Find([Argument] string pattern, [Option('f')] bool fileOnly, [Option('d')] bool directoryOnly)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var oldList = new List<string>();
        var newList = new List<string>();

        foreach (var fileSystemInfo in Ganss.IO.Glob.Expand(pattern))
        {
            bool isFile = !fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
            var relativePath = fileSystemInfo.FullName[(currentDirectory.Length + 1)..];

            if (isFile)
            {
                if (relativePath.StartsWith(WorkingDirectoryName + Path.DirectorySeparatorChar)) continue;

                // "-d" が指定されているのみの場合は、ファイルは対象外にする
                if (!fileOnly && directoryOnly)
                {
                    continue;
                }
            }
            else
            {
                if (relativePath.StartsWith(WorkingDirectoryName)) continue;

                // "-d" が指定されていない場合は、ディレクトリは対象外にする
                if (!directoryOnly)
                {
                    continue;
                }
            }

            if (fileSystemInfo.FullName == currentDirectory) continue;

            oldList.Add(relativePath);
            newList.Add(relativePath);
        }

        oldList.Sort();
        newList.Sort();

        this.SaveConfig(new Config() { OldPathList = oldList.ToArray(), NewPathList = newList.ToArray() });
    }

    public void Move([Option('u')] bool undo)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var config = this.LoadConfig();

        if (!undo)
        {
            this.Rename(currentDirectory, config?.OldPathList ?? Array.Empty<string>(), config?.NewPathList ?? Array.Empty<string>());
        }
        else
        {
            this.Rename(currentDirectory, config?.NewPathList ?? Array.Empty<string>(), config?.OldPathList ?? Array.Empty<string>());
        }
    }

    public void Clean()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.Delete(Path.Combine(currentDirectory, WorkingDirectoryName), true);
    }

    private record Config
    {
        public string[]? OldPathList;
        public string[]? NewPathList;
    }

    private Config LoadConfig()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        using var oldReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "old"));
        using var newReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "new"));

        var oldPathList = oldReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var newPathList = newReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();

        return new Config() { OldPathList = oldPathList, NewPathList = newPathList };
    }

    private void SaveConfig(Config config)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        Directory.CreateDirectory(Path.Combine(currentDirectory, WorkingDirectoryName));

        using var oldWriter = new StreamWriter(Path.Combine(currentDirectory, WorkingDirectoryName, "old"), false);
        using var newWriter = new StreamWriter(Path.Combine(currentDirectory, WorkingDirectoryName, "new"), false);
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
                if (!Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                }

                File.Move(oldPath, newPath);
            }
            else if (Directory.Exists(oldPath))
            {
                var newDir = Path.GetDirectoryName(newPath);
                if (!Directory.Exists(newDir))
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

[System.Serializable]
public class EditableMoveException : System.Exception
{
    public EditableMoveException() { }
    public EditableMoveException(string message) : base(message) { }
    public EditableMoveException(string message, System.Exception inner) : base(message, inner) { }
    protected EditableMoveException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
