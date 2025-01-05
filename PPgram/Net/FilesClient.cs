using PPgram.Net;
using PPgram.Shared;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

internal class FilesClient
{
    // Controls suspension and resumption of the Listen method
    private const int CHUNK_SIZE = 1 * 1024 * 1024; // 1 MiB
    const int MESSAGE_ALLOCATION_SIZE = 4 * 1024 * 1024; // 4 MiB

    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource cts = new();
    private readonly ConcurrentQueue<object> requests = [];
    private readonly Mutex mutex = new();
    public async Task<bool> Connect(ConnectionOptions options)
    {
        try
        {
            if (client != null) throw new InvalidOperationException("Client is already connected");
            client = new();
            Task task = client.ConnectAsync(options.Host, options.FilesPort);
            if (await Task.WhenAny(task, Task.Delay(5000)) != task) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            Thread listenThread = new(() => Listen(cts.Token)) { IsBackground = true };
            listenThread.Start();
            return true;
        }
        catch { return false; }
    }
    private void Listen(CancellationToken ct)
    {
        TcpConnection connection = new();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                connection.ReadStream(stream);
                if (connection.IsReady)
                {
                    string response = connection.GetResponseAsString();
                    Debug.WriteLine(response);
                    JsonNode? rootNode = JsonNode.Parse(response);
                    string? r_method = rootNode?["method"]?.GetValue<string>();
                    string? r_error = rootNode?["error"]?.GetValue<string>();
                    bool? ok = rootNode?["ok"]?.GetValue<bool>();
                    if (r_method != null && requests.TryDequeue(out object? tcs))
                    {
                        switch (r_method)
                        {
                            case "upload_file":
                                if (ok == false) return;
                                string? hash = rootNode?["sha256_hash"]?.GetValue<string>();
                                if (hash == null) return;
                                if (tcs is TaskCompletionSource<string> upload_tcs) upload_tcs.TrySetResult(hash);
                                break;
                            case "download_file":
                                if (ok == false) return;
                                JsonNode? previewjson = rootNode?["preview_metadata"];
                                JsonNode? filejson = rootNode?["file_metadata"];
                                string? preview_path = null;
                                string? file_path = null;
                                if (previewjson != null) preview_path = DownloadFromNode(previewjson);
                                if (filejson != null) file_path = DownloadFromNode(filejson);
                                if (tcs is TaskCompletionSource<(string?, string?)> dload_tcs) dload_tcs.TrySetResult((preview_path, file_path));
                                break;
                        }
                    }   
                }
            }
            catch { Disconnect(); }
        }
    }
    public void Disconnect()
    {
        if (client?.Client.Connected == true) client?.Client.Disconnect(false);
        cts.Cancel();
        requests.Clear();
        client = null;
        cts = new();
    }
    public async Task<string> UploadFile(string path, bool is_media, bool compress)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        var payload = new
        {
            method = "upload_file",
            name = Path.GetFileName(path),
            is_media,
            compress
        };
        SendJson(payload);
        mutex.WaitOne();
        try
        {
            // send file length
            FileInfo fileInfo = new(path);
            byte[] lengthBytes = BitConverter.GetBytes(fileInfo.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            stream?.Write(lengthBytes);
            // send file
            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[CHUNK_SIZE];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                stream?.Write(buffer, 0, bytesRead);
            }
        }
        finally { mutex.ReleaseMutex(); }
        TaskCompletionSource<string> tcs = new();
        requests.Enqueue(tcs);
        return await tcs.Task;
    }
    public async Task<(string?, string?)> DownloadFile(string sha256Hash, DownloadMode mode)
    {
        var payload = new
        {
            method = "download_file",
            sha256_hash = sha256Hash,
            mode = mode.ToString()
        };
        TaskCompletionSource<(string?, string?)> tcs = new();
        SendJson(payload);
        requests.Enqueue(tcs);
        return await tcs.Task;
    }
    private void SendJson(object data)
    {
        mutex.WaitOne();
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(TcpConnection.BuildJsonRequest(request));
        }
        catch { Disconnect(); }
        finally { mutex.ReleaseMutex(); }
    }
    private string DownloadFromNode(JsonNode node)
    {
        ulong expected_size = node?["file_size"]?.GetValue<ulong>() ?? throw new JsonException("Unable to deserialize file size");
        string temp_path = Path.Combine(PPPath.FileCacheFolder, Path.GetRandomFileName());
        byte[] buffer = new byte[CHUNK_SIZE];
        int bytesRead;
        ulong totalRead = 0;
        using FileStream fs = new(temp_path, FileMode.Append, FileAccess.Write);
        while (stream != null && totalRead < expected_size)
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) break; 
            fs.Write(buffer.AsSpan()[..bytesRead]);
            totalRead += (ulong)bytesRead;
        }
        return temp_path;
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
}
