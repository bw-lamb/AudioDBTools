using System;
using System.Collections.Generic;
using System.Data.SQLite;
class DBAgent
{
    private readonly string dbLocation;
    public DBAgent(string dbFilepath)
    {
        dbLocation = string.Format("Data Source=\"{0}\"", dbFilepath);
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
    album_tracks INTEGER
);

CREATE TABLE songs (
    song_id     INTEGER     PRIMARY KEY NOT NULL,
    album_id    INTEGER,
    song_name   VARCHAR(50),
    song_length INTEGER,
    song_diskno INTEGER,
    song_trackno INTEGER,
    song_filepath    TEXT,

    FOREIGN KEY (album_id) REFERENCES albums(album_id)
);

CREATE TABLE playlists (
    playlist_id INTEGER PRIMARY KEY NOT NULL,
    playlist_name TEXT NOT NULL
);

CREATE TABLE artist_relations (
    artist_id INTEGER NOT NULL,
    song_id INTEGER NOT NULL,
    
    PRIMARY KEY (artist_id, song_id),
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id),
    FOREIGN KEY (song_id) REFERENCES songs(song_id)
);

CREATE TABLE genre_relations (
    genre_id INTEGER NOT NULL,
    song_id INTEGER NOT NULL,
    
    PRIMARY KEY (genre_id, song_id),
    FOREIGN KEY (genre_id) REFERENCES genres(genre_id),
    FOREIGN KEY (song_id) REFERENCES songs(song_id)
);

