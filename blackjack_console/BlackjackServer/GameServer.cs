using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackServer;
public static class GameServer
{
    public static List<Table> Tables { get; set; } = [];
    private static List<ClientHandler> Clients { get; set; } = [];
    public static readonly Lock StateLock = new();

    public static void Main()
    {
        var loop = new Thread(ServerLoop)
        {
            IsBackground = true
        };
        
        loop.Start();
        
        var listener = new TcpListener(IPAddress.Any, 13000);
        listener.Start();
        Console.WriteLine("Server started on port 13000...");

        while (true)
        {
            var tcpClient = listener.AcceptTcpClient();
            Console.WriteLine($"New client connected from {tcpClient.Client.RemoteEndPoint}!");
            var handler = new ClientHandler(tcpClient);
            lock (StateLock) { Clients.Add(handler); }

            new Thread(handler.Run).Start();
        }
    }
    
    private static void ServerLoop()
    {
        while (true)
        {
            Thread.Sleep(100);

            lock (StateLock)
            {
                for (var i = 0; i < Tables.Count; i++)
                {
                    var table = Tables[i];
                    var previousState = table.State;
                    if (!table.Heartbeat()) continue;
                    
                    if (previousState != GameState.GameOver && table.State == GameState.GameOver)
                    {
                        foreach (var seat in table.Seats.Where(s => s.Player is { IsGuest: false }))
                        {
                            if(seat.Player is not null) UserManager.SaveUser(seat.Player);
                        }
                    }
                    BroadcastTableState(i);
                }
            }
        }
    }
    
    public static void BroadcastTableState(int tableIndex)
    {
        var table = Tables[tableIndex];
        JsonElement json;
        
        lock (StateLock)
        {
            json = JsonSerializer.SerializeToElement(table);
        }
        
        const string command = "TABLE_UPDATE";
        
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
        var tables = Tables
            .OrderByDescending(t => t.PlayerCount)
            .ToList();
        JsonElement json;
        
        lock (StateLock)
        {
            json = JsonSerializer.SerializeToElement(tables);
        }
        
        const string command = "LIST_TABLES";
        
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
            if(client.User is { IsGuest: false }) UserManager.SaveUser(client.User);
            if (client is not { CurrentTable: not null, User: not null }) return;
            
            var table = client.CurrentTable;
            var seat = table.GetSeatOf(client.User);
            seat?.StandUp();
            BroadcastTableState(table.Id);
        }
    }
    
}