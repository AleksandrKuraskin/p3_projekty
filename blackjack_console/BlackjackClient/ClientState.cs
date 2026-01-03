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
    
    public static List<Table> LobbyList { get; set; } = [];
    public static ViewState View { get; set; } = ViewState.Welcome;

    public static DateTime TurnExpiresAt { get; set; }
    public static bool IsHandInProgress { get; set; }

    public static NetworkManager Network { get; } = new NetworkManager();

    public static void Init()
    {
        ConsoleRenderer.AddLog("[System] Connecting to server...");
        
        ConsoleRenderer.AddLog(Network.Connect("127.0.0.1") ?
            "[System] Connected successfully!" :
            "[Error] Could not connect to server. Is it running?");
        ConsoleRenderer.Draw();
    }

    public static void Logout()
    {
        Network.Send("LOGOUT", new {});
        UserProfile = null;
        CurrentTable = null;
        View = ViewState.Welcome;
    }
}