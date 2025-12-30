using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackClient;
public class NetworkManager
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private bool _isConnected;

    public bool Connect(string ip)
    {
        try
        {
            _client = new TcpClient(ip, 13000);
            _stream = _client.GetStream();
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            _reader = new StreamReader(_stream);
            _isConnected = true;

            Thread t = new Thread(ListenLoop);
            t.IsBackground = true;
            t.Start();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Send(string command, object args)
    {
        if (!_isConnected || _writer == null) return;

        try
        {
            var envelope = new ClientRequest { Command = command, Args = args };
            var json = JsonSerializer.Serialize(envelope);
            _writer.WriteLine(json);
        }
        catch(Exception ex)
        {
            ConsoleRenderer.AddLog($"[Error] Failed to send data: {ex.Message}.");
            _isConnected = false;
        }
    }

    private void ListenLoop()
    {
        try
        {
            string? line;
            while (_isConnected && _reader != null && (line = _reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                CommandProcessor.HandleServerMessage(line);
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.AddLog($"[System] Disconnected from server {ex.Message}.");
        }
        finally
        {
            ConsoleRenderer.AddLog("[System] Disconnected from server.");
            _isConnected = false;
            _writer?.Dispose();
            _reader?.Dispose();
            _client?.Close();
            ConsoleRenderer.Draw();
        }
    }
}