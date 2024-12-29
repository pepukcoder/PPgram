using PPgram.App;
using PPgram.MVVM.Models.File;
using PPgram.Net;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

internal class FilesClient
{
    private TcpClient? client;
    private NetworkStream? stream;

    // Controls suspension and resumption of the Listen method
    private const int chunkSize = 64 * 1024 * 1024; // 64 MiB
    const int MESSAGE_ALLOCATION_SIZE = 4 * 1024 * 1024; // 4 MiB

    public async Task<bool> Connect(ConnectionOptions options)
    {
        try
        {
            if (client != null) throw new InvalidOperationException("Client is already connected");
            client = new();
            Task task = client.ConnectAsync(options.Host, options.FilesPort);
            if (await Task.WhenAny(task, Task.Delay(5000)) != task) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            return true;
        }
        catch { return false; }
    }
    public void Disconnect()
    {
        if (client?.Client.Connected == true) client?.Client.Disconnect(false);
        client = null;
    }
    private void SendJson(object data)
    {
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(JsonConnection.BuildJsonRequest(request));
        }
        catch { Disconnect(); }
    }
    public string? UploadFile(string path)
    {
        FileInfo fileInfo = new(path);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

        var payload = new
        {
            method = "upload_file",
            name = Path.GetFileName(path),
            is_media = false,
            compress = false
        };
        SendJson(payload);

        using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[chunkSize];
        uint fileBytesSent = 0;
        int bytesRead;

        byte[] lengthBytes = BitConverter.GetBytes(fileInfo.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        stream?.Write(lengthBytes);

        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            stream?.Write(buffer, 0, bytesRead);
            fileBytesSent += (uint)bytesRead;
        }

        // Read the response
        JsonConnection jsonConnection = new();
        while (!jsonConnection.IsReady) jsonConnection.ReadStream(stream);
        string response = jsonConnection.GetResponseAsString();

        HandleJsonResponse(response);

        JsonNode? rootNode = JsonNode.Parse(response);
        return rootNode?["sha256_hash"]?.GetValue<string>();
    }
    private void ReadUntilFilled(byte[] buffer, int offset, long expected_size)
    {
        if (stream == null) return;
        long size = 0;
        while (size < expected_size)
        {
            int read = stream.Read(buffer, offset + (int)size, (int)(expected_size - size));
            if (read == 0) throw new EndOfStreamException("The connection was closed before the requested size was read.");
            size += read;
        }
    }
    public MetadataModel? DownloadMetadata(string sha256Hash)
    {
        var payload = new
        {
            method = "download_metadata",
            sha256_hash = sha256Hash
        };
        SendJson(payload);

        JsonConnection jsonConnection = new();
        while (!jsonConnection.IsReady) jsonConnection.ReadStream(stream);

        DownloadMetadataResponseModel? metadatas = jsonConnection.GetResponseAsJson<DownloadMetadataResponseModel>();
        return metadatas?.Metadatas[0];
    }
    public void DownloadFiles(string sha256Hash, bool previewsOnly = false)
    {
        var payload = new
        {
            method = "download_file",
            sha256_hash = sha256Hash,
            previews_only = previewsOnly
        };
        string request = JsonSerializer.Serialize(payload);
        stream?.Write(JsonConnection.BuildJsonRequest(request));


        JsonConnection jsonConnection = new();
        while (!jsonConnection.IsReady) jsonConnection.ReadStream(stream);

        DownloadMetadataResponseModel? metadatas = jsonConnection.GetResponseAsJson<DownloadMetadataResponseModel>();
        if (metadatas == null) { return; }

        string current_file_name = metadatas.Metadatas[0].FileName;
        long current_file_size = metadatas.Metadatas[0].FileSize;

        while (metadatas.Metadatas.Count != 0)
        {
            byte[] binary = new byte[current_file_size];
            ReadUntilFilled(binary, 0, current_file_size);

            // print recieved file size
            Debug.WriteLine(binary.Length.ToString());

            FSManager.SaveBinary(sha256Hash, binary, metadatas.Metadatas[0].FileName, false);

            metadatas.Metadatas.RemoveAt(0);

            if (metadatas.Metadatas.Count != 0)
            {
                current_file_name = metadatas.Metadatas[0].FileName;
                current_file_size = metadatas.Metadatas[0].FileSize;
            }
            else
            {
                break;
            }
        }
    }
    private void HandleJsonResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();

        if (ok == false && r_method != null && r_error != null)
        {
            Debug.WriteLine($"Error: {r_error}");
            return;
        }
        if (r_method == "upload_file")
        {
            string? sha256_hash = rootNode?["sha256_hash"]?.GetValue<string>();
            if (sha256_hash == null) return;
            Debug.WriteLine(sha256_hash);
        }
    }
}
