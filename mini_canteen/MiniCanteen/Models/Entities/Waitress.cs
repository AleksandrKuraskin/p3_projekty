using System.Collections.Concurrent;

using MiniCanteen.Abstractions;
using MiniCanteen.Config.Assets;
using MiniCanteen.Models.Areas.ServiceArea;

namespace MiniCanteen.Models.Entities;

public class Waitress(string name, ServingCounter counter, BlockingCollection<Student> orders, Action<string> logger )
    : Entity(name, logger)
{
    protected override (string Message, string Icon) GetStateConfig(EntityState state) => state switch
    {
        EntityState.Idle => ("Waiting for orders", Icons.WaitressIdle),
        EntityState.Waiting => ("Waiting for food", Icons.ChefWaiting),
        EntityState.Working => ("Serving food", Icons.WaitressCarry),
        _ => (state.ToString(), "?")
    };

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // 1. Czekaj na zamówienie (blokada)
                SetStatus(EntityState.Idle);
                var student = orders.Take(token);

                // 2. Czekaj na jedzenie w kuchni (blokada)
                SetStatus(EntityState.Waiting);
                var meal = counter.Counter.Take(token);

                // 3. Zanieś
                SetStatus(EntityState.Working, $"Serving {student.Name}...");
                await SimulateWork(1000, 2000, token); 

                // 4. Odblokuj studenta
                student.ReceiveFood();
                Logger($"[magenta]{Name}[/] served {student.Name}.");
            }
            catch (OperationCanceledException) { break; }
        }
    }
}