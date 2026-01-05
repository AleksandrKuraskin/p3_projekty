using System.Text.Json.Serialization;

namespace BlackjackShared;

public class Seat
{
    public int SeatNumber { get; set; }
    public Profile? Player { get; set; } = null;
    public Hand Hand { get; set; } = new Hand();
    
    public double CurrentBet { get; set; }

    public bool IsTaken => Player != null;

    [JsonConstructor]
    public Seat() {}

    public Seat(int number)
    {
        SeatNumber = number;
    }
    public void ResetHand()
    {
        Hand = new Hand();
    }

    public void AddBet(double amount)
    {
        CurrentBet += amount;
    }
    public void RemoveBet(double amount)
    {
        CurrentBet -= amount;
    }
    public void ResetBet()
    {
        CurrentBet = 0;
    }
    
    public void SitDown(Profile p)
    {
        Player = p;
        Hand = new Hand();
        CurrentBet = 0;
    }
    public void StandUp()
    {
        Player = null;
        ResetHand();
        CurrentBet = 0;
    }
}