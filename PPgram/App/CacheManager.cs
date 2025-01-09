using PPgram.Shared;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace PPgram.App;

internal static class CacheManager
{
    private static readonly AppState appState = AppState.Instance;
    private static SQLiteConnection Init()
    {
        FSManager.RestoreDirs(PPPath.CacheDBFile);
        string connectionString = @$"Data Source={PPPath.CacheDBFile};New=false";
        SQLiteConnection connection = new(connectionString);
        if (!File.Exists(PPPath.CacheDBFile))
        {
            connection.Open();
            string query = @"
            CREATE TABLE IF NOT EXISTS files 
            (
                hash TEXT NOT NULL PRIMARY KEY,
                file_path TEXT,
                preview_path TEXT
            );
            CREATE TABLE IF NOT EXISTS avatars 
            (
                hash TEXT NOT NULL PRIMARY KEY,
                file_path TEXT NOT NULL
            );";
            using SQLiteCommand command = new(query, connection);
            command.ExecuteNonQuery();
            connection.Close();
        };
        return connection;
    }
    private static string? ReadHash(string query, string hash)
    {
        try
        {
            using SQLiteConnection connection = Init();
            connection.Open();
            using SQLiteCommand command = new(query, connection);
            command.Parameters.AddWithValue("$hash", hash);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.HasRows && reader.Read()) return reader.IsDBNull(1) ? null : reader.GetString(1);
            else return null;
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); return null; }
    }
    public static void CacheFile(string hash, string name, string? temp_preview_path, string? temp_file_path)
    {
        if (appState.DownloadsFolder == null) return;
        string? preview_path = null;
        string? file_path = null;
        if (temp_preview_path != null)
        {
            preview_path = Path.Combine(PPPath.FileCacheFolder, Path.ChangeExtension(Path.GetRandomFileName(), "preview"));
            FSManager.RestoreDirs(preview_path);
            File.Move(temp_preview_path, preview_path, true);
        }
        if (temp_file_path != null)
        {
            file_path = Path.Combine(appState.DownloadsFolder, name);
            FSManager.RestoreDirs(file_path);
            File.Move(temp_file_path, file_path, true);
        }
        string query = @"
        INSERT INTO files (hash,file_path,preview_path)
        VALUES ($hash,$file_path,$preview_path)
        ON CONFLICT(hash) DO UPDATE SET
        file_path = COALESCE(files.file_path, excluded.file_path),
        preview_path = COALESCE(files.preview_path, excluded.preview_path);";
        using SQLiteConnection connection = Init();
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$file_path", file_path ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$preview_path", preview_path ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
    public static string? GetCachedFile(string hash, bool preview = false)
    {
        string query = preview ?
        @$"SELECT hash, preview_path 
        FROM files 
        WHERE hash = $hash 
        LIMIT 1;"
        :
        @$"SELECT hash, file_path 
        FROM files 
        WHERE hash = $hash 
        LIMIT 1;";
        return ReadHash(query, hash);
    }
    public static string? GetCachedAvatar(string hash)
    {
        string query = @$"
        SELECT hash, file_path 
        FROM avatars 
        WHERE hash = $hash 
        LIMIT 1;";
        return ReadHash(query, hash);
    }
    public static void DeleteCached(string hash, bool avatar = false)
    {
        string query;
        if (avatar) query = @"DELETE FROM avatars WHERE hash = $hash;";
        else query = @"DELETE FROM files WHERE hash = $hash;";
        using SQLiteConnection connection = Init();
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        command.ExecuteNonQuery();
    }
    public static bool IsCached(string hash, bool avatar = false)
    {
        string query;
        if (avatar) query = @"SELECT EXISTS(SELECT 1 FROM avatars WHERE hash = $hash);";
        else query = @"SELECT EXISTS(SELECT 1 FROM files WHERE hash = $hash);";
        using SQLiteConnection connection = Init();
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }
}