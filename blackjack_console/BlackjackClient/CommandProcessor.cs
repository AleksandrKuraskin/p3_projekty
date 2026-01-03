using System.Diagnostics;
using System.Text.Json;
using BlackjackShared;

namespace BlackjackClient;

public static class CommandProcessor
{
    public static void Process(string input, ref bool running)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = args[0].ToLower();

        if (cmd is "exit" or "quit")
        {
            running = false;
            return; 
        }

        switch (ClientState.View)
        {
            case ViewState.Welcome:
                HandleWelcome(cmd, args);
                break;
            case ViewState.Lobby:
                HandleLobby(cmd, args);
                break;
            case ViewState.Table:
                HandleTable(cmd, args);
                break;
            default: break;
        }
    }

    private static void HandleWelcome(string cmd, string[] args)
    {
        switch(cmd)
        {
            case "guest":
                ClientState.Network.Send("GUEST", new { });
                break;
            case "login":
                if(args.Length != 3) break;
                ClientState.Network.Send("LOGIN", new { username = args[1], password = args[2] });
                break;
            case "register":
                if(args.Length != 3) break;
                ClientState.Network.Send("REGISTER", new { username = args[1], password = args[2] });
                break;
            default:
                ConsoleRenderer.AddLog("Commands: guest, login <username> <password>, register <username> <password>, exit");
                break;
        }
    }

    private static void HandleLobby(string cmd, string[] args)
    {
        switch (cmd)
        {
            case "logout":
                ClientState.Logout();
                break;
            case "list":
                ClientState.Network.Send("LIST", new {});
                break;
            case "create":
                if (args.Length != 3) break;
                ClientState.Network.Send("CREATE", new
                {
                    name = args[1],
                    maxPlayers = int.TryParse(args[2], out var max) ? max : 4
                });
                break;
            case "join":
                if (args.Length != 2) break;
                if (int.TryParse(args[1], out var arg))
                {
                    ConsoleRenderer.AddLog($"[Info] Joining table with id {arg}...");
                    ClientState.Network.Send("JOIN", new { id = arg });
                }
                else
                {
                    ConsoleRenderer.AddLog($"[Info] Joining table with name {args[1]}...");
                    ClientState.Network.Send("JOIN", new { name = args[1] });
                }
                break;
            default:
                ConsoleRenderer.AddLog("Commands: list, join <id/name>, create <name> <max_players>, logout, exit");
                break;
        }
    }

    private static void HandleTable(string cmd, string[] args)
    {
        switch (cmd)
        {
            case "leave":
                ClientState.Network.Send("LEAVE", new {});
                break;
            case "back":
                ClientState.Network.Send("BACK", new {});
                break;
            case "sit":
                if (args.Length != 2) break;
                if (int.TryParse(args[1], out var seat)) ClientState.Network.Send("SIT", new { seatId = seat - 1 });
                break;
            case "bet":
                if (args.Length != 2) break;
                if (int.TryParse(args[1], out var a)) ClientState.Network.Send("BET", new { amount = a });
                break;
            case "hit":
                ClientState.Network.Send("HIT", new {});
                break;
            case "stand":
                ClientState.Network.Send("STAND", new {});
                break;
            default:
                ConsoleRenderer.AddLog("Commands: leave, back, sit <seat>, bet <amount>, hit, stand, exit");
                break;
        }
    }
    
    public static void HandleServerMessage(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<ServerResponse>(json);
            if (msg is not {Data: JsonElement root}) return;

            switch (msg.Status)
            {
                case "LOGIN_OK":
                    ClientState.UserProfile = JsonSerializer.Deserialize<Profile>(root.GetRawText());
                    ClientState.View = ViewState.Lobby;
                    ConsoleRenderer.AddLog("[System] Login Successful.");
                    ClientState.Network.Send("LIST", new {});
                    break;

                case "INFO":
                    ConsoleRenderer.AddLog($"[Info] {root.GetProperty("message").GetString()}");
                    break;

                case "LIST_TABLES":
                    var tables = JsonSerializer.Deserialize<List<Table>>(root.GetRawText());
                    ClientState.LobbyList = tables ?? [];
                    ConsoleRenderer.Draw();
                    break;

                case "TABLE_UPDATE":
                    ClientState.CurrentTable = JsonSerializer.Deserialize<Table>(root.GetRawText());
                    if (ClientState.CurrentTable == null) return;
                    if (ClientState.UserProfile == null) return;

                    var seat = ClientState.CurrentTable.GetSeatOf(ClientState.UserProfile);
                    if (seat != null)
                    {
                        var player = seat.Player;
                        ClientState.UserProfile = player;
                        if (ClientState.CurrentTable.TimerEnd.HasValue)
                        {
                            ClientState.TurnExpiresAt = ClientState.CurrentTable.TimerEnd.Value;
                        }

                        var isMyTurn = (ClientState.CurrentTable.CurrentTurnSeatIndex == seat.SeatNumber - 1);
                        ClientState.IsHandInProgress = (isMyTurn && ClientState.CurrentTable.State == GameState.Playing);

                        if (ClientState.IsHandInProgress)
                        {
                            ConsoleRenderer.AddLog($"[Info] It's your turn. Type 'hit' or 'stand'.");
                        }
                        
                    }
                    
                    if (ClientState.View != ViewState.Table) 
                    {
                        ClientState.View = ViewState.Table;
                        ConsoleRenderer.AddLog($"[Info] You have joined table '{ClientState.CurrentTable.Name}'. Type 'leave' to leave.");
                    }
                    
                    ConsoleRenderer.Draw();
                    break;
                    
                case "LEAVE_OK":
                    ClientState.CurrentTable = null;
                    ClientState.View = ViewState.Lobby;
                    ClientState.Network.Send("LIST", new {});
                    ConsoleRenderer.Draw();
                    break;
                
                case "ERROR":
                    ConsoleRenderer.AddLog($"[Error] {root.GetProperty("message").GetString()}");
                    break;
            }
        }
        catch (Exception ex)
        {
            var st = new StackTrace(ex, true);
            var frame = st.GetFrame(st.FrameCount-1);
            var line = frame?.GetFileLineNumber();
            ConsoleRenderer.AddLog($"[NetError] {ex.Message} ({line})");
        }
        finally
        {
            ConsoleRenderer.Draw();
        }
    }
}