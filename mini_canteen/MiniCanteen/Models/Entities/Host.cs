using System.Collections.Concurrent;
using MiniCanteen.Abstractions;
using MiniCanteen.Config.Assets;

namespace MiniCanteen.Models.Entities;

public class Host(int diningCapacity, int queueCapacity, Action<string> logger) : Entity("Host", logger)
{
    public BlockingCollection<Student> EntranceQueue { get; } = new(queueCapacity);
    private readonly SemaphoreSlim _diningSemaphore = new(diningCapacity, diningCapacity);

    public int CurrentOccupancy => diningCapacity - _diningSemaphore.CurrentCount;

    public bool TryAddToQueue(Student student) => EntranceQueue.TryAdd(student);

    public void StudentLeft() => _diningSemaphore.Release();

    protected override (string Message, string Icon) GetStateConfig(EntityState state) => state switch
    {
        EntityState.Idle => ("Waiting for students", Icons.HostIdle),
        EntityState.Waiting => ("Waiting for seats", Icons.HostWaiting),
        EntityState.Working => ("Seating student", Icons.HostSeating),
        _ => (state.ToString(), "?")
    };

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Czekaj na kogoś w kolejce (blokada)
                SetStatus(EntityState.Idle);
                var student = EntranceQueue.Take(token);

                // Czekaj na wolne miejsce w sali (blokada)
                if (_diningSemaphore.CurrentCount == 0) SetStatus(EntityState.Waiting);
                await _diningSemaphore.WaitAsync(token);

                // Wpuszczanie
                SetStatus(EntityState.Working, $"Seating {student.Name}");
                student.GrantEntry();
                await SimulateWork(500, 1000, token);
            }
            catch (OperationCanceledException) { break; }
        }
    }
}