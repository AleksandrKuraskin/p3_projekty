using System.Text.Json.Serialization;

namespace BlackjackShared;

public class Profile
{
    public bool IsGuest { get; init; } = true;
    public string Name { get; init; } = "";
    public double Balance { get; set; }
    public int GlobalXp { get; set; }
    public int Xp => GlobalXp % 1000;
    
    [JsonIgnore] public int Level => (GlobalXp / 1000) + 1;

    [JsonConstructor]
    public Profile() { }

    public Profile(bool isGuest, string name, double startingBalance)
    {
        IsGuest = isGuest;
        Name = name;
        Balance = startingBalance;
    }
}