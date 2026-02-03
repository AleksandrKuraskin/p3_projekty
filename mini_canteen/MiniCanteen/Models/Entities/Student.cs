using System.Collections.Concurrent;
using MiniCanteen.Abstractions;
using MiniCanteen.Config.Assets;

namespace MiniCanteen.Models;

public class Student(
    string name, 
    SemaphoreSlim leftFork, 
    SemaphoreSlim rightFork,
    BlockingCollection<Student> ordersQueue,
    Action<string> logger
    ) : Entity(name, logger)
{
    private readonly SemaphoreSlim _entrySemaphore = new(0, 1);
    private readonly SemaphoreSlim _plateSemaphore = new(0, 1);

    public void GrantEntry() => _entrySemaphore.Release();

    public void ReceiveFood() => _plateSemaphore.Release();

    protected override (string Message, string Icon) GetStateConfig(EntityState state) => state switch
    {
        EntityState.Idle => ("Thinking", Icons.StudentThinking),
        EntityState.Waiting => ("In Queue", Icons.StudentQueue),
        EntityState.Critical => ("Hungry", Icons.StudentHungry),
        EntityState.Working => ("Eating", Icons.StudentEating),
        EntityState.Success => ("Leaving", Icons.StudentLeaving),
        _ => (state.ToString(), "?")
    };

    public override async Task RunAsync(CancellationToken token)
    {
        try
        {
            // 1. Czekanie na wejście (Semafor)
            SetStatus(EntityState.Waiting);
            await _entrySemaphore.WaitAsync(token);

            // 2. Siadanie i zamawianie
            SetStatus(EntityState.Critical);
            ordersQueue.Add(this, token);
            
            await _plateSemaphore.WaitAsync(token);
            
            SetStatus(EntityState.Working, "Got food! Grabbing forks...");
            await leftFork.WaitAsync(token);
            try
            {
                await Task.Delay(100, token);
                await rightFork.WaitAsync(token);
                try
                {
                    SetStatus(EntityState.Working, "Eating...");
                    await SimulateWork(3000, 6000, token);
                }
                finally { rightFork.Release(); }
            }
            finally { leftFork.Release(); }

            // 5. Wyjście
            SetStatus(EntityState.Success);
            await Task.Delay(1000, token);
        }
        catch (OperationCanceledException) { }
    }
}