using System;
using System.IO;
using System.Linq;

class PathConverter
{
    public static string ToArduinoPath(string path)
    {
        if (!PathIsValid(path))
        {
            throw new System.IO.IOException("Path given may not be a child of the curent directory (it uses the \'..\' directory)");
        }

        if (FileFunctions.PathIsAbsolute(path))
        {
            int prefix = System.Environment.CurrentDirectory.Length;
            path = path.Substring(prefix + 1); // CurrentDirectory doesn't include a trailing slash
        }

        var split = path.Split(System.IO.Path.DirectorySeparatorChar);

        string newPath = "";
        foreach (string s in split)
        {
            // Ignore the cwd shorthand
            if (s.Equals("."))
                continue;

            newPath += "/" + s;
        }

        newPath = newPath.Replace(" ", "\\ ");
        return newPath;
    }

    private static bool PathIsValid(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        var dirs = path.Split(System.IO.Path.DirectorySeparatorChar); // Look at each string between separators for "..". 
                                                                      // If we looked at the whole string at once, "..." in a filname would be detected
                                                                      // We are either in a subdir of cwd, or in cwd, and the path does not contain ".."
        if (fullPath.StartsWith(System.Environment.CurrentDirectory) && !dirs.Contains(".."))
        {
            return true;
        }

        return false;
    }
}