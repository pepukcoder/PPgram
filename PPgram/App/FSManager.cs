using System;
using System.IO;
using System.Text;
using System.Text.Json;
using PPgram.Shared;

namespace PPgram.App;

internal class FSManager
{
    private static readonly AppState appState = AppState.Instance;
    public static void CreateFile(string path, byte[] data)
    {
        RestoreDirs(path);
        using BinaryWriter writer = new(File.OpenWrite(path));
        writer.Write(data);
    }
    public static void CreateFile(string path, string data) => CreateFile(path, Encoding.UTF8.GetBytes(data));
    public static bool IsHashed(string hash) => File.Exists(Path.Combine(PPPath.FileCacheFolder, hash + ".link"));
    public static void CreateJsonFile(string path, object data)
    {
        string json = JsonSerializer.Serialize(data, options: new() { WriteIndented = true } );
        CreateFile(path, json);
    }
    public static T LoadFromJsonFile<T>(string path)
    {
        string data = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(data) ?? throw new InvalidDataException("Unable to deserialize json file");
    }
    public static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
