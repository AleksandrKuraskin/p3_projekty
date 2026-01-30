using System.Collections.Concurrent;

namespace MiniCanteen.Models.Areas.ServiceArea;

public class ServingCounter
{
    public BlockingCollection<string> MealsBuffer { get; } = new(5);
    public int MealsReady => MealsBuffer.Count;
}