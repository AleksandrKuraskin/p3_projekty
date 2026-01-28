using MiniCanteen.Config;
using MiniCanteen.Core;
using Spectre.Console;

namespace MiniCanteen;

class Program
{
    static async Task Main()
    {
        Console.Title = "MiNI Canteen Simulator";
        while (Console.WindowWidth < 100 || Console.WindowHeight < 30)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[red]⚠️ Console window is too small![/]");
            AnsiConsole.MarkupLine(
                $"Required: [bold]120x40[/], Current: [bold]{Console.WindowWidth}x{Console.WindowHeight}[/]");
            AnsiConsole.MarkupLine("Try resizing the console.");
            await Task.Delay(500);
        }

        var simulation = new CanteenSimulation();

        var cts = new CancellationTokenSource();
        
        await AnsiConsole.Live(ConsoleRenderer.CreateLayout(simulation.State))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Bottom)
            .StartAsync(async context =>
            {
                var simTask = simulation.StartAsync(cts.Token);
                
                while (!cts.Token.IsCancellationRequested)
                {
                    context.UpdateTarget(ConsoleRenderer.UpdateView(simulation.State));
                    
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
                            await cts.CancelAsync();
                    }

                    try
                    {
                        await Task.Delay(33, cts.Token);
                    }
                    catch(OperationCanceledException)
                    {
                        break;
                    }
                    
                }

                try
                {
                    await simTask;
                }
                catch (OperationCanceledException) {}
            });

        Console.CursorVisible = true;
        AnsiConsole.MarkupLine("[green]👋 Simulation done![/]");
    }
}