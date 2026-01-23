using MiniCanteen.Models;
using MiniCanteen.UI;
using Spectre.Console;

namespace MiniCanteen;

class Program
{
    static async Task Main()
    {
        while (Console.WindowWidth < 100 || Console.WindowHeight < 30)
        {
            Console.Clear();
            Console.WriteLine("⚠️ Window too small!");
            Console.WriteLine($"Current: {Console.WindowWidth}x{Console.WindowHeight}");
            Console.WriteLine("Required: 100x30");
            Console.WriteLine("Please resize console...");
            Thread.Sleep(500);
        }

        var state = new CanteenState();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Adding supplier logic...");
            while (true)
            {
                await Task.Delay(500);

                state.Kitchen.SupplierPutIngredients();
                state.Log("👨‍🍳 Supplier refilled table.");
            }
        });

        foreach (var chef in state.Kitchen.Chefs)
        {
            chef.StartWork();
        }

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Adding customers logic...");
            while (true)
            {
                await Task.Delay(2000); // New customer every 2s
                state.Log(state.Entrance.TryEnterQueue()
                    ? "🚪 Customer entered queue."
                    : "🛑 Customer balked (Queue Full).");
            }
        });

        _ = Task.Run(async () =>
        {
            while (true)
            {
                if (state.Entrance.WaitingCount > 0)
                {
                    state.Entrance.SeatCustomer();
                    state.Log("✅ Host seated a guest.");
                }

                await Task.Delay(500);
            }
        });

        AnsiConsole.Write(new Text("\u001b[?1049h"));
        Console.CursorVisible = false;

        try
        {
            // Now we can properly AWAIT the loop!
            await RunDashboardLoop(state);
        }
        finally
        {
            // Restore Main Screen Buffer (Clean Exit)
            // \u001b[?1049l is the exit code
            AnsiConsole.Write(new Text("\u001b[?1049l"));
            Console.CursorVisible = true;
        }
    }

    private static async Task RunDashboardLoop(CanteenState state)
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Q || key == ConsoleKey.Escape) break;
                if (key == ConsoleKey.C) state.SystemLog.Clear();
            }

            Console.SetCursorPosition(0, 0); 
            ConsoleRenderer.ShowDashboard(state);
            
            await Task.Delay(50);
        }
    }
}