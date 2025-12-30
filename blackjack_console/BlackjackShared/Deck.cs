using System.Text.Json.Serialization;

namespace BlackjackShared;

using System;
using System.Collections.Generic;

public class Deck
{
    public Stack<Card> Cards { get; set; } = new Stack<Card>();
    private int Count { get; set; } = 5;

    [JsonConstructor]
    public Deck()
    {
        ResetAndShuffle();
    }

   public Deck(int count)
   {
       Count = count;
       ResetAndShuffle();
   }

    private int CardCount => Cards.Count;
    private void ResetAndShuffle()
    {
        var cards = new List<Card>();
        for (var i = 0; i < Count; i++)
        {
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank r in Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(s, r, false));
                }
            }
        }
        
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (cards[k], cards[n]) = (cards[n], cards[k]);
        }
        Cards = new Stack<Card>(cards);
        Console.WriteLine($"Deck reset and shuffled with first card being {Cards.Peek().ToString()}");
    }

    public void CheckEmpty()
    {
        if(CardCount < 0.2 * 52 * Count)
        {
            ResetAndShuffle();
        }
    }

    public Card Draw(bool faseUp = true)
    {
        if (CardCount == 0) throw new InvalidOperationException("Deck is empty!");
        var card = Cards.Pop();
        card.IsFaceUp = faseUp;
        return card;
    }
}