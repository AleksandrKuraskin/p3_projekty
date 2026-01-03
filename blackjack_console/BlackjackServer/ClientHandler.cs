using System.Net.Sockets;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackServer;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public Profile? User { get; private set; }
    public Table? CurrentTable { get; private set;}

    public ClientHandler(TcpClient client)
    {
        _client = client;
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public void Run()
    {
        try
        {
            while (_reader.ReadLine() is {} line)
            {
                ProcessJsonCommand(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[System] Client error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"[System] Client disconnected.");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        try
        {
            lock (GameServer.StateLock)
            {
                GameServer.RemoveClient(this);
            }
        }
        catch
        {
            Console.WriteLine($"[System] Error disconnecting client {_client.Client.RemoteEndPoint}.");
        }
        finally
        {
            _reader.Dispose();
            _writer.Dispose();
            _client.Close();
        }
    }
    
    private void ProcessJsonCommand(string jsonLine)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ClientRequest>(jsonLine);
            if (message == null) return;
            var root = (JsonElement)message.Args;
            
            lock (GameServer.StateLock)
            {
                switch (message.Command)
                {
                    case "GUEST":
                        HandleGuest();
                        break;
                    case "LOGIN":
                        var user = root.GetProperty("username").GetString();
                        var pass = root.GetProperty("password").GetString();
                        if (user == null || pass == null) return;
                        HandleLogin(user, pass);
                        break;
                    case "REGISTER":
                        var regUser = root.GetProperty("username").GetString();
                        var regPass = root.GetProperty("password").GetString();
                        if (regUser == null || regPass == null)
                        {
                            SendError("Username or password was not provided.");
                            break;
                        }
                        HandleRegister(regUser, regPass);
                        break;

                    case "LOGOUT":
                        HandleLogout();
                        break;
                    
                    case "LIST": HandleList(); break;
                    case "CREATE":
                        var tableName = root.GetProperty("name").GetString();
                        var maxPlayers = root.GetProperty("maxPlayers").GetInt32();
                        if (tableName == null) return;
                        HandleCreate(tableName, maxPlayers);
                        break;

                    case "JOIN":
                        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
                        HandleJoin(id, name);
                        break;

                    case "SIT":
                        var seatId = root.GetProperty("seatId").GetInt32();
                        HandleSit(seatId);
                        break;

                    case "BET":
                        var amount = root.GetProperty("amount").GetInt32();
                        HandleBet(amount);
                        break;

                    case "HIT":
                        HandleHit();
                        break;

                    case "STAND":
                        HandleStand();
                        break;

                    case "BACK":
                        HandleLeaveSeat();
                        break;
                    
                    case "LEAVE": 
                        HandleLeaveTable(); 
                        break;

                    default:
                        SendError($"Unknown Command: {message.Command}");
                        break;
                }
            }
        }
        catch (KeyNotFoundException)
        {
            SendError("Invalid Payload: Missing required fields.");
        }
        catch (JsonException)
        {
            SendError("Invalid JSON format.");
        }
        catch (Exception ex)
        {
            SendError($"Server Error: {ex.Message}");
        }
    }

    private void HandleGuest()
    {
        var name = "Guest_" + new Random().Next(100, 999);
        User = new Profile(true, name, 500);
        Send(CreateResponse("LOGIN_OK", JsonSerializer.SerializeToElement(User)));
    }
    private void HandleLogin(string u, string p)
    {
        if (string.IsNullOrWhiteSpace(u)) { SendError("Invalid username."); return; }
        
        var profile = UserManager.Login(u, p); 
        if (profile != null)
        {
            User = profile;
            Send(CreateResponse("LOGIN_OK", JsonSerializer.SerializeToElement(profile)));
            Console.WriteLine($"[Auth] {User.Name} logged in.");
        }
        else SendError("Invalid credentials.");
        
        
    }

    private void HandleRegister(string u, string p)
    {
        if (UserManager.Register(u, p))
        {
            SendInfo("Registration successful. You can now login.");
        }
        else 
        {
            SendError("Username already taken.");
        }
    }

    private void HandleLogout()
    {
        if (User != null)
        {
            UserManager.SaveUser(User);
            if (CurrentTable != null)
            {
                var t = CurrentTable;
                t.StandUp(User);
                GameServer.BroadcastTableState(CurrentTable.Id);
            }
            Console.WriteLine($"[Auth] {User.Name} logged out.");
            User = null;
            CurrentTable = null;
            SendInfo("Logged out successfully.");
        }
    }

    private void HandleList()
    {
        var tables = GameServer.Tables
            .OrderByDescending(t => t.PlayerCount)
            .ToList();
        Send(CreateResponse("LIST_TABLES", JsonSerializer.SerializeToElement(tables)));
    }

    private void HandleCreate(string name, int maxPlayers)
    {
        if (string.IsNullOrWhiteSpace(name)) { SendError("Name required."); return; }
        if (GameServer.Tables.Any(t => t.Name == name)) { SendError("Table name already taken."); return; }
        
        GameServer.Tables.Add(new Table(GameServer.Tables.Count, name, maxPlayers));
        SendInfo($"Table '{name}' created.");
        GameServer.BroadcastTablesList();
    }

    private void HandleJoin(int tableId, string? name = null)
    {
        var tId = tableId == -1 ? GameServer.Tables.FindIndex(t => t.Name == name) : tableId;
        
        if (tId < 0 || tId >= GameServer.Tables.Count)
        {
            SendError("Table does not exist.");
            return;
        }
        
        CurrentTable = GameServer.Tables[tId];
        GameServer.BroadcastTableState(tId);
        SendInfo($"Joined table {CurrentTable.Name}.");
    }

    private void HandleSit(int seatIndex)
    {
        
        if (User == null) { SendError("Please login first."); return; }
        if (CurrentTable == null) { SendError("You must join a table first."); return; }
        
        var t = CurrentTable;
        var result = t.Sit(User, seatIndex);
        if (result == "OK")
        {
            GameServer.BroadcastTableState(t.Id);
            GameServer.BroadcastTablesList();
        }
        else SendError(result);
    }

    private void HandleBet(int amount)
    {
        if (User == null) { SendError("Please login first."); return; }
        if (CurrentTable == null) { SendError("You must join a table first."); return; }
        
        var t = CurrentTable;
        var result = t.PlaceBet(User, amount);
        if (result == "OK") GameServer.BroadcastTableState(t.Id);
        else SendError(result);
    }

    private void HandleHit()
    {
        if (User == null) { SendError("Please login first."); return; }
        if (CurrentTable == null) { SendError("You must join a table first."); return; }
        
        var t = CurrentTable;
        var result = t.Hit(User);
        if (result == "OK")
        {
            GameServer.BroadcastTableState(CurrentTable.Id);
            CheckTriggerDealer(CurrentTable);
        }
        else SendError(result);

    }

    private void HandleStand()
    {
        if (User == null) { SendError("Please login first."); return; }
        if (CurrentTable == null) { SendError("You must join a table first."); return; }
        
        var t = CurrentTable;
        var result = t.Stand(User);
        if (result == "OK")
        {
            GameServer.BroadcastTableState(CurrentTable.Id);
            CheckTriggerDealer(CurrentTable);
        }
        else SendError(result);
    }
    
    private static void CheckTriggerDealer(Table t)
    {
        if (t.State == GameState.DealerTurn)
        {
            Task.Run(async () => 
            {
                var dealerActive = true;
                while (dealerActive)
                {
                    await Task.Delay(1000);

                    lock (GameServer.StateLock)
                    {
                        if (t.State != GameState.DealerTurn) break;
                        
                        dealerActive = t.ExecuteDealerStep();

                        GameServer.BroadcastTableState(t.Id);
                    }
                }
            });
        }
    }

    private void HandleLeaveSeat()
    {
        if (User == null) { SendError("Please login first."); return; }
        if (CurrentTable == null) { SendError("You must join a table first."); return; }
        
        var t = CurrentTable;
        var result = t.StandUp(User);
        if (result == "OK")
        {
            GameServer.BroadcastTableState(CurrentTable.Id);
            CheckTriggerDealer(CurrentTable);
            GameServer.BroadcastTablesList();
        }
        else SendError(result);
    }
    
    private void HandleLeaveTable()
    {
        if (CurrentTable != null && User != null)
        {
            var t = CurrentTable;

            t.StandUp(User); 
            GameServer.BroadcastTableState(CurrentTable.Id);
            GameServer.BroadcastTablesList();
        }

        CurrentTable = null;
        Send(CreateResponse("LEAVE_OK", JsonSerializer.SerializeToElement(new {})));
    }

    public static ServerResponse CreateResponse(string status, object data)
    {
        return new ServerResponse { Status = status, Data = data };
    }
    public void Send(ServerResponse response)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            Console.WriteLine($"[Server] Sending: {json}");
            _writer.WriteLine(json);
        }
        catch
        {
            Console.WriteLine($"[System] Error sending message to client {_client.Client.RemoteEndPoint}.");
        }
    }

    private void SendError(string msg)
    {
        Send(
            CreateResponse("ERROR", JsonSerializer.SerializeToElement(new { message = msg }))
        );
    }
    private void SendInfo(string msg)
    {
        Send(
            CreateResponse("INFO", JsonSerializer.SerializeToElement(new { message = msg }))
        );
    }
}