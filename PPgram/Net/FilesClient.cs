using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.MVVM.Models.Message;
using PPgram.Net;
using PPgram.Shared;

internal class FilesClient
{
    private string host = string.Empty;
    private int port;
    private readonly TcpClient client = new();
    private NetworkStream? stream;

    // Controls suspension and resumption of the Listen method
    private const int chunkSize = 64 * 1024 * 1024; // 64 MiB
    const int MESSAGE_ALLOCATION_SIZE = 4 * 1024 * 1024;

    public void Connect(string remoteHost, int remotePort)
    {
        host = remoteHost;
        port = remotePort;
        try
        {
            if (!client.ConnectAsync(host, port).Wait(5000)) throw new Exception();
            stream = client.GetStream();
        }
        catch
        {
            // DIALOGFIX
            // notify files client connection failed
        }
    }

    public string? UploadFile(string filePath)
    {
        uint fileBytesSent = 0;

        var data = new
        {
            method = "upload_file",
            name = Path.GetFileName(filePath),
            is_media = false,
            compress = false
        };
        var metadata = JsonSerializer.Serialize(data);

        FileInfo fileInfo = new(filePath);
        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[chunkSize];
        int bytesRead;

        stream?.Write(RequestBuilder.BuildJsonRequest(metadata));

        byte[] lengthBytes = BitConverter.GetBytes(fileInfo.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        stream?.Write(lengthBytes);

        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            stream?.Write(buffer, 0, bytesRead);
            fileBytesSent += (uint)bytesRead;
        }

        // Read the response
        JsonMessageParser parser = new();
        while (!parser.IsReady())
        {
            parser.ReadStream(stream);
        }

        string response = parser.GetResponseAsString();
        HandleJsonResponse(response);

        JsonNode? rootNode = JsonNode.Parse(response);
        return rootNode?["sha256_hash"]?.AsValue().ToString();
    }

    private void ReadUntilFilled(byte[] buffer, int offset, long size)
    {
        if (stream == null) return;
        long totalBytesRead = 0;
        while (totalBytesRead < size)
        {
            int bytesRead = stream.Read(buffer, offset + (int)totalBytesRead, (int)(size - totalBytesRead));
            if (bytesRead == 0) throw new EndOfStreamException("The connection was closed before the requested size was read.");
            totalBytesRead += bytesRead;
        }
    }
    // TODO: Save files to the FS
    public void DownloadFiles(string sha256Hash)
    {
        var download_request = new
        {
            method = "download_file",
            sha256_hash = sha256Hash
        };
        var request = JsonSerializer.Serialize(download_request);
        stream?.Write(RequestBuilder.BuildJsonRequest(request));

        JsonMessageParser parser = new();
        while (!parser.IsReady()) parser.ReadStream(stream);

        DownloadFileResponseModel? metadatas = parser.GetResponseAsJson<DownloadFileResponseModel>();
        if (metadatas == null) { return; }

        string current_file_name = metadatas.Metadatas[0].FileName;
        long current_file_size = metadatas.Metadatas[0].FileSize;

        while (metadatas.Metadatas.Count != 0)
        {
            byte[] buffer = new byte[current_file_size];
            ReadUntilFilled(buffer, 0, current_file_size);

            // print recieved file size
            Debug.Print(buffer.Length.ToString());

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
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();

        if (ok == false && r_method != null && r_error != null)
        {
            string result = $"Error: {r_error}";
            /* DIALOGFIX
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Error occurred!",
                text = result,
                decline = ""
            });
            */
            return;
        }

        if (r_method == "upload_file")
        {
            string? sha256_hash = rootNode?["sha256_hash"]?.GetValue<string>();
            if (sha256_hash != null)
            {

            }
        }
    }
}