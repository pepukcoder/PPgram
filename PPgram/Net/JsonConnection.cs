using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PPgram.Net;

internal class JsonConnection {
    private bool isFirst = true;
    private int expected_size = 0;
    private readonly List<byte> response_chunks = [];
    private bool is_ready = false;
    public bool IsReady { get => is_ready; }

    public T? GetResponseAsJson<T>() where T : class {
        if (!is_ready)
            throw new InvalidOperationException("Response not ready yet.");

        // Convert the accumulated bytes to a JSON string and deserialize it into the specified type
        byte[] responseData = [.. response_chunks];
        Reset();
        return JsonSerializer.Deserialize<T>(responseData);
    }

    public string GetResponseAsString() {
        if (!is_ready)
            throw new InvalidOperationException("Response not ready yet.");

        // Convert the accumulated bytes to a JSON string and deserialize it into the specified type
        byte[] responseData = [.. response_chunks];
        Reset();
        return Encoding.UTF8.GetString(responseData);
    }

    public void ReadStream(NetworkStream? stream) {
        if (stream == null) { return; }
        int read_count;
        // get server response length if chunk is first
        if (isFirst)
        {
            byte[] length_bytes = new byte[4];
            read_count = stream.Read(length_bytes, 0, 4);
            if (read_count == 0) return;
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

        // check if whole response was received
        if (response_chunks.Count >= expected_size)
        {
            is_ready = true;
        }
    }
    private void Reset()
    {
        response_chunks.Clear();
        expected_size = 0;
        isFirst = true;
        is_ready = false;
    }
    public static byte[] BuildJsonRequest(string message)
    {
        // Get request bytes from text
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        // Get size of request bytes
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        // Combine size and request into one array
        byte[] request_bytes = new byte[4 + messageBytes.Length];
        Array.Copy(lengthBytes, 0, request_bytes, 0, 4);
        Array.Copy(messageBytes, 0, request_bytes, 4, messageBytes.Length);
        return request_bytes;
    }
}
internal class ConnectionOptions
{
    [JsonPropertyName("host")]
    public required string Host { get; set; }
    [JsonPropertyName("port_json")]
    public required int JsonPort { get; set; }
    [JsonPropertyName("port_files")]
    public required int FilesPort { get; set; }
}
