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
        var simTask = simulation.StartAsync(cts.Token);
        
        AnsiConsole.AlternateScreen(async () =>
        {
            // Ukrywamy kursor
            Console.CursorVisible = false;

            // Tworzymy bazowy szkielet Layoutu
            var layout = ConsoleRenderer.CreateLayout();

            // Uruchamiamy Live Display na tym Layoucie
            await AnsiConsole.Live(layout)
                .AutoClear(false) // W AlternateScreen nie czyścimy, tylko nadpisujemy
                .StartAsync(async ctx =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // Aktualizujemy zawartość Layoutu
                        ConsoleRenderer.UpdateLayout(layout, simulation.State);
                        ctx.Refresh(); // Wymuszamy odświeżenie

                        // Obsługa klawiszy
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
                            {
                                cts.Cancel();
                            }
                        }

                        try
                        {
                            await Task.Delay(50, cts.Token);
                        }
                        catch (OperationCanceledException) { break; }
                    }
                });
        });

        // Po wyjściu z bloku AlternateScreen wracamy do normalnej konsoli
        try { await simTask; } catch (OperationCanceledException) { }
        
        Console.CursorVisible = true;
        AnsiConsole.MarkupLine("[green]👋 Symulacja zakończona poprawnie.[/]");
    }
}