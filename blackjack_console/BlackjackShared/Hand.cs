using System.Text.Json.Serialization;

namespace BlackjackShared;

public class Hand
{
    public List<Card> Cards { get; set; } = [];
    
    [JsonIgnore] public bool IsBust => Score() > 21;
    [JsonIgnore] public bool IsBlackjack => Cards.Count == 2 && Score() == 21 && Cards.All(c => c.IsFaceUp);
    [JsonIgnore] public string Status => (HasHiddenCard || !IsBust && !IsBlackjack) ? "" : IsBust ? "Bust!" : "Blackjack!";
    
    [JsonIgnore] public bool HasHiddenCard => Cards.Any(c => !c.IsFaceUp);
    
    [JsonIgnore] public string StringScore => Score() >= 0 ? Score().ToString() : $"Soft {Score()*-1}";
    [JsonIgnore] public int AbsScore => Score() >= 0 ? Score() : Score()*-1;
    
    [JsonConstructor]
    public Hand() { }

    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    public int Score()
    {
        var score = Cards.Where(c => c.IsFaceUp).Sum(c => c.Value);
        var aceCount = Cards.Count(c => c.Rank is Rank.Ace && c.IsFaceUp);

        while (score > 21 && aceCount > 0)
        {
            score -= 10;
            aceCount--;
        }

        if (aceCount > 0 && score < 21)
        {
            score = -score;
        }
        return score;
    }

    public override string ToString()
    {
        var score = Score();
        return string.Join(", ", Cards.Select(card => card.Symbol));
    }
}