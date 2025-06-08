using System;
using System.Linq;

public class FileFunctions
{
    public static bool PathIsAbsolute(string path)
    {
        return path.Equals(System.IO.Path.GetFullPath(path));
    }

    // Gets the directory of the file given, based off of the cwd (eg. /dir1/dir2/file -> dir1/dir2)
    public static string GetFileDirectory(string filepath)
    {
        var split = filepath.Split(System.IO.Path.DirectorySeparatorChar);
        var directory = split.SkipLast(1);

        if (!directory.Any())
            return "";

        return string.Join(System.IO.Path.DirectorySeparatorChar, directory);
    }

    // Gets file name from filepath, stripping any directories or extensions (eg, dir/file.extn -> file)
    public static string GetFileName(string filepath)
    {
        string filename = filepath.Split(System.IO.Path.DirectorySeparatorChar).TakeLast(1).ElementAt(0);
        var split = filename.Split('.').SkipLast(1); // Skip the extension

        return string.Join('.', split);
    }

}