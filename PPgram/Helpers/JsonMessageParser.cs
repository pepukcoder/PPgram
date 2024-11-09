using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

internal class JsonMessageParser {
    private bool isFirst;
    private int expected_size;
    private List<byte> response_chunks;
    private bool is_ready;

    public JsonMessageParser() {
        isFirst = true;
        expected_size = 0;
        response_chunks = [];
        is_ready = false;
    }

    public bool IsReady() {
        return is_ready;
    }

    public T? GetResponseAsJson<T>() where T : class {
        if (!is_ready)
            throw new InvalidOperationException("Response not ready yet.");

        // Convert the accumulated bytes to a JSON string and deserialize it into the specified type
        byte[] responseData = [.. response_chunks];
        response_chunks.Clear();
        expected_size = 0;
        isFirst = true;
        is_ready = false;
        return JsonSerializer.Deserialize<T>(responseData);
    }

    public string GetResponseAsString() {
        if (!is_ready)
            throw new InvalidOperationException("Response not ready yet.");

        // Convert the accumulated bytes to a JSON string and deserialize it into the specified type
        byte[] responseData = [.. response_chunks];
        response_chunks.Clear();
        expected_size = 0;
        isFirst = true;
        is_ready = false;
        return Encoding.UTF8.GetString(responseData);
    }

    public void ReadStream(NetworkStream? stream) {
        if (stream == null) {return;}
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
}