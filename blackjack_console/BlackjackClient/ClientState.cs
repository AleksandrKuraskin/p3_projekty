using BlackjackShared;

namespace BlackjackClient;
public enum ViewState
{
    Welcome,
    Lobby,
    Table,
}

public static class ClientState
{
    public static Profile? UserProfile { get; set; }
    public static Table? CurrentTable { get; set; }
    
    public static List<TableInfo> LobbyList { get; set; } = new List<TableInfo>();
    public static ViewState View { get; set; } = ViewState.Welcome;

    public static DateTime TurnExpiresAt { get; set; }
    public static bool IsHandInProgress { get; set; } = false;
    
    public static NetworkManager? Network { get; set; }

    public static void Init()
    {
        Network = new NetworkManager();
        ConsoleRenderer.AddLog("[System] Connecting to server...");
        
        if (Network.Connect("127.0.0.1"))
        {
            ConsoleRenderer.AddLog("[System] Connected to server.");
        }
        else
        {
            ConsoleRenderer.AddLog("[Error] Could not connect to server. Is it running?");
        }
        ConsoleRenderer.Draw();
    }

    public static void Logout()
    {
        if (Network == null) return;
        Network.Send("LOGOUT", new {});
        UserProfile = null;
        CurrentTable = null;
        View = ViewState.Welcome;
        View = ViewState.Welcome;
    }
}