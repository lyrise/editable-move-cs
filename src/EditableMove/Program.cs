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

    public void Find([Option('p')] string? pattern = null)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        Directory.CreateDirectory(Path.Combine(currentDirectory, WorkingDirectoryName));
        using var dirWriter = new StreamWriter(Path.Combine(currentDirectory, WorkingDirectoryName, "dir"), false);
        using var oldWriter = new StreamWriter(Path.Combine(currentDirectory, WorkingDirectoryName, "old"), false);
        using var newWriter = new StreamWriter(Path.Combine(currentDirectory, WorkingDirectoryName, "new"), false);
        dirWriter.NewLine = "\n";
        oldWriter.NewLine = "\n";
        newWriter.NewLine = "\n";

        dirWriter.WriteLine(currentDirectory);

        var list = Ganss.IO.Glob.Expand(pattern)
            .Where(n => !n.Attributes.HasFlag(FileAttributes.Directory))
            .Select(n => n.FullName)
            .OrderBy(n => n)
            .ToList();

        foreach (var path in list)
        {
            var value = path[(currentDirectory.Length + 1)..];
            if (value.StartsWith(WorkingDirectoryName + Path.DirectorySeparatorChar)) continue;

            oldWriter.WriteLine(value);
            newWriter.WriteLine(value);
        }
    }

    public void Run()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        using var dirReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "dir"));
        using var oldReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "old"));
        using var newReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "new"));

        var directory = dirReader.ReadLine() ?? string.Empty;
        var oldList = oldReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var newList = newReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();

        Rename(directory, oldList, newList);
    }

    public void Undo()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        using var dirReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "dir"));
        using var oldReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "old"));
        using var newReader = new StreamReader(Path.Combine(currentDirectory, WorkingDirectoryName, "new"));

        var directory = dirReader.ReadLine() ?? string.Empty;
        var oldList = oldReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var newList = newReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();

        Rename(directory, newList, oldList);
    }

    private static void Rename(string directory, string[] oldList, string[] newList)
    {
        if (directory == string.Empty || !Directory.Exists(directory)) throw new Exception("directory not found");
        if (oldList.Length != newList.Length) throw new Exception("old and new line count is not equal");

        // Check Conflict
        {
            if (newList.Length != new HashSet<string>(newList).Count) throw new Exception("line conflict");
            if (oldList.Length != new HashSet<string>(oldList).Count) throw new Exception("line conflict");
        }

        for (int i = 0; i < oldList.Length; i++)
        {
            var oldPath = Path.Combine(directory, oldList[i]);
            var newPath = Path.Combine(directory, newList[i]);

            if (oldPath == newPath) continue;
            if (!File.Exists(oldPath)) continue;

            // Sort
            {
                var newDir = Path.GetDirectoryName(newPath);
                if (!Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                }
            }

            File.Move(oldPath, newPath);
        }
    }

    public void Clean()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        Directory.Delete(Path.Combine(currentDirectory, WorkingDirectoryName), true);
    }
}
