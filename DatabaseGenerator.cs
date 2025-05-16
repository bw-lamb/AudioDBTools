using System;
using System.Collections.Generic;
using System.Linq;

public class DatabaseGenerator
{
    private static readonly string[] ACCEPTED_EXTENSIONS = ["mp3", "m4a", "flac"];
    private static readonly string PLAYLIST_EXTENSION = "m3u";
    private readonly DBAgent db;
   
    private DatabaseGenerator(string dbFilepath)
    {
        db = new DBAgent(dbFilepath);
    }

    public static DatabaseGenerator GetWithInit(string dbFilepath)
    {
        DatabaseGenerator dbg = new DatabaseGenerator(dbFilepath);
        dbg.db.InitDB();
        return dbg;
    }

    public static DatabaseGenerator GetWithoutInit(string dbFilepath)
    {
        DatabaseGenerator dbg = new DatabaseGenerator(dbFilepath);
        return dbg;
    }

    private static bool IsSongFile(string filename)
    {
        string[] split = filename.Split('.');
        string extn = split[^1];

        if((split.Length != 1 || split[0] != "") && ACCEPTED_EXTENSIONS.Contains(extn.ToLower()))
        {
            return true;
        }

        return false;
    }

    private static bool IsPlaylistFile(string filename)
    {
        string[] split = filename.Split('.');
        string extn = split[^1];

        if ((split.Length != 1 || split[0] != "") && extn.ToLower().Equals(PLAYLIST_EXTENSION))
        {
            return true;
        }

        return false;
    }

    public void ProcessSong(string filename)
    {
        TagLib.File fileHandle = TagLib.File.Create(filename);

        string title = fileHandle.Tag.Title;
        string[] artists = fileHandle.Tag.Performers;
        string album = fileHandle.Tag.Album;
        string albumArtist = String.Join(", ", fileHandle.Tag.AlbumArtists);
        TimeSpan length = fileHandle.Properties.Duration;
        uint year = fileHandle.Tag.Year;
        uint diskno = fileHandle.Tag.Disc;
        uint totalDisks = fileHandle.Tag.DiscCount;
        uint totalTracks = fileHandle.Tag.TrackCount;
        uint trackno = fileHandle.Tag.Track;
        string[] genres = fileHandle.Tag.Genres;

        // SANITIZATION
        if(artists.Length == 0)
            artists = ["Unknown"];

        if(album.Equals(""))
            album = "Unknown";

        if(genres.Length == 0)
            genres = ["Unknown"];

        if(totalDisks == 0)
            totalDisks = 1;

        if(diskno == 0)
            diskno = 1;

        // ARTISTS
        foreach (string artist in artists)
        {
            if (db.HasArtist(artist))
            {
                Console.WriteLine("[INFO] Artist {0} already in DB. Skipping", artist);
            }
            else
            {
                Console.WriteLine("[INFO] New artist {0} added to DB.", artist);
                db.AddArtist(artist);
            }
        }
        // GENRES
        foreach (string genre in genres)
        {
            if (db.HasGenre(genre))
            {
                Console.WriteLine("[INFO] Genre {0} already in DB. Skipping", genre);
            }
            else
            {
                Console.WriteLine("[INFO] New genre {0} added to DB.", genre);
                db.AddGenre(genre);
            }
        }
        // ALBUMS
        if(db.HasAlbum(album, albumArtist, year))
        {
            Console.WriteLine("[INFO] Album {0} already in DB. skipping.", album);
        }
        else
        {
            Console.WriteLine("[INFO] New album {0} added to DB.", album);
            db.AddAlbum(album, albumArtist, year, totalDisks, totalTracks);
        }

        string absolutePath = System.IO.Path.GetFullPath(filename);

        // SONGS
        if(db.HasSong(filename))
        {
            
            if(!db.HasSong(absolutePath))
            {
                Console.WriteLine("[WARN] Song {0} with same details already in DB but from another file. Adding duplicate.", title);
                db.AddSong(title, artists, album, albumArtist, genres, length, diskno, trackno, year, absolutePath);
            }   
            else 
            {
                Console.WriteLine("[INFO] Song {0} ({1}) already in DB. skipping.", title, filename);
            }
        }
        else
        {
            Console.WriteLine("[INFO] New song {0} ({1}) added to DB.", title, filename);
            
            db.AddSong(title, artists, album, albumArtist, genres, length, diskno, trackno, year, absolutePath);
        }
    }

    public void ProcessPlaylist(string filename)
    {
        db.AddPlaylist(filename);
    }

    public void ProcessFiles(string[] files)
    {
        List<string> playlists = new List<string>();

        foreach(string file in files)
        {
            if(IsSongFile(file))
                ProcessSong(file);
            else if(IsPlaylistFile(file))
                playlists.Add(file);
            else
                Console.WriteLine("[ERR ] Unkown file {0}", file);
        }

        // do all playlists after all songs are dealt with
        foreach(string playlist in playlists)
        {
            ProcessPlaylist(playlist);
        }
    }

    public void RemoveFiles(string[] files)
    {
        foreach(string file in files)
        {
            if(IsSongFile(file))
            {
                Console.WriteLine("[INFO] Removing song {0} from DB.", file);
                db.RemoveSong(file);
            }
            else if(IsPlaylistFile(file))
            {
                Console.WriteLine("[INFO] Removing playlist {0}.", file);
                db.RemovePlaylist(file);
            }
            else
                Console.WriteLine("[ERR ] Unkown file {0}", file);
        }
    }

    public void PruneDB()
    {
        List<uint> unusedAlbums = db.UnusedAlbums();
        List<uint> unusedArtists = db.UnusedArtists();
        List<uint> unusedGenres = db.UnusedGenres();

        foreach (uint albumId in unusedAlbums)
        {
            Console.WriteLine("[INFO] Pruning unused album {0} from DB.", db.GetAlbumName(albumId));
            db.RemoveAlbum(albumId);
        }
        foreach (uint artistId in unusedArtists)
        {
            Console.WriteLine("[INFO] Pruning unused artist {0} from DB.", db.GetArtistName(artistId));
            db.RemoveAlbum(artistId);
        }

        foreach (uint genreId in unusedGenres)
        {
            Console.WriteLine("[INFO] Pruning unused genre {0} from DB.", db.GetGenreName(genreId));
            db.RemoveAlbum(genreId);
        }
    }
}