using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackServer;
public static class GameServer
{
    public static List<Table> Tables { get; set; } = new List<Table>();
    public static List<ClientHandler> Clients { get; set; } = new List<ClientHandler>();
    public static readonly System.Threading.Lock StateLock = new();

    public static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 13000);
        listener.Start();
        Console.WriteLine("Server started on port 13000...");

        while (true)
        {
            TcpClient tcpClient = listener.AcceptTcpClient();
            Console.WriteLine($"New client connected from {tcpClient.Client.RemoteEndPoint}!");
            ClientHandler handler = new ClientHandler(tcpClient);
            lock (StateLock) { Clients.Add(handler); }

            new Thread(handler.Run).Start();
        }
    }
    
    public static void BroadcastTableState(int tableIndex)
    {
        var table = Tables[tableIndex];
        JsonElement json;
        
        lock (StateLock)
        {
            json = System.Text.Json.JsonSerializer.SerializeToElement(table);
        }
        
        var command = "TABLE_UPDATE";
        
        List<ClientHandler> targets;
        lock (StateLock)
        {
            targets = Clients.
            Where(c => c.CurrentTable != null && c.CurrentTable.Id == tableIndex)
            .ToList();
            
        }

        foreach (var client in targets)
        {
            client.Send
            (
                ClientHandler.CreateResponse(command, json)
            );
        }
    }
    
    public static void BroadcastTablesList()
    {
        var tables = GameServer.Tables
            .Select(t => new TableInfo
            {
                Id = t.Id,
                Name = t.Name,
                MaxPlayers = t.MaxPlayers,
                PlayerCount = t.PlayerCount
            })
            .OrderByDescending(t => t.PlayerCount)
            .ToList();
        JsonElement json;
        
        lock (StateLock)
        {
            json = System.Text.Json.JsonSerializer.SerializeToElement(tables);
        }
        
        var command = "LIST_TABLES";
        
        List<ClientHandler> targets;
        lock (StateLock)
        {
            targets = Clients.
                Where(c => c.CurrentTable == null && c.User != null)
                .ToList();
            
        }

        foreach (var client in targets)
        {
            client.Send
            (
                ClientHandler.CreateResponse(command, json)
            );
        }
    }
    
    public static void RemoveClient(ClientHandler client)
    {
        lock (StateLock)
        {
            Clients.Remove(client);
            if (client.CurrentTable != null && client.User != null)
            {
                var table = client.CurrentTable;
                var seat = table.GetSeatOf(client.User);
                if (seat != null) seat.StandUp();
            }
        }
    }
    
}