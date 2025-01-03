using System;
using System.IO;
using System.Text;
using System.Text.Json;
using PPgram.Net;
using PPgram.Shared;

namespace PPgram.App;

internal class FSManager
{
    private static readonly AppState appState = AppState.Instance;
    public static void SaveBinary(string sha256_hash, byte[] binary, string fileName, bool isPreview)
    {
        try
        {
            // DIALOGFIX if unable to get downloads folder
            if (appState.DownloadsFolder == null) return;

            string filePath;
            if (isPreview) filePath = Path.Combine(PPPath.FileCacheFolder, sha256_hash + ".preview");
            else
            {
                filePath = Path.Combine(appState.DownloadsFolder, fileName);
                CreateFile(Path.Combine(PPPath.FileCacheFolder, sha256_hash + ".link"), filePath);
            }
            CreateFile(filePath, binary);
        }
        catch (Exception ex)
        {
            // DIALOGFIX if unable to save file
            Console.WriteLine("An error occurred: " + ex.Message);
        } 
    }
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
    private static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
