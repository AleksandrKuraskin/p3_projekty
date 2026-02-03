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