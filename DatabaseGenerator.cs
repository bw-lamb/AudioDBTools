using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class DatabaseGenerator
{
    private static readonly string[] ACCEPTED_EXTENSIONS = ["mp3", "m4a", "flac"];
    private static readonly string PLAYLIST_EXTENSION = "m3u";
    private readonly DBAgent db;
    private readonly bool arduinoMode;
    private uint songsAffected;
    private uint artistsAffected;
    private uint albumsAffected;
    private uint genresAffected;
    private uint playlistsAffected;

    private DatabaseGenerator(string dbFilepath, bool arduinoMode)
    {
        db = new DBAgent(dbFilepath);
        songsAffected = 0;
        artistsAffected = 0;
        albumsAffected = 0;
        genresAffected = 0;
        playlistsAffected = 0;
        this.arduinoMode = arduinoMode;
    }

    // Create and get a handle on a database.
    public static DatabaseGenerator GetWithInit(string dbFilepath, bool arduinoMode)
    {
        if (File.Exists(dbFilepath))
        {
            string msg;
            if (IsDatabase(dbFilepath))
            {
                msg = string.Format("Database already present at {0}. Use the \"add\" command instead.", dbFilepath);
            }
            else
            {
                msg = string.Format("A file already present at {0}, and it is not a database.", dbFilepath);
            }
            throw new IOException(msg);
        }
        DatabaseGenerator dbg = new DatabaseGenerator(dbFilepath, arduinoMode);
        dbg.db.InitDB();
        return dbg;
    }

    // Get a handle on an existing database.
    public static DatabaseGenerator GetWithoutInit(string dbFilepath, bool arduinoMode)
    {
        if (!File.Exists(dbFilepath))
        {
            throw new FileNotFoundException(string.Format("File {0} does not exist. Did you mean to use \"init\" instead?", dbFilepath));
        }

        if (!IsDatabase(dbFilepath))
        {
            throw new IOException(string.Format("File {0} is not an SQLite 3 database.", dbFilepath));
        }

        DatabaseGenerator dbg = new DatabaseGenerator(dbFilepath, arduinoMode);
        return dbg;
    }

    // Checks if the file is a database. This only looks for the header all databases must have.
    // See https://www.sqlite.org/fileformat.html 
    private static bool IsDatabase(string filepath)
    {
        byte[] SQL_HEADER = [0x53, 0x51, 0x4c, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6f, 0x72, 0x6d, 0x61, 0x74, 0x20, 0x33, 0x00];
        byte[] header = new byte[16];

        using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
        {
            reader.Read(header, 0, 16);
        }

        if (header.SequenceEqual(SQL_HEADER))
        {
            return true;
        }

        return false;
    }

    private static bool IsSongFile(string filename)
    {
        string[] split = filename.Split('.');
        string extn = split[^1];

        if ((split.Length != 1 || split[0] != "") && ACCEPTED_EXTENSIONS.Contains(extn.ToLower()))
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
        string[] albumArtists = fileHandle.Tag.AlbumArtists;
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

        if (albumArtists.Length == 0)
            albumArtists = ["Unknown"];

        if (genres.Length == 0)
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
                Logger.LogInfo(string.Format("Artist {0} already in DB. Skipping", artist));
            }
            else
            {
                Logger.LogInfo(string.Format("New artist {0} added to DB.", artist));
                db.AddArtist(artist);
                artistsAffected++;
            }
        }
        // GENRES
        foreach (string genre in genres)
        {
            if (db.HasGenre(genre))
            {
                Logger.LogInfo(string.Format("Genre {0} already in DB. Skipping", genre));
            }
            else
            {
                Logger.LogInfo(string.Format("New genre {0} added to DB.", genre));
                db.AddGenre(genre);
                genresAffected++;
            }
        }

        string allAlbumArtists = String.Join(",", albumArtists);

        // ALBUMS
        if (db.HasAlbum(album, allAlbumArtists, year))
        {
            Logger.LogInfo(string.Format("Album {0} already in DB. skipping.", album));
        }
        else
        {
            Logger.LogInfo(string.Format("New album {0} added to DB.", album));
            db.AddAlbum(album, allAlbumArtists, year, totalDisks, totalTracks);
            albumsAffected++;
        }

        string absolutePath;
        if (arduinoMode)
            absolutePath = PathConverter.ToArduinoPath(filename);
        else
            absolutePath = System.IO.Path.GetFullPath(filename);

        // SONGS
        if(db.HasSong(absolutePath))
        {
            Logger.LogWarn(string.Format("Song {0} ({1}) already in DB. skipping.", title, filename));
        }
        else
        {
            Logger.LogInfo(string.Format("New song {0} ({1}) added to DB.", title, filename));
            
            db.AddSong(title, artists, album, allAlbumArtists, genres, length, diskno, trackno, year, absolutePath);
            songsAffected++;
        }
    }

    public void ProcessPlaylist(string filename)
    {
        if(!db.HasPlaylist(filename))
        {
            db.AddPlaylist(filename, arduinoMode);
            playlistsAffected++;
        }
        else
        {
            Logger.LogError(string.Format("Playlist from {0} already exists. Ignoring.", filename));
        }
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
                Logger.LogError(string.Format("Unkown file {0}", file));    
        }

        // do all playlists after all songs are dealt with
        foreach(string playlist in playlists)
        {
            ProcessPlaylist(playlist);
        }

        StringBuilder sb = new();
        
        if(songsAffected != 0)
        {
            sb.Append($"Added {songsAffected} songs");
        }

        if(artistsAffected != 0)
        {
            sb.Append($"\nAdded {artistsAffected} artists");
        }

        if(albumsAffected != 0)
        {
            sb.Append($"\nAdded {albumsAffected} albums");
        }

        if(genresAffected != 0)
        {
            sb.Append($"\nAdded {genresAffected} genres");
        }

        if(playlistsAffected != 0)
        {
            sb.Append($"Added {playlistsAffected} playlists");
        }

        Logger.LogResult(sb.ToString());
    }

    public void RemoveFiles(string[] files, bool prune)
    {
        foreach(string file in files)
        {
            if(IsSongFile(file))
            {
                Logger.LogInfo(string.Format("Removing song {0} from DB.", file));
                db.RemoveSong(file);
                songsAffected++;
            }
            else if(IsPlaylistFile(file))
            {
                Logger.LogInfo(string.Format("Removing playlist {0} from DB.", file));
                db.RemovePlaylist(file);
                playlistsAffected++;
            }
            else
                Logger.LogError(string.Format("Unkown file {0}", file));
        }

        if(prune)
            PruneDB();
        
        StringBuilder sb = new();
        
        if(songsAffected != 0)
        {
            sb.Append($"Removed {songsAffected} songs");
        }

        if(playlistsAffected != 0)
        {
            sb.Append($"Removed {playlistsAffected} playlists");
        }

        Logger.LogResult(sb.ToString());
    }

    public void PruneDB()
    {
        List<uint> unusedAlbums = db.UnusedAlbums();
        List<uint> unusedArtists = db.UnusedArtists();
        List<uint> unusedGenres = db.UnusedGenres();

        foreach (uint albumId in unusedAlbums)
        {
            Logger.LogInfo(string.Format("Pruning unused album {0} from DB.", db.GetAlbumName(albumId)));

            db.RemoveAlbum(albumId);
            albumsAffected++;
        }
        foreach (uint artistId in unusedArtists)
        {
            Logger.LogInfo(string.Format("Pruning unused artist {0} from DB.", db.GetArtistName(artistId)));
            db.RemoveArtist(artistId);
            artistsAffected++;
        }

        foreach (uint genreId in unusedGenres)
        {
            Logger.LogInfo(string.Format("Pruning unused genre {0} from DB.", db.GetGenreName(genreId)));
            db.RemoveGenre(genreId);
            genresAffected++;
        }

        StringBuilder sb = new();
        
        if(artistsAffected != 0)
        {
            sb.Append($"Removed {artistsAffected} artists");
        }

        if(albumsAffected != 0)
        {
            sb.Append($"\nRemoved {albumsAffected} albums");
        }

        if(genresAffected != 0)
        {
            sb.Append($"\nRemoved {genresAffected} genres");
        }

        Logger.LogResult(sb.ToString());
    }
}