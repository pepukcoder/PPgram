using System;
using System.IO;
using PPgram.Shared;

namespace PPgram.App;

internal class FSManager
{
    private static readonly AppState appState = AppState.Instance;
    public static void SaveBinary(string sha256_hash, byte[] binary, string fileName, bool isPreview)
    {
        // DIALOGFIX if unable to get downloads folder
        if (appState.DownloadsFolder == null) return;

        string hashFolder = Path.Combine(PPpath.FileCacheFolder, sha256_hash);
        RestoreDirs(hashFolder);

        string filePath;
        if (isPreview) filePath = Path.Combine(hashFolder, fileName);
        else filePath = Path.Combine(appState.DownloadsFolder, fileName);

        // skip is file is already downloaded
        if (File.Exists(filePath)) return;

        try
        {
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fileStream);
            writer.Write(binary);
        }
        catch (Exception ex)
        {
            // DIALOGFIX if unable to save file
            Console.WriteLine("An error occurred: " + ex.Message);
            return;
        }
        if (!isPreview)
        {
            File.CreateSymbolicLink(Path.Combine(hashFolder, fileName), filePath);
        }
    }
    public static void CreateFile(string path, string data)
    {
        RestoreDirs(path);
        using StreamWriter writer = new(File.OpenWrite(path));
        writer.Write(data);
    }
    private static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