CREATE TABLE playlist_relations (
    playlist_id INTEGER NOT NULL,
    song_id INTEGER NOT NULL,
    
    PRIMARY KEY (playlist_id, song_id),
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id),
    FOREIGN KEY (song_id) REFERENCES songs(song_id)
);";
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = initDDL;
            cmd.ExecuteNonQuery();
        }
    }

    public bool HasArtist(string artist)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (artist_name) FROM artists WHERE artist_name = $name;";
            cmd.Parameters.AddWithValue("$name", artist);

            using (SQLiteDataReader result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public uint GetArtistId(string artist)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (artist_id) FROM artists WHERE artist_name = $name";
            cmd.Parameters.AddWithValue("$name", artist);

            using (SQLiteDataReader result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return (uint)result.GetInt32(0);
                }
            }
                return 0;
        }
    }

    public string? GetArtistName(uint artistId)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT artist_name FROM artists WHERE artist_id = $aid;";
            cmd.Parameters.AddWithValue("$aid", artistId);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return result.GetString(0);
                }
            }
            return null;
        }
    }

    public List<uint> UnusedArtists()
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT artist_id FROM artists WHERE artist_id NOT IN (SELECT DISTINCT artist_id FROM artist_relations);";

            List<uint> unused = new List<uint>();

            using (var result = cmd.ExecuteReader())
            {
                while (result.HasRows)
                {
                    if (result.Read())
                        unused.Add((uint)result.GetInt32(0));
                }
            }

            return unused;
        }
    }

     public void AddArtist(string artist)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO artists (artist_name) VALUES ($value);";
            cmd.Parameters.AddWithValue("$value", artist);
            cmd.ExecuteNonQuery();
        }
    }
    
    public void RemoveArtist(uint artistid)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM artists WHERE artist_id = $aid;";
            cmd.Parameters.AddWithValue("$aid", artistid);
            cmd.ExecuteNonQuery();
        }
    }

    public bool HasAlbum(string album, string artist, uint year)
    {
        uint artistId = GetArtistId(artist);

        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM albums WHERE album_name = $name AND artist_id = $artist_id AND album_year = $year";
            cmd.Parameters.AddWithValue("$name", album);
            cmd.Parameters.AddWithValue("$artist_id", artistId);
            cmd.Parameters.AddWithValue("$year", year);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                    return true;
            }
            return false;
        }
    }

    public uint GetAlbumId(string album, string artist, uint year)
    {
        uint artistId = GetArtistId(artist);

        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (album_id) FROM albums WHERE album_name = $name AND artist_id = $artist_id AND album_year = $year";
            cmd.Parameters.AddWithValue("$name", album);
            cmd.Parameters.AddWithValue("$artist_id", artistId);
            cmd.Parameters.AddWithValue("$year", year);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return (uint)result.GetInt32(0);
                }
            }
        }
        return 0;
    }

    public string? GetAlbumName(uint albumId)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT album_name FROM albums WHERE album_id = $aid;";
            cmd.Parameters.AddWithValue("$aid", albumId);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return result.GetString(0);
                }
            }
        }
        return null;
    }

    public List<uint> UnusedAlbums()
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT album_id FROM albums WHERE album_id NOT IN (SELECT DISTINCT album_id FROM songs);";

            List<uint> unused = new List<uint>();

            using (var result = cmd.ExecuteReader())
            {
                while (result.HasRows)
                {
                    if (result.Read())
                        unused.Add((uint)result.GetInt32(0));
                }
            }
            return unused;
        }
    }

    public void AddAlbum(string album, string artist, uint year, uint numDisks, uint numTracks)
    {
        uint artistId = GetArtistId(artist);

        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO albums (album_name, artist_id, album_year, album_disks, album_tracks) VALUES ($name, $artist_id, $year, $disks, $tracks)";
            cmd.Parameters.AddWithValue("$name", album);
            cmd.Parameters.AddWithValue("$artist_id", artistId);
            cmd.Parameters.AddWithValue("$year", year);
            cmd.Parameters.AddWithValue("$disks", numDisks);
            cmd.Parameters.AddWithValue("$tracks", numTracks);

            cmd.ExecuteNonQuery();
        }
    }

    public void RemoveAlbum(uint albumId)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();

        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM albums WHERE album_id = $aid;";
            cmd.Parameters.AddWithValue("$aid", albumId);
            cmd.ExecuteNonQuery();
        }
    }
    
    public bool HasGenre(string genre)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();

        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (genre_name) FROM genres WHERE genre_name = $name;";
            cmd.Parameters.AddWithValue("$name", genre);

            using (SQLiteDataReader result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public uint GetGenreId(string genre)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (genre_id) FROM genres WHERE genre_name = $name";
            cmd.Parameters.AddWithValue("$name", genre);

            using (SQLiteDataReader result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return (uint)result.GetInt32(0);
                }
            }
            return 0;
        }
    }

    public string? GetGenreName(uint genreId)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT genre_name FROM genres WHERE genre_id = $gid;";
            cmd.Parameters.AddWithValue("$gid", genreId);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return result.GetString(0);
                }
            }
        }
        return null;
    }

    public List<uint> UnusedGenres()
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT genre_id FROM genres WHERE genre_id NOT IN (SELECT DISTINCT genre_id FROM genre_relations);";

            List<uint> unused = new List<uint>();

            using (var result = cmd.ExecuteReader())
            {
                while (result.HasRows)
                {
                    if (result.Read())
                        unused.Add((uint)result.GetInt32(0));
                }
            }

            return unused;
        }
    }
    public void AddGenre(string genre)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            string query = "INSERT INTO genres (genre_name) VALUES ($value);";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("$value", genre);

            cmd.ExecuteNonQuery();
        }
    }

    public void RemoveGenre(uint genreId)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM genres WHERE genre_id = $gid;";
            cmd.Parameters.AddWithValue("$gid", genreId);
            cmd.ExecuteNonQuery();
        }
    }

    public bool HasSong(string filepath)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM songs WHERE song_filepath = $fp;";
            cmd.Parameters.AddWithValue("$fp", filepath);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                    return true;
            }
        }
        return false;
    }

    public uint GetSongId(string filepath)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {

            cmd.CommandText = "SELECT (song_id) FROM songs WHERE song_filepath = $fp";
            cmd.Parameters.AddWithValue("$fp", filepath);

            using (SQLiteDataReader result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return (uint)result.GetInt32(0);
                }
            }
        }
        return 0;
    }

    public void AddSong(string title, string[] artists, string album, string albumArtist, string[] genres, TimeSpan length,
                        uint numDisks, uint numTracks, uint year, string filepath)
    {
        uint[] artistIds = new uint[artists.Length];
        for (int i = 0; i < artists.Length; i++)
        {
            artistIds[i] = GetArtistId(artists[i]);
        }

        uint[] genreIds = new uint[genres.Length];
        for (int i = 0; i < genres.Length; i++)
        {
            genreIds[i] = GetGenreId(genres[i]);
        }

        uint albumId = GetAlbumId(album, albumArtist, year);

        using (SQLiteConnection conn = new(dbLocation))
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText =
            "INSERT INTO songs (song_name, album_id, song_length, song_diskno, song_trackno, song_filepath) VALUES ($title, $album_id, $song_length, $song_diskno, $song_trackno, $fp);";
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$album_id", albumId);
            cmd.Parameters.AddWithValue("$song_length", length.ToString());
            cmd.Parameters.AddWithValue("$song_diskno", numDisks);
            cmd.Parameters.AddWithValue("$song_trackno", numTracks);
            cmd.Parameters.AddWithValue("$fp", filepath);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        uint songId = GetSongId(filepath);

        foreach (uint aId in artistIds)
        {
            using (SQLiteConnection conn = new(dbLocation))
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO artist_relations (artist_id, song_id) VALUES ($aid, $sid);";
                cmd.Parameters.AddWithValue("$aid", aId);
                cmd.Parameters.AddWithValue("$sid", songId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        foreach (uint gId in genreIds)
        {
            using (SQLiteConnection conn = new(dbLocation))
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO genre_relations (genre_id, song_id) VALUES ($gid, $sid);";
                cmd.Parameters.AddWithValue("$gid", gId);
                cmd.Parameters.AddWithValue("sid", songId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void RemoveSong(string filepath)
    {
        string absolutePath = System.IO.Path.GetFullPath(filepath);

        if(!HasSong(absolutePath))
        {
            Logger.LogWarn($"Song from file {absolutePath} not in DB. Ignoring.");
            return;
        }

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM artist_relations WHERE song_id = $sid;";
                cmd.Parameters.AddWithValue("$sid", GetSongId(absolutePath));
                cmd.ExecuteNonQuery();
            }
        }
        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM genre_relations WHERE song_id = $sid;";
                cmd.Parameters.AddWithValue("$sid", GetSongId(absolutePath));
                cmd.ExecuteNonQuery();
            }
        }

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM playlist_relations WHERE song_id = $sid;";
                cmd.Parameters.AddWithValue("$sid", GetSongId(absolutePath));
                cmd.ExecuteNonQuery();
            }
        }

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM songs WHERE song_filepath = $fp;";
                cmd.Parameters.AddWithValue("$fp", absolutePath);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public bool HasPlaylist(string filename)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();
        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM playlists WHERE playlist_name = $name;";
            cmd.Parameters.AddWithValue("$name", GetPlaylistName(filename));

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                    return true;
            }
        }
        return false;
    }

    public uint GetPlaylistId(string playlist)
    {
        using SQLiteConnection conn = new(dbLocation);
        conn.Open();

        using (SQLiteCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT (playlist_id) FROM playlists WHERE playlist_name = $name";
            cmd.Parameters.AddWithValue("$name", playlist);

            using (var result = cmd.ExecuteReader())
            {
                if (result.HasRows)
                {
                    result.Read();
                    return (uint)result.GetInt32(0);
                }
            }
        }
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
            Logger.LogError($"Playlist {name} already exists. Skipping");
            return;
        }

        Logger.LogInfo($"Making new playlist from {name}.");
        string playlistName = GetPlaylistName(name);

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO playlists (playlist_name) VALUES ($name);";
                cmd.Parameters.AddWithValue("$name", playlistName);
                cmd.ExecuteNonQuery();
            }
        }
        uint playlistId = GetPlaylistId(playlistName);

        var lines = System.IO.File.ReadAllLines(name);
        foreach(string line in lines)
        {
            string absolutePath = System.IO.Path.GetFullPath(line);
            if(HasSong(absolutePath))
            {
                uint songId = GetSongId(absolutePath);

                using (SQLiteConnection conn = new(dbLocation))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO playlist_relations (playlist_id, song_id) VALUES ($pid, $sid);";
                        cmd.Parameters.AddWithValue("$pid", playlistId);
                        cmd.Parameters.AddWithValue("$sid", songId);
                        cmd.ExecuteNonQuery();
                    }
                }
                Logger.LogInfo($"Added song at {line} to playlist {playlistName}");
            }
        }
    }

    public void RemovePlaylist(string filename)
    {
        if(!HasPlaylist(filename))
        {
            Logger.LogWarn($"Playlist from file {filename} not in DB. Ignoring.");
            return;
        }

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM playlist_relations WHERE playlist_id = $pid;";
                cmd.Parameters.AddWithValue("$pid", GetPlaylistId(GetPlaylistName(filename)));
                cmd.ExecuteNonQuery();
            }
        }

        using (SQLiteConnection conn = new(dbLocation))
        {
            conn.Open();
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM playlists WHERE playlist_name = $name;";
                cmd.Parameters.AddWithValue("$name", GetPlaylistName(filename));
                cmd.ExecuteNonQuery();
            }
        }
    }
}