using System.Text.Json;

namespace BlackjackShared;

public class ServerResponse
{
    public string Status { get; set; }
    public object? Data { get; set; }
    
    public ServerResponse(string status = "OK", object? data = null)
    {
        Status = status;
        Data = data;
    }
}