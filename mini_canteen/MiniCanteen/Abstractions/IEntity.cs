namespace MiniCanteen.Abstractions;

public abstract class IEntity
{
    public string Name { get; }
    public EntityStatus CurrentStatus { get; protected set; }
    
    protected Action<string> Logger { get; }

    protected IEntity(string name, Action<string> logger)
    {
        Name = name;
        Logger = logger;
        SetStatus(EntityState.Idle, "Init...", "💤");
    }

    protected void SetStatus(EntityState state, string message, string icon)
    {
        CurrentStatus = new EntityStatus(state, message, icon);
    }
    
    public abstract Task RunAsync(CancellationToken token);

    protected async Task SimulateWork(int minMs, int maxMs, CancellationToken token)
    {
        await Task.Delay(Random.Shared.Next(minMs, maxMs), token);
    }
}