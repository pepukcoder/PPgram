using PPgram.Shared;
using System;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using PPgram.MVVM.Models.User;
using System.Collections.Concurrent;

namespace PPgram.App;

internal class CacheManager
{
    private readonly AppState appState = AppState.Instance;
    private readonly SqliteConnection connection;
    private readonly ConcurrentDictionary<int, ProfileModel> cachedProfiles = new();
    public CacheManager()
    {
        FSManager.RestoreDirs(PPPath.CacheDBFile);
        bool init = !File.Exists(PPPath.CacheDBFile);
        connection = new(@$"Data Source={PPPath.CacheDBFile}");
        connection.Open();
        if (init)
        {
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
            using SqliteCommand command = new(query, connection);
            command.ExecuteNonQuery();
        };
    }
    ~CacheManager()
    {
        connection.Close();
    }
    private string? ReadHash(string query, string hash)
    {
        try
        {
            using SqliteCommand command = new(query, connection);
            command.Parameters.AddWithValue("$hash", hash);
            SqliteDataReader reader = command.ExecuteReader();
            if (reader.HasRows && reader.Read()) return reader.IsDBNull(1) ? null : reader.GetString(1);
            else return null;
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); return null; }
    }
    public void CacheFile(string hash, string name, string? temp_preview_path, string? temp_file_path)
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
        using SqliteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$file_path", file_path ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$preview_path", preview_path ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
    public void CacheAvatar(string hash, string temp_file_path)
    {
        string file_path = Path.Combine(PPPath.AvatarCacheFolder, Path.ChangeExtension(Path.GetRandomFileName(), "ppavtr"));
        FSManager.RestoreDirs(file_path);
        File.Move(temp_file_path, file_path, true);
        string query = @"
        INSERT INTO avatars (hash,file_path)
        VALUES ($hash,$file_path)
        ON CONFLICT(hash) DO UPDATE SET
        file_path = COALESCE(avatars.file_path, excluded.file_path);";
        using SqliteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$file_path", file_path);
        command.ExecuteNonQuery();
    }
    public void CacheProfile(int id, ProfileModel profile) => cachedProfiles.TryAdd(id, profile);
    public void UpdateProfile(int id, ProfileModel profile)
    {
        if (!cachedProfiles.TryGetValue(id, out ProfileModel? old)) return;
        old.Avatar = profile.Avatar;
        old.Name = profile.Name;
        old.Username = profile.Username;
        old.Color = profile.Color;
    }
    public string? GetCachedFile(string hash, bool preview = false)
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
    public string? GetCachedAvatar(string hash)
    {
        string query = @$"
        SELECT hash, file_path 
        FROM avatars 
        WHERE hash = $hash 
        LIMIT 1;";
        return ReadHash(query, hash);
    }
    public ProfileModel GetCachedProfile(int id) => cachedProfiles[id];
    public void DeleteCachedFile(string hash, bool avatar = false)
    {
        string query;
        if (avatar) query = @"DELETE FROM avatars WHERE hash = $hash;";
        else query = @"DELETE FROM files WHERE hash = $hash;";
        using SqliteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        command.ExecuteNonQuery();
    }
    public bool IsFileCached(string hash, bool avatar = false)
    {
        string query;
        if (avatar) query = @"SELECT EXISTS(SELECT 1 FROM avatars WHERE hash = $hash);";
        else query = @"SELECT EXISTS(SELECT 1 FROM files WHERE hash = $hash);";
        using SqliteCommand command = new(query, connection);
        command.Parameters.AddWithValue("$hash", hash);
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }
    public bool IsProfileCached(int id) => cachedProfiles.ContainsKey(id);
}