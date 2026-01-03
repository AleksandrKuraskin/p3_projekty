using System.Text.Json;

namespace BlackjackShared;

public class ClientRequest
{
    public string Command { get; init; } = "";
    public object Args { get; init; } = new {};
}