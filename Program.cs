using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    private static readonly string DEFAULT_DB_LOCATION = "db";
    private static string dbLocation = DEFAULT_DB_LOCATION; // This can change
    private static bool flagPruneAfterRemoving = false;
    private static bool arduinoMode = false;

    private static void Usage()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] [command] [flags] [files]");
        Console.WriteLine("\nWhere [command] can be:");
        Console.WriteLine("\tinit\n\tadd\n\tremove\n\tprune");

        Console.WriteLine("\nFlags:");
        Console.WriteLine("\t-o [output_file] | --output [output_file]\tWrite database to output_file");
        Console.WriteLine("\t-p | --prune\t\t\t\t\tRun prune after removing files.");
        Console.WriteLine("\t-q | --quiet\t\t\t\t\tSuppress output");
        Console.WriteLine("\t-a | --arduino\t\t\t\tArduino mode [WORK IN PROGRESS]");

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
            var dbg = DatabaseGenerator.GetWithInit(dbLocation, arduinoMode);
            dbg.ProcessFiles(files);
        } 
        catch(IOException ex)
        {
            Logger.LogCritical(ex.Message);
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
            var dbg = DatabaseGenerator.GetWithoutInit(dbLocation, arduinoMode);
            dbg.ProcessFiles(files);
        }
        catch (Exception ex)
        when(ex is FileNotFoundException || ex is IOException)
        {
            Logger.LogCritical(ex.Message);    
        }
    }

    private static void HelpRemove()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] remove [flags] filename1 filename2 ...");
        Console.WriteLine("\tWhere filename[n] is any accepted audio file or playlist file.");
        Console.WriteLine("Accepted audio files include flac, mp3, & m4a files. m3u files are used to describe playlists.");
    }

    private static void Remove(string[] files, bool prune)
    {
        try
        {
            DatabaseGenerator dbg = DatabaseGenerator.GetWithoutInit(dbLocation, arduinoMode);
            dbg.RemoveFiles(files, prune);
        }
        catch (Exception ex)
        when(ex is FileNotFoundException || ex is IOException)
        {
            Logger.LogCritical(ex.Message); 
        }
    }

    private static void HelpPrune()
    {
        Console.WriteLine("Usage: [ADD PROGRAM NAME HERE] prune");
    }

    private static void Prune()
    {
        try
        {
            DatabaseGenerator dbg = DatabaseGenerator.GetWithoutInit(dbLocation, arduinoMode);
            dbg.PruneDB();
        }
        catch (FileNotFoundException ex)
        {
            Logger.LogCritical(ex.Message);
        }
        catch (IOException ex)
        {
            Logger.LogCritical(ex.Message);
        }
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

    // Goes through args, processing any flags present.
    // Returns args without any flag values if successful, or null if something went wrong.
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
                        Logger.LogError($"Flag {args[i]} was last given argument, expected destination.");
                        return null;
                    }
                    else
                    {
                        dbLocation = args[i + 1];
                        i += 1; // Don't process outfile name as a song/playlist
                    }
                    break;
                case "--prune" or "-p":
                    if (!command.Equals("remove"))
                        Logger.LogWarn("Pruning flag given, but we are not removing anything. Ignoring");
                    else
                        flagPruneAfterRemoving = true;
                    break;
                case "--quiet" or "-q":
                    Logger.SetSilent(true);
                    break;
                case "--arduino" or "-a":
                    arduinoMode = true;
                    break;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        Logger.LogError($"Unknown flag {args[i]} given.");
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

        if (files is null) // We've seen an error in parsing flags
        {
            Logger.Stop();
            return;
        }

        switch (command)
            {
                case "init":
                    if (files.Length == 0)
                    {
                        Logger.LogError("No audio files provided.");
                        break;
                    }
                    Init(files);
                    break;
                case "add":
                    if (files.Length == 0)
                    {
                        Logger.LogError("No audio files provided.");
                        break;
                    }
                    Add(files);
                    break;
                case "remove":
                    if (files.Length == 0)
                    {
                        Logger.LogError("No audio files provided.");
                        break;
                    }
                    Remove(files, flagPruneAfterRemoving);
                    break;
                case "prune":
                    if (files.Length != 0)
                        Logger.LogWarn("Files given while we are pruning. They are being ignored. Use \"remove\" to delete files");
                    Prune();
                    break;
                default:
                    Logger.LogError($"Unknown function \"{command}\"");
                    Usage();
                    break;
            }
            Logger.Stop();
    }   
}
