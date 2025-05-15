using System;
using System.Data.SQLite;

class DBAgent
{
    private SQLiteConnection conn;

    public DBAgent(string dbFilepath)
    {
        string loc = string.Format("Data Source=\"{0}\"", dbFilepath);
        conn = new SQLiteConnection(loc);
        conn.Open();
    }

    ~DBAgent()
    {
        conn.Close();
    }

    public void InitDB()
    {
        string initDDL = 
        @"CREATE TABLE artists (
    artist_id   INTEGER     PRIMARY KEY NOT NULL,
    artist_name VARCHAR(50)             NOT NULL
);

CREATE TABLE genres (
    genre_id    INTEGER     PRIMARY KEY NOT NULL,
    genre_name  VARCHAR(20) NOT NULL
);

CREATE TABLE albums (
    album_id    INTEGER     PRIMARY KEY NOT NULL,
    artist_id   INTEGER     NOT NULL,
    album_name  VARCHAR(75),
    album_year  INTEGER,
    album_disks INTEGER,
    album_tracks INTEGER,

    FOREIGN KEY (artist_id) REFERENCES artists(artist_id)
);

CREATE TABLE songs (
    song_id     INTEGER     PRIMARY KEY NOT NULL,
    artist_id   INTEGER,
    album_id    INTEGER,
    genre_id    INTEGER,
    song_name   VARCHAR(50),
    song_length INTEGER,
    song_diskno INTEGER,
    song_trackno INTEGER,
    song_filepath    TEXT,

    FOREIGN KEY (artist_id) REFERENCES artists(artist_id),
    FOREIGN KEY (album_id) REFERENCES albums(album_id),
    FOREIGN KEY (genre_id) REFERENCES genres(genre_id)
);

CREATE TABLE playlists (
    playlist_id INTEGER PRIMARY KEY NOT NULL,
    playlist_name TEXT NOT NULL
);

CREATE TABLE playlist_relations (
    playlist_id INTEGER NOT NULL,
    song_id INTEGER NOT NULL,
    
    PRIMARY KEY (playlist_id, song_id),
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id),
    FOREIGN KEY (song_id) REFERENCES songs(song_id)
);";

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = initDDL;
        cmd.ExecuteNonQuery();
    }

    public bool HasArtist(string artist)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT (artist_name) FROM artists WHERE artist_name = $name;";
        cmd.Parameters.AddWithValue("$name", artist);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            return true;
        }

        return false;
    }

    public uint GetArtistId(string artist)
    {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT (artist_id) FROM artists WHERE artist_name = $name";
        cmd.Parameters.AddWithValue("$name", artist);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            result.Read();
            return (uint)result.GetInt32(0);
        }
        else
        {
            return 0;
        }
    }

     public void AddArtist(string artist)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText  = "INSERT INTO artists (artist_name) VALUES ($value);";
        
        cmd.Parameters.AddWithValue("$value", artist);
        cmd.ExecuteNonQuery();
    }

    public bool HasAlbum(string album, string artist, uint year)
    {
        uint artistId = GetArtistId(artist);

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM albums WHERE album_name = $name AND artist_id = $artist_id AND album_year = $year";
        cmd.Parameters.AddWithValue("$name", album);
        cmd.Parameters.AddWithValue("$artist_id", artistId);
        cmd.Parameters.AddWithValue("$year", year);

        var result = cmd.ExecuteReader();
        if(result.HasRows)
            return true;
        return false;
    }

    public uint GetAlbumId(string album, string artist, uint year)
    {
        uint artistId = GetArtistId(artist);

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT (album_id) FROM albums WHERE album_name = $name AND artist_id = $artist_id AND album_year = $year";
        cmd.Parameters.AddWithValue("$name", album);
        cmd.Parameters.AddWithValue("$artist_id", artistId);
        cmd.Parameters.AddWithValue("$year", year);

        var result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            result.Read();
            return (uint)result.GetInt32(0);
        }
        return 0;
    }

    public void AddAlbum(string album, string artist, uint year, uint numDisks, uint numTracks)
    {
        uint artistId = GetArtistId(artist);

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO albums (album_name, artist_id, album_year, album_disks, album_tracks) VALUES ($name, $artist_id, $year, $disks, $tracks)";
        cmd.Parameters.AddWithValue("$name", album);
        cmd.Parameters.AddWithValue("$artist_id", artistId);
        cmd.Parameters.AddWithValue("$year", year);
        cmd.Parameters.AddWithValue("$disks", numDisks);
        cmd.Parameters.AddWithValue("$tracks", numTracks);

        cmd.ExecuteNonQuery();
    }

    public bool HasGenre(string genre)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT (genre_name) FROM genres WHERE genre_name = $name;";
        cmd.Parameters.AddWithValue("$name", genre);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            return true;
        }

        return false;
    }

    public uint GetGenreId(string genre)
    {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT (genre_id) FROM genres WHERE genre_name = $name";
        cmd.Parameters.AddWithValue("$name", genre);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            result.Read();
            return (uint)result.GetInt32(0);
        }
        else
            return 0;
    }

    public void AddGenre(string genre)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        string query = "INSERT INTO genres (genre_name) VALUES ($value);";
        cmd.CommandText = query;
        
        cmd.Parameters.AddWithValue("$value", genre);
        cmd.ExecuteNonQuery();
    }

    public bool HasSong(string title, string artist, string album, uint year)
    {
        uint artistId = GetArtistId(artist);
        uint albumId = GetAlbumId(album, artist, year);

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM songs WHERE song_name = $name AND artist_id = $artist_id AND album_id = $album_id;";
        cmd.Parameters.AddWithValue("$name", title);
        cmd.Parameters.AddWithValue("$artist_id", artistId);
        cmd.Parameters.AddWithValue("$album_id", albumId);

        var result = cmd.ExecuteReader();
        if(result.HasRows)
            return true;
        return false;
    }

    public bool HasSong(string filename)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM songs WHERE song_filepath = $fp;";
        cmd.Parameters.AddWithValue("$fp", filename);

        var result = cmd.ExecuteReader();
        if(result.HasRows)
            return true;
        return false;        
    }

    public uint GetSongId(string filepath)
    {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT (song_id) FROM songs WHERE song_filepath = $fp";
        cmd.Parameters.AddWithValue("$fp", filepath);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            result.Read();
            return (uint)result.GetInt32(0);
        }
        else
            return 0;
    }

    public void AddSong(string title, string artist, string album, string genre, TimeSpan length, 
                        uint numDisks, uint numTracks, uint year, string filepath)
    {
        uint artistId = GetArtistId(artist);
        uint albumId = GetAlbumId(album, artist, year);
        uint genreId = GetGenreId(genre);

        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = 
        "INSERT INTO songs (song_name, artist_id, album_id, genre_id, song_length, song_diskno, song_trackno, song_filepath) VALUES ($title, $artist_id, $album_id, $genre_id, $song_length, $song_diskno, $song_trackno, $fp);";
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$artist_id", artistId);
        cmd.Parameters.AddWithValue("$album_id", albumId);
        cmd.Parameters.AddWithValue("$genre_id", genreId);
        cmd.Parameters.AddWithValue("$song_length", length.ToString());
        cmd.Parameters.AddWithValue("$song_diskno", numDisks);
        cmd.Parameters.AddWithValue("$song_trackno", numTracks);
        cmd.Parameters.AddWithValue("$fp", filepath);

        cmd.ExecuteNonQuery();
    }

    public void RemoveSong(string filepath)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        string absolutePath = System.IO.Path.GetFullPath(filepath);

        cmd.CommandText = "DELETE FROM playlist_relations WHERE song_id = $sid;";
        cmd.Parameters.AddWithValue("$sid", GetSongId(absolutePath));
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM songs WHERE song_filepath = $fp;";
        cmd.Parameters.AddWithValue("$fp", absolutePath);
        cmd.ExecuteNonQuery();
    }

    public bool HasPlaylist(string name)
    {
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM playlists WHERE playlist_name = $name;";
        cmd.Parameters.AddWithValue("$name", name);

        var result = cmd.ExecuteReader();
        if(result.HasRows)
            return true;
        return false;
    }

    public uint GetPlaylistId(string playlist)
    {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT (playlist_id) FROM playlists WHERE playlist_name = $name";
        cmd.Parameters.AddWithValue("$name", playlist);

        SQLiteDataReader result = cmd.ExecuteReader();
        if(result.HasRows)
        {
            result.Read();
            return (uint)result.GetInt32(0);
        }
        else
            return 0;
    }

    private static string GetPlaylistName(string filename)
    {
        string[] split = filename.Split('.');
        string extn = split[split.Length - 1];

        string result = "";

        foreach(string part in split)
        {
            if(!part.Equals(extn))
                result += part;
        }

        return result;
    }

    public void AddPlaylist(string name)
    {
        if(HasPlaylist(name))
        {
            Console.WriteLine("[ERR ] Playlist {0} already exists. Skipping", name);
            return;
        }

        Console.WriteLine("[INFO] Making new playlist from {0}.", name);
        string playlistName = GetPlaylistName(name);
        SQLiteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO playlists (playlist_name) VALUES ($name);";
        cmd.Parameters.AddWithValue("$name", playlistName);
        cmd.ExecuteNonQuery();

        uint playlistId = GetPlaylistId(playlistName);

        var lines = System.IO.File.ReadAllLines(name);
        foreach(string line in lines)
        {
            string absolutePath = System.IO.Path.GetFullPath(line);
            if(HasSong(absolutePath))
            {
                uint songId = GetSongId(absolutePath);

                cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO playlist_relations (playlist_id, song_id) VALUES ($pid, $sid);";
                cmd.Parameters.AddWithValue("$pid", playlistId);
                cmd.Parameters.AddWithValue("$sid", songId);
                cmd.ExecuteNonQuery();

                Console.WriteLine("[INFO] Added song at {0} to playlist {1}", line, playlistName);
            }
        }
    }

    public void RemovePlaylist(string filename)
    {
        SQLiteCommand cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM playlist_relations WHERE playlist_id = $pid;";
        cmd.Parameters.AddWithValue("$pid", GetPlaylistId(GetPlaylistName(filename)));
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM playlists WHERE playlist_name = $name;";
        cmd.Parameters.AddWithValue("$name", GetPlaylistName(filename));
        cmd.ExecuteNonQuery();
    }
}