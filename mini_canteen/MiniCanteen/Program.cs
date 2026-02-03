using MiniCanteen.Core;
using MiniCanteen.UI;
using Spectre.Console;

namespace MiniCanteen;

class Program
{
    static async Task Main()
    {
        Console.Title = "MiNI Canteen Simulator";
        
        var cts = new CancellationTokenSource();
        var manager = new CanteenManager();

        // Uruchamiamy symulację w tle
        var simTask = manager.StartSimulation(cts.Token);

        // AlternateScreen jest synchroniczny. 
        // Musimy zablokować wątek główny wewnątrz (GetAwaiter().GetResult()), 
        // aby ekran się nie zamknął, dopóki pętla Live Display działa.
        AnsiConsole.AlternateScreen(() =>
        {
            Console.WriteLine("HELLO!!!");
            Console.CursorVisible = false;
            var layout = UI.ConsoleLayout.Build();

            // Uruchamiamy Live Display
            AnsiConsole.Live(layout)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // Aktualizacja UI (zczytuje stan manager'a)
                        UI.ConsoleLayout.Update(layout, manager);
                        ctx.Refresh();
                        
                        // Obsługa klawiszy wyjścia
                        if (Console.KeyAvailable)
                        {
                            var k = Console.ReadKey(true).Key;
                            if (k == ConsoleKey.Q || k == ConsoleKey.Escape) 
                            {
                                cts.Cancel();
                            }
                        }

                        // Opóźnienie pętli UI
                        try 
                        { 
                            await Task.Delay(100, cts.Token); 
                        }
                        catch (OperationCanceledException) { break; }
                    }
                })
                .GetAwaiter()
                .GetResult(); // <--- Tu czekamy na zakończenie Taska UI
        });
        
        // Czekamy na bezpieczne zakończenie tasków symulacji
        try 
        { 
            await simTask; 
        } 
        catch (OperationCanceledException) { }

        Console.CursorVisible = true;
        AnsiConsole.MarkupLine("[green]Symulacja zakończona.[/]");
        Console.WriteLine("HELLO!!!");
    }
}