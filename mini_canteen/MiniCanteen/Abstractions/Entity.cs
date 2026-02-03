using MiniCanteen.Config;

namespace MiniCanteen.Abstractions;

public record EntityStatus(EntityState State, string Message, string Icon)
{
    public string ToMarkup()
    {
        var color = State switch
        {
            EntityState.Idle => Theme.TextIdle,
            EntityState.Working => Theme.TextAction,
            EntityState.Waiting => Theme.TextWarning,
            EntityState.Critical => Theme.TextFail,
            EntityState.Success => Theme.TextSuccess,
            _ => Theme.TextGeneric
        };
        
        return $"[{color}]{Icon} {Message}[/]";
    }
}

public abstract class Entity
{
    public string Name { get; }
    public EntityStatus CurrentStatus { get; set; } = new EntityStatus(EntityState.Idle, "Init", "⚙️");
    
    protected Action<string> Logger { get; }

    protected Entity(string name, Action<string> logger)
    {
        Name = name;
        Logger = logger;
        SetStatus(EntityState.Idle);
    }

    protected void SetStatus(EntityState state, string? customMessage = null)
    {
        var (defaultMessage, icon) = GetStateConfig(state);
        CurrentStatus = new EntityStatus(state, customMessage ?? defaultMessage, icon);
    }
    
    protected abstract (string Message, string Icon) GetStateConfig(EntityState state);
    
    public abstract Task RunAsync(CancellationToken token);

    protected async Task SimulateWork(int minMs, int maxMs, CancellationToken token)
    {
        await Task.Delay(Random.Shared.Next(minMs, maxMs), token);
    }
}