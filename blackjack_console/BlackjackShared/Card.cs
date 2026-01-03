using System.Text.Json.Serialization;

namespace BlackjackShared;

public enum Rank
{
    Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack, Queen, King, Ace
}

public enum Suit
{
    Hearts, Diamonds, Clubs, Spades
}

public class Card(Suit suit, Rank rank, bool isFaceUp)
{
    private Suit Suit { get; } = suit;
    public Rank Rank { get; } = rank;
    public bool IsFaceUp { get; set; } = isFaceUp;

    [JsonIgnore]
    public string Symbol
    {
        get
        {
            if (!IsFaceUp) return "[?]";
            char suitChar;
            switch (Suit)
            {
                case Suit.Spades:   suitChar = '\u2660'; break;
                case Suit.Hearts:   suitChar = '\u2665'; break;
                case Suit.Diamonds: suitChar = '\u2666'; break;
                case Suit.Clubs:    suitChar = '\u2663'; break;
                default:            suitChar = '?';      break;
            }
            string rankStr;
            switch (Rank)
            {
                case Rank.Ace:   rankStr = "A"; break;
                case Rank.King:  rankStr = "K"; break;
                case Rank.Queen: rankStr = "Q"; break;
                case Rank.Jack:  rankStr = "J"; break;
                default:         rankStr = ((int)Rank).ToString(); break;
            }
            return $"[{rankStr}{suitChar}]";
        }
    }
    
    [JsonIgnore]
    public int Value
    {
        get 
        {
            if (Rank == Rank.Ace) return 11;
            if (Rank >= Rank.Ten) return 10; 
            return (int)Rank;
        }
    }
    
    public override string ToString() => $"{Rank} of {Suit}";
}
