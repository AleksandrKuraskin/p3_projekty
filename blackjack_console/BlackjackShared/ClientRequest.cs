using System.Text.Json;

namespace BlackjackShared;

public class ClientRequest
{
    public string Command { get; set; } = "";
    public object Args { get; set; } = new {};
}