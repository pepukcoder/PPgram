using CommunityToolkit.Mvvm.Messaging;
using PPgram.Shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PPgram.Net;

internal abstract class BaseClient
{
    protected string host = string.Empty;
    protected int port;
    protected readonly TcpClient client = new();
    protected NetworkStream? stream;
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
    protected void Listen()
    {
        if (stream == null) return;
        // init reponse chunks
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
                    HandleResponse(response);
                }
            }
            catch { Disconnected(); }
        }
    }

    protected void Disconnected()
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
    protected static byte[] BuildJsonRequest(string message)
    {
        // Get request bytes from text
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        // Get size of request bytes
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        // Combine size and request into one array
        byte[] request_bytes = new byte[4 + messageBytes.Length];
        Array.Copy(lengthBytes, 0, request_bytes, 0, 4);
        Array.Copy(messageBytes, 0, request_bytes, 4, messageBytes.Length);

        return request_bytes;
    }
    protected void Stop()
    {
        stream?.Dispose();
        client.Close();
        Disconnected();
    }
    protected abstract void HandleResponse(string response);
}
