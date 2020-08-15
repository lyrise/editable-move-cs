using System.Linq;
using System.IO;
using Cocona;
using DotNet.Globbing;
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        CoconaLiteApp.Run<Program>(args);
    }

    public void Find([Option('p')] string? pattern = null, [Option('d')] string? directory = null)
    {
        if (directory is null) directory = Directory.GetCurrentDirectory();
        if (pattern is null) pattern = "*";

        var glob = Glob.Parse(pattern);
        directory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(".vrename");
        using var dirWriter = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/dir"), false);
        using var oldWriter = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/old"), false);
        using var newWriter = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/new"), false);
        dirWriter.NewLine = "\n";
        oldWriter.NewLine = "\n";
        newWriter.NewLine = "\n";

        dirWriter.WriteLine(directory);

        var list = Directory.EnumerateFiles(directory, "*", new EnumerationOptions { RecurseSubdirectories = true }).ToList();
        list.Sort();

        foreach (var path in list)
        {
            var value = path[directory.Length..];

            if (glob.IsMatch(value))
            {
                oldWriter.WriteLine(value);
                newWriter.WriteLine(value);
            }
        }
    }

    public void Run()
    {
        using var dirReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/dir"));
        using var oldReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/old"));
        using var newReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/new"));

        var directory = dirReader.ReadLine() ?? string.Empty;
        var oldList = oldReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var newList = newReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).ToArray();

        Rename(directory, oldList, newList);
    }

    public void Undo()
    {
        using var dirReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/dir"));
        using var oldReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/old"));
        using var newReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), ".vrename/new"));

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
            var oldPath = oldList[i];
            var newPath = newList[i];

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

            File.Move(Path.Combine(directory, oldPath), Path.Combine(directory, newPath));
        }
    }

    public void Clean()
    {
        Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), ".vrename"), true);
    }
}
