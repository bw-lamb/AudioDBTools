using System;
using System.Linq;

class Program
{
    private static readonly string DEFAULT_DB_LOCATION = "db";
    private static string? dbLocation = null;
    private static string[] PROG_ARGS = [];
    private static readonly string[] COMMANDS = ["init", "add", "remove", "prune"];
    private static Action[] DELEGATES = [() => Init(PROG_ARGS), () => Add(PROG_ARGS), () => Remove(PROG_ARGS), Prune];
    private static Action[] HELP_FNS = [HelpInit, HelpAdd, HelpRemove, HelpPrune];

    private static void Usage()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] [command] [flags] [args]");
        Console.WriteLine("\nWhere [command] can be:");

        foreach(string cmd in COMMANDS)
        {
            Console.WriteLine("\t{0}", cmd);
        }

        Console.WriteLine("For help with a specific function, add the --help/-h flag after [command]");
    }

    private static void HelpInit()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] init [flags] filename1 filename2 ...");
        Console.WriteLine("\tWhere filename[n] is any accepted audio file or playlist file.");
        Console.WriteLine("Accepted audio files include flac, mp3, & m4a files. m3u files are used to describe playlists.");
    }

    private static void Init(string[] files)
    {
        try 
        {
            var dbg = DatabaseGenerator.GetWithInit("db");
            dbg.ProcessFiles(files);
        } 
        catch(System.Data.SQLite.SQLiteException e)
        {
            if(e.ErrorCode == 1) // "Table already exists" returns error code of 1
                Console.WriteLine("[ERR ] An error occured. Are you trying to add to an existing database? Use the add option instead of init.");
            else
                Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }

    }

    private static void HelpAdd()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] add [flags] filename1 filename2 ...");
        Console.WriteLine("\tWhere filename[n] is any accepted audio file or playlist file.");
        Console.WriteLine("Accepted audio files include flac, mp3, & m4a files. m3u files are used to describe playlists.");
    }

    private static void Add(string[] files)
    {
        try
        {
            var dbg = DatabaseGenerator.GetWithoutInit("db");
            dbg.ProcessFiles(files); 
        }
        catch
        {
            Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }
    }

    private static void HelpRemove()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] remove [flags] filename1 filename2 ...");
        Console.WriteLine("\tWhere filename[n] is any accepted audio file or playlist file.");
        Console.WriteLine("Accepted audio files include flac, mp3, & m4a files. m3u files are used to describe playlists.");
    }

    private static void Remove(string[] files)
    {
        try
        {
            var dbg = DatabaseGenerator.GetWithoutInit("db");
            dbg.RemoveFiles(files);
        }
        catch
        {
            Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }
    }

    private static void HelpPrune()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] prune [flags]");
        Console.WriteLine("\tWhere filename[n] is any accepted audio file or playlist file.");
        Console.WriteLine("Accepted audio files include flac, mp3, & m4a files. m3u files are used to describe playlists.");
    }

    private static void Prune()
    {
        try
        {
            var dbg = DatabaseGenerator.GetWithoutInit("db");
            dbg.PruneDB();
        }
        catch
        {
            Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }
    }

    public static void Main(string[] args)
    {
        if(args.Length == 0)
        {
            Usage();
            return;
        }

        string command = args[0].ToLower();
        PROG_ARGS = args.Skip(1).Take(args.Length - 1).ToArray();

        for(int i = 0; i < COMMANDS.Length; i++)
        {
            if(command.Equals(COMMANDS[i]))
            {
                foreach(string arg in PROG_ARGS) 
                {
                    if(arg.ToLower().Equals("--help") || arg.ToLower().Equals("-h"))
                    {
                        HELP_FNS[i]();
                        return;
                    }
                }
                DELEGATES[i]();
                return;
            }
        }

        Console.WriteLine("Unknown function \"{0}\"", command);
        Usage();
    }   
}
