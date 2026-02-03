using System.Collections.Concurrent;

namespace MiniCanteen.Models.Areas.ServiceArea;

public class ServingCounter
{
    // Okno wydawcze ma małą pojemność (np. 5 talerzy). 
    // Jak kucharze zrobią za dużo, muszą czekać aż kelnerki odbiorą.
    private const int MaxMeals = 5;
    public BlockingCollection<string> Counter { get; } = new(MaxMeals);

    public int MealsReady => Counter.Count;
}