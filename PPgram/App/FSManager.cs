using System;
using System.IO;
using System.Text;
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
            if (isPreview) filePath = Path.Combine(PPpath.FileCacheFolder, sha256_hash + ".preview");
            else
            {
                filePath = Path.Combine(appState.DownloadsFolder, fileName);
                CreateFile(Path.Combine(PPpath.FileCacheFolder, sha256_hash + ".link"), filePath);
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
    private static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
