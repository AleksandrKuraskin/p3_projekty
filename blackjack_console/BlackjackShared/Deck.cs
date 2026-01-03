using System.Text.Json.Serialization;

namespace BlackjackShared;

using System;
using System.Collections.Generic;

public class Deck
{
    private Stack<Card> Cards { get; set; } = new Stack<Card>();
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
        var cards = Enumerable.Range(0, Count)
            .SelectMany(_ => Enum.GetValues<Suit>()
                .SelectMany(s => Enum.GetValues<Rank>()
                    .Select(r => new Card(s, r, false))))
            .ToList();
        var n = cards.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Shared.Next(n + 1);
            (cards[k], cards[n]) = (cards[n], cards[k]);
        }
        Cards = new Stack<Card>(cards);
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