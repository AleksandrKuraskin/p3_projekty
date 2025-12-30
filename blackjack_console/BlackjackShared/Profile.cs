using System.Text.Json.Serialization;

namespace BlackjackShared;

public class Profile
{
    public string Name { get; set; }
    public double Balance { get; set; }
    public int Xp { get; set; }
    public int Level => (Xp / 1000) + 1;
    
    [JsonConstructor]
    public Profile() { }

    public Profile(string name, double startingBalance)
    {
        Name = name;
        Balance = startingBalance;
        Xp = 0;
    }
}