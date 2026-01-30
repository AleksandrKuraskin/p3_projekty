using MiniCanteen.Abstractions;
using MiniCanteen.Models.Resources;

namespace MiniCanteen.Models.Entities;

public class Waitress(string name, ServingCounter counter, Action<string> logger) : IEntity(name, logger)
{
    private readonly ServingCounter _counter = counter;


    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetStatus(EntityState.Idle, "Watching pass...", "👀");

            try
            {
                var meal = _counter.MealsBuffer.Take(token);
                
                SetStatus(EntityState.Working, "Carrying to buffet...", "🏃‍♀️");
                await SimulateWork(500, 1500, token);
                
                if (!_counter.MealsBuffer.TryAdd(meal, 2000, token))
                {
                    SetStatus(EntityState.Critical, "Buffet full! Waiting...", "⚠️");
                    _counter.MealsBuffer.Add(meal, token);
                }
                
                Logger($"[magenta]{Name}[/] refilled the buffet.");
            }
            catch (OperationCanceledException) { break; }
        }
    }
}