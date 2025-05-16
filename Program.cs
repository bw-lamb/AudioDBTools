using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static readonly string DEFAULT_DB_LOCATION = "db";
    private static string dbLocation = DEFAULT_DB_LOCATION; // This can change

    private static readonly string[] COMMANDS = ["init", "add", "remove", "prune"];

    private static bool flagPruneAfterRemoving = false;

    private static void Usage()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] [command] [flags] [args]");
        Console.WriteLine("\nWhere [command] can be:");

        foreach (string cmd in COMMANDS)
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
        //try 
        //{
            var dbg = DatabaseGenerator.GetWithInit(dbLocation);
            dbg.ProcessFiles(files);
        //} 
        /*catch(System.Data.SQLite.SQLiteException e)
        {
            if(e.ErrorCode == 1) // "Table already exists" returns error code of 1
                Console.WriteLine("[ERR ] An error occured. Are you trying to add to an existing database? Use the add option instead of init.");
            else
                Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }*/
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
            var dbg = DatabaseGenerator.GetWithoutInit(dbLocation);
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
            var dbg = DatabaseGenerator.GetWithoutInit(dbLocation);
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
    }

    private static void Prune()
    {
        //try
        //{
            var dbg = DatabaseGenerator.GetWithoutInit(dbLocation);
            dbg.PruneDB();
        /*}
        catch
        {
            Console.WriteLine("[ERR ] An unknown error occured. This may be a good time to make an issue at https://github.com/bw-lamb/AudioDBTools");
        }*/
    }

    private static void Help(string command)
    {
        switch (command)
        {
            case "init":
                HelpInit();
                break;
            case "add":
                HelpAdd();
                break;
            case "remove":
                HelpRemove();
                break;
            case "prune":
                HelpPrune();
                break;
        }
    }

    private static string[]? ParseFlags(string command, string[] args)
    {
        List<string> files = [];

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--help" or "-h":
                    Help(command);
                    return null;
                case "--output" or "-o":
                    if (i + 1 == args.Length)
                    {
                        System.Console.WriteLine("[ERR ] Flag {0} was last given argument, expected destination.", args[i]);
                        return null;
                    }
                    else
                    {
                        dbLocation = args[i + 1];
                    }
                    break;
                case "--prune" or "-p":
                    if (!command.Equals("remove"))
                        System.Console.WriteLine("[WARN] Pruning flag given, but we are not removing anything. Ignoring");
                    else
                        flagPruneAfterRemoving = true;
                    break;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        System.Console.WriteLine("[ERR ] Unknown flag {0} given.", args[i]);
                        return null;
                    }
                    else
                        files.Add(args[i]);
                    break;
            }
        }

        return files.ToArray();
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Usage();
            return;
        }

        string command = args[0].ToLower();

        
        string[]? files = ParseFlags(command, args.Skip(1).ToArray());

        if (files == null) // We've seen an error in parsing flags
        {
            return;
        }

        switch (command)
            {
                case "init":
                    Init(files);
                    break;
                case "add":
                    Add(files); 
                    break;
                case "remove":
                    Remove(files);
                    if (flagPruneAfterRemoving)
                        Prune();
                    break;
                case "prune":
                    if(files.Length != 0)
                        System.Console.WriteLine("[WARN] Files given while we are pruning. They are being ignored. Use \"remove\" to delete files");
                    Prune();
                    break;
                default:
                    Console.WriteLine("Unknown function \"{0}\"", command);
                    Usage();
                    break;
            }
    }   
}
