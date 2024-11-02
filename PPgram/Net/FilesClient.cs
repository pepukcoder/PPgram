using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;

internal class FilesClient
{
    private string host = string.Empty;
    private int port;
    private readonly TcpClient client = new();
    private NetworkStream? stream;
    
    // If needed Skip next message(if we know that next message is e.g. file itself)
    private bool skipNext = false;
    
    public void Connect(string remoteHost, int remotePort)
    {
        host = remoteHost;
        port = remotePort;
        try
        {
            // try to connect the socket
            if (!client.ConnectAsync(host, port).Wait(5000)) throw new Exception();
            stream = client.GetStream();
            // make a background thread for connection handling
            Thread listenThread = new(new ThreadStart(Listen))
            { IsBackground = true };
            listenThread.Start();
        }
        catch { Disconnected(); }
    }
    private void Listen()
    {
        if (stream == null || skipNext) return;
        // init response chunks
        List<byte> response_chunks = [];
        int expected_size = 0;
        bool isFirst = true;
        while (true)
        {
            try
            {
                int read_count;
                // get server response length if chunk is first
                if (isFirst)
                {
                    byte[] length_bytes = new byte[4];
                    read_count = stream.Read(length_bytes, 0, 4);
                    if (read_count == 0) break;
                    if (BitConverter.IsLittleEndian) Array.Reverse(length_bytes);
                    int length = BitConverter.ToInt32(length_bytes);
                    // set response size
                    expected_size = length;
                    isFirst = false;
                }
                // get server response chunk
                byte[] responseBytes = new byte[expected_size];
                read_count = stream.Read(responseBytes, 0, expected_size);
                // cut chunk by actual read count and to list 
                ArraySegment<byte> segment = new(responseBytes, 0, read_count);
                responseBytes = [.. segment];
                response_chunks.AddRange(responseBytes);
                // check if whole response was recieved
                if (response_chunks.Count >= expected_size)
                {
                    // get whole response if size equals expected
                    string response = Encoding.UTF8.GetString(response_chunks.ToArray());
                    // reset size and chunks
                    response_chunks.Clear();
                    expected_size = 0;
                    isFirst = true;
                    // handle response
                    HandleJsonResponse(response);
                }
            }
            catch { Disconnected(); }
        }
    }

    private void Disconnected()
    {
        // show disconnected dialog
        WeakReferenceMessenger.Default.Send(new Msg_ShowDialog
        {
            icon = DialogIcons.Error,
            header = "Connection error",
            text = "Unable to connect to the server",
            accept = "Retry",
            decline = ""
        });
        // listen for retry action
        WeakReferenceMessenger.Default.Register<Msg_DialogResult>(this, (r, e) =>
        {
            WeakReferenceMessenger.Default.Unregister<Msg_DialogResult>(this);
            if (e.action == DialogAction.Accepted) Connect(host, port);
        });
    }

    private void HandleJsonResponse(string response)
    {
        JsonNode? rootNode = JsonNode.Parse(response);
        // parse common fields
        string? r_method = rootNode?["method"]?.GetValue<string>();
        string? r_event = rootNode?["event"]?.GetValue<string>();
        string? r_error = rootNode?["error"]?.GetValue<string>();
        bool? ok = rootNode?["ok"]?.GetValue<bool>();  

        // LATER: remove debug errors dialogs and insted proccess them in mainview

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
            case "upload_media":
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