using System;
using System.Diagnostics;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

internal class QuicClient
{
    private const string ServerAddress = "0.0.0.0";
    private const int ServerPort = 8000;
    private QuicConnection? _connection;

    public async Task ConnectAsync()
    {
        try
        {
            // Define QUIC client connection options
            var endpoint = new IPEndPoint(IPAddress.Parse(ServerAddress), ServerPort);
            var options = new QuicClientConnectionOptions
            {
                RemoteEndPoint = endpoint,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = ServerAddress,
                    RemoteCertificateValidationCallback = ValidateServerCertificate
                }
            };

            // Establish the connection
            _connection = await QuicConnection.ConnectAsync(options);
            Console.WriteLine("Connected to the server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect: {ex.Message}");
        }
    }

    // Validate the server's TLS certificate
    private bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true; // Accept valid certificate
        }

        // Customize this section to accept invalid certificates (not recommended for production)
        Console.WriteLine("Warning: Accepting all certificates!");
        return true;
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync(0);
            Console.WriteLine("Disconnected from the server.");
        }
    }
}
