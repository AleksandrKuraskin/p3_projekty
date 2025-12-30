using System;
using System.Collections.Generic;
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

        if (cmd == "exit" || cmd == "quit")
        {
            running = false;
            return; 
        }
        
        try 
        {
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
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.AddLog($"[Error] {ex.Message}");
        }
    }

    private static void HandleWelcome(string cmd, string[] args)
    {
        if (cmd == "guest")
        {
            ClientState.Network.Send("GUEST", new { });
        }
        else if (cmd == "login" && args.Length == 3)
        {
            ClientState.Network.Send("LOGIN", new { username = args[1], password = args[2] });
        }
        else if (cmd == "register" && args.Length == 3)
        {
            ClientState.Network.Send("REGISTER", new { username = args[1], password = args[2] });
        }
        else
        {
            ConsoleRenderer.AddLog("Commands: login <username> <password>, register <username> <password>, guest, exit");
        }
    }

    private static void HandleLobby(string cmd, string[] args)
    {
        if (cmd == "logout") { ClientState.Logout(); return; }

        if (cmd == "list") { ClientState.Network.Send("LIST", new {}); }
        else if (cmd == "create" && args.Length == 3)
        {
            ClientState.Network.Send("CREATE", new { name = args[1], maxPlayers = int.TryParse(args[2], out var max) ? max : 4  });
        }
        else if (cmd == "join" && args.Length == 2)
        {
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
        }
        else
        {
            ConsoleRenderer.AddLog("Commands: list, join <id/name>, create <name> <max_players>, logout, exit");
        }
    }

    private static void HandleTable(string cmd, string[] args)
    {
        if (cmd == "leave") { ClientState.Network.Send("LEAVE", new {}); }
        else if (cmd == "back") { ClientState.Network.Send("BACK", new {}); }
        else if (cmd == "sit" && args.Length == 2) 
        {
            if (int.TryParse(args[1], out var seat)) ClientState.Network.Send("SIT", new { seatId = seat - 1 });
        }
        else if (cmd == "bet" && args.Length == 2)
        {
            if (int.TryParse(args[1], out var a)) ClientState.Network.Send("BET", new { amount = a });
        }
        else if (cmd == "hit") { ClientState.Network.Send("HIT", new {}); }
        else if (cmd == "stand") { ClientState.Network.Send("STAND", new {}); }
        else
        {
            ConsoleRenderer.AddLog("Commands: list, join <id/name>, create <name> <max_players>, logout, exit");
        }
    }
    
    public static void HandleServerMessage(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<ServerResponse>(json);
            if (msg == null) return;
            var root = (JsonElement)msg.Data;

            switch (msg.Status)
            {
                case "LOGIN_OK":
                    ClientState.UserProfile = new Profile(
                        root.GetProperty("name").GetString(), 
                        root.GetProperty("balance").GetDouble()
                    ) { Xp = root.GetProperty("xp").GetInt32() };
                    ClientState.View = ViewState.Lobby;
                    ConsoleRenderer.AddLog("[System] Login Successful.");
                    ClientState.Network.Send("LIST", new {});
                    break;

                case "INFO":
                    ConsoleRenderer.AddLog($"[Info] {root.GetProperty("message").GetString()}");
                    break;

                case "LIST_TABLES":
                    ClientState.LobbyList = JsonSerializer.Deserialize<List<TableInfo>>(root.GetRawText());
                    ConsoleRenderer.Draw();
                    break;

                case "TABLE_UPDATE":
                    ClientState.CurrentTable = JsonSerializer.Deserialize<Table>(root.GetRawText());
                    if (ClientState.CurrentTable == null) return;
                    if (ClientState.UserProfile != null)
                    {
                        var seat = ClientState.CurrentTable.GetSeatOf(ClientState.UserProfile);
                        var player = seat?.Player;
                        if (player != null)
                        {
                            ClientState.UserProfile = player;
                        }

                        if (ClientState.CurrentTable.CurrentTurnSeatIndex == seat?.SeatNumber - 1)
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
            var line = frame.GetFileLineNumber();
            ConsoleRenderer.AddLog($"[NetError] {ex.Message} ({line})");
        }
        finally
        {
            ConsoleRenderer.Draw();
        }
    }
}