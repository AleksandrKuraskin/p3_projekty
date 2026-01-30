using Spectre.Console;

namespace MiniCanteen.Abstractions;

public enum EntityState
{
    Idle,
    Working,
    Waiting,
    Critical,
    Success
}

public record EntityStatus(EntityState State, string Message, string Icon)
{
    private Color GetColor() => State switch
    {
        EntityState.Idle => Color.Grey,
        EntityState.Working => Color.Blue,
        EntityState.Waiting => Color.Yellow,
        EntityState.Critical => Color.Red,
        EntityState.Success => Color.Green,
        _ => Color.White
    };

    public string ToMarkup() => $"[{GetColor().ToMarkup()}]{Icon} {Message}[/]";
}