using CommunityToolkit.Mvvm.Messaging;
using PPgram.Net;
using PPgram.Shared;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

internal class FilesClient : BaseClient
{
    // Controls suspension and resumption of the Listen method
    private readonly ManualResetEventSlim listenEvent = new(true);
    private const int chunkSize = 64 * 1024 * 1024; // 64 MiB

    // Needed to suspend listening -> when retrieving the file binary, we don't want to handle message as Json
    public void SuspendListening() => listenEvent.Reset();
    public void ResumeListening() => listenEvent.Set();

    /// <summary>
    /// Uploads file by opening a 64 MiB window on the file, and then sending chunks to the stream.
    /// </summary>
    /// <param name="filePath">The path of the file to upload.</param>
    public void UploadFile(string filePath)
    {
        uint fileBytesSent = 0;

        var data = new {
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

        // Write metadata
        stream?.Write(BuildJsonRequest(metadata));
        
        // Write Binary Size
        byte[] lengthBytes = BitConverter.GetBytes(fileInfo.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        stream?.Write(lengthBytes);

        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            // Send the binary itself
            stream?.Write(buffer, 0, bytesRead);
            fileBytesSent += (uint)bytesRead;
        }
    }
    protected override void HandleResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();  

        if (ok == false && r_method != null && r_error != null) {
            string method = r_method;
            string error = r_error;

            string result;
            #if DEBUG
                result = $"[DEBUG] Error in method: {method}\n Error:{error}";
            #else
                result = $"Error: {error}";
            #endif
            WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
            {
                icon = DialogIcons.Error,
                header = "Error occurred!",
                text = result,
                decline = ""
            });
            return;
        }

        // parse specific fields
        switch (r_method)
        {
            case "upload_file":
                string? sha256_hash = rootNode?["sha256_hash"]?.GetValue<string>();
                if (sha256_hash != null) {
                    string hash = sha256_hash;
                    WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
                    {
                        icon = DialogIcons.Info,
                        header = "Hash Result!",
                        text = hash,
                        decline = ""
                    });
                }
                break;
        }
    }
}