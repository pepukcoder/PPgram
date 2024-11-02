using System;
using System.Text;
using System.Text.Json;
using PPgram.MVVM.Models.Message;

namespace PPgram.Net;

class RequestBuilder
{
    public static byte[] BuildJsonRequest(string message)
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
}