using System.Collections.Concurrent;
using MiniCanteen.Abstractions;

namespace MiniCanteen.Models.Entities;

public class Host(int capacity, Action<string> logger) : IEntity("Host", logger)
{
    public ConcurrentQueue<Student> Queue { get; } = new();
    
    private readonly SemaphoreSlim _diningCapacity = new SemaphoreSlim(capacity, capacity);

    public bool TryAddToQueue(Student student)
    {
        if (Queue.Count >= 5) return false;
        Queue.Enqueue(student);
        return true;
    }

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Queue.IsEmpty)
            {
                SetStatus(EntityState.Idle, "Greeting...", "👋");
                await Task.Delay(500, token);
                continue;
            }

            if (Queue.TryPeek(out var student))
            {
                SetStatus(EntityState.Waiting, "Checking tables...", "🧐");
                
                await _diningCapacity.WaitAsync(token);

                if (Queue.TryDequeue(out var s))
                {
                    SetStatus(EntityState.Working, $"Seating {s.Name}", "👉");
                    s.GrantEntry();
                    await Task.Delay(1000, token);
                }
                else
                {
                    _diningCapacity.Release();
                }
            }
        }
    }

    public void StudentLeft()
    {
        _diningCapacity.Release();
    }
    
    public int CurrentOccupancy => _diningCapacity.CurrentCount;
}