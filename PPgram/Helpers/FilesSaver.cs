using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PPgram.Helpers;

internal class FilesSaver
{
    private static readonly string localAppPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string basePath = Path.Combine(localAppPath, "PPgram");

    // default folder for all binaries to be saved
    private string saveFolder;
    private string symlinksFolder;

    static string GetDownloadFolderPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // On Linux, check the XDG user directories
            string? xdgDownloads = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
            if (!string.IsNullOrEmpty(xdgDownloads))
            {
                return xdgDownloads;
            }
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public FilesSaver()
    {
        saveFolder = Path.Combine(GetDownloadFolderPath(), "PPgram");
        symlinksFolder = Path.Combine(basePath, "Documents");

        CreateDirectoryIfNotExists(saveFolder);
        CreateDirectoryIfNotExists(symlinksFolder);
    }

    public void SaveBinary(string sha256_hash, byte[] binary, string fileName, bool isPreview)
    {
        string hashFolder = Path.Combine(symlinksFolder, sha256_hash);
        CreateDirectoryIfNotExists(hashFolder);

        string filePath;
        if (isPreview)
        {
            filePath = Path.Combine(hashFolder, fileName);
        }
        else
        {
            filePath = Path.Combine(saveFolder, fileName);
        }

        // binary already downloaded, skip
        if (File.Exists(filePath))
        {
            return;
        }

        try
        {

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    writer.Write(binary);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
            return;
        }

        if (!isPreview) {
            File.CreateSymbolicLink(Path.Combine(hashFolder, fileName), filePath);
        }
    }
}
