using PPgram.App;
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
using System.Xml.Linq;

internal class FilesClient
{
    // Controls suspension and resumption of the Listen method
    private const int CHUNK_SIZE = 1 * 1024 * 1024; // 1 MiB
    const int MESSAGE_ALLOCATION_SIZE = 4 * 1024 * 1024; // 4 MiB

    private TcpClient? client;
    private NetworkStream? stream;
    private ConnectionOptions? options;
    private CancellationTokenSource cts = new();
    private readonly AppState state = AppState.Instance;
    private readonly ConcurrentQueue<object> requests = [];
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private bool reconnecting;
    public async Task<bool> Connect(ConnectionOptions connectionOptions)
    {
        try
        {
            client = new();
            options = connectionOptions;
            Task task = client.ConnectAsync(options.FilesHost, options.FilesPort);
            if (await Task.WhenAny(task, Task.Delay(5000)) != task) throw new TimeoutException("Connection to server timed out");
            stream = client.GetStream();
            _ = Task.Run(() => Listen(cts.Token));
            return true;
        }
        catch { return false; }
    }
    private async Task Listen(CancellationToken ct)
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
                                if (tcs is TaskCompletionSource<string> upload_tcs)
                                {
                                    string? hash = rootNode?["sha256_hash"]?.GetValue<string>();
                                    if (ok == true && hash != null) upload_tcs.SetResult(hash);
                                    else upload_tcs.SetException(new Exception(r_error ?? "Upload file failed"));
                                }
                                break;
                            case "download_file":
                                if (tcs is TaskCompletionSource<(string?, string?)> dload_tcs)
                                {
                                    JsonNode? preview_json = rootNode?["preview_metadata"];
                                    JsonNode? file_json = rootNode?["file_metadata"];
                                    string? preview_path = null;
                                    string? file_path = null;
                                    if (ok == true)
                                    {
                                        try
                                        {
                                            if (preview_json != null) preview_path = DownloadFromNode(preview_json);
                                            if (file_json != null) file_path = DownloadFromNode(file_json);
                                            dload_tcs.SetResult((preview_path, file_path));
                                        }
                                        catch (Exception ex) { dload_tcs.SetException(ex); }
                                    }
                                }
                                break;
                            case "download_metadata":
                                if (tcs is TaskCompletionSource<(string, long)> meta_tcs)
                                {
                                    JsonNode? filejson = rootNode?["file_metadata"];
                                    string? name = filejson?["file_name"]?.GetValue<string>();
                                    long? size = filejson?["file_size"]?.GetValue<long>();
                                    if (ok == true && name != null && size != null) meta_tcs.TrySetResult((name ?? "???", size ?? 0));
                                    else meta_tcs.SetException(new JsonException(r_error ?? "Download metadata failed"));
                                }
                                break;
                        }
                    }   
                }
            }
            catch { await Disconnect(); }
        }
    }
    /// <summary>
    /// Disconnects from server and resets client socket and requests
    /// </summary>
    public async Task Disconnect()
    {
        if (reconnecting) return;
        if (client?.Connected == true) client.Close();
        requests.Clear();
        cts.Cancel();
        cts = new();
        if (options == null || state.ReconnectionDelay < 0) return;
        reconnecting = true;
        while (reconnecting)
        {
            await Task.Delay(state.ReconnectionDelay);
            Debug.WriteLine("[FILES] reconnect attempt");
            if (await Connect(options))
            {
                Debug.WriteLine("[FILES] reconnect success");
                reconnecting = false;
                break;
            }
            Debug.WriteLine("[FILES] reconnect fail");
        }
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
        await SendJson(payload);
        await semaphore.WaitAsync();
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
        finally { semaphore.Release(); }
        TaskCompletionSource<string> tcs = new();
        requests.Enqueue(tcs);
        return await tcs.Task;
    }
    public async Task<(string? preview_path, string? file_path)> DownloadFile(string sha256Hash, DownloadMode mode)
    {
        var payload = new
        {
            method = "download_file",
            sha256_hash = sha256Hash,
            mode = mode.ToString()
        };
        TaskCompletionSource<(string?, string?)> tcs = new();
        await SendJson(payload);
        requests.Enqueue(tcs);
        return await tcs.Task;
    }
    public async Task<(string name, long size)> DownloadFileMetadata(string sha256Hash)
    {
        var payload = new
        {
            method = "download_metadata",
            sha256_hash = sha256Hash,
        };
        TaskCompletionSource<(string, long)> tcs = new();
        await SendJson(payload);
        requests.Enqueue(tcs);
        return await tcs.Task;
    }
    private async Task SendJson(object data)
    {
        await semaphore.WaitAsync();
        try
        {
            string request = JsonSerializer.Serialize(data);
            stream?.Write(TcpConnection.BuildJsonRequest(request));
        }
        catch { await Disconnect(); }
        finally { semaphore.Release(); }
    }
    private string DownloadFromNode(JsonNode node)
    {
        ulong expected_size = node?["file_size"]?.GetValue<ulong>() ?? throw new JsonException("Unable to deserialize file size");
        string temp_path = Path.Combine(PPPath.FileCacheFolder, Path.GetRandomFileName());
        FSManager.RestoreDirs(temp_path);
        byte[] buffer = new byte[CHUNK_SIZE];
        int bytesRead;
        ulong totalRead = 0;
        using FileStream fs = new(temp_path, FileMode.Append, FileAccess.Write);
        while (stream != null && totalRead < expected_size)
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) throw new EndOfStreamException("The connection was closed before the requested size was read.");
            fs.Write(buffer.AsSpan()[..bytesRead]);
            totalRead += (ulong)bytesRead;
        }
        return temp_path;
    }
}
