using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
    public static void CreateJsonFile(string path, object data)
    {
        string json = JsonSerializer.Serialize(data, options: new() { WriteIndented = true } );
        CreateFile(path, json);
    }
    public static async Task<T> LoadFromJsonFile<T>(string path)
    {
        string data = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(data) ?? throw new InvalidDataException("Unable to deserialize json file");
    }
    public static void RestoreDirs(string path) => Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
}
