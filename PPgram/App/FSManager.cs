using System;
using System.Diagnostics;
using System.IO;
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
        
            string hashFolder = Path.Combine(PPpath.FileCacheFolder, sha256_hash);
            // save regular files to downloads and previews to cache
            string filePath;
            if (isPreview) filePath = Path.Combine(hashFolder, fileName);
            else filePath = Path.Combine(appState.DownloadsFolder, fileName);

            // skip if file is already downloaded
            if (File.Exists(filePath)) return;

            CreateFile(filePath, binary);

            if (!isPreview)
            {
                RestoreDirs(Path.Combine(hashFolder, fileName));
                File.CreateSymbolicLink(Path.Combine(hashFolder, fileName), filePath);
            }
        }
        catch (Exception ex)
        {
            // DIALOGFIX if unable to save file
            Console.WriteLine("An error occurred: " + ex.Message);
        } 
    }
    public static void CreateFile(string path, object data)
    {
        RestoreDirs(path);
        using StreamWriter writer = new(File.OpenWrite(path));
        writer.Write(data);
    }
    private static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
