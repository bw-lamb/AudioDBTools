using System.IO;

public class Logger
{
    private static bool silent = false;
    private static StreamWriter output = new(System.Console.OpenStandardOutput());

    public static void Stop()
    {
        output.Close();
    }

    public static void SetSilent(bool val)
    {
        silent = val;
    }

    private static void Log(string prefix, string text, bool critical = false)
    {
        if(silent && !critical)
            return;

        string log = string.Format("[{0}] {1}", prefix, text);
        output.WriteLine(log);
    }

    public static void LogResult(string text)
    {
        output.WriteLine(text);
    }

    public static void LogInfo(string text)
    {
        Log("INFO", text);
    }

    public static void LogWarn(string text)
    {
        Log("WARN", text, true);
    }

    public static void LogError(string text)
    {
        Log("ERR ", text, true);
    }

    public static void LogCritical(string text)
    {
        Log("CRIT", text, true);
    }
}