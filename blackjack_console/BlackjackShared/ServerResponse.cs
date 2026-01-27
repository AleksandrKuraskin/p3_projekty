using System.Text.Json;

namespace BlackjackShared;

public class ServerResponse(string status = "OK", object? data = null)
{
    public string Status { get; init; } = status;
    public object? Data { get; init; } = data;

}