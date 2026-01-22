using MiniCanteen.Models;
using MiniCanteen.UI;

namespace MiniCanteen;

class Program
{
    static async Task Main()
    {
        while (Console.WindowWidth < 100 || Console.WindowHeight < 30)
        {
            Console.WriteLine("Window too small! Please resize console to at least 100x30.");
        }
        
        var state = new CanteenState();
        
        _ = Task.Run(async () => {
            while (true) {
                await Task.Delay(2000);
                state.Kitchen.SupplierPutIngredients();
                state.Log("👨‍🍳 Supplier added ingredients.");
            }
        });
        
        StartChef(state, "Chef Diego", "🍞", "🍅", "🧀");
        StartChef(state, "Chef Leo", "🍅", "🍞", "🧀");
        StartChef(state, "Chef Mario", "🧀", "🍅", "🍞");
        
        _ = Task.Run(async () => {
            while(true) {
                await Task.Delay(5000);
                state.Log(state.Entrance.TryEnterQueue()
                    ? "🚪 Customer entered queue."
                    : "🛑 Customer balked (Queue Full).");
            }
        });

        _ = Task.Run(async () => {
            while(true) {
                if(state.Entrance.WaitingCount > 0) {
                    state.Entrance.SeatCustomer();
                    state.Log("✅ Host seated a guest.");
                }
                await Task.Delay(500);
            }
        });
        
        Console.Clear();
        while (true)
        {
            Console.SetCursorPosition(0,0); 
            ConsoleRenderer.ShowDashboard(state);
            await Task.Delay(16);
        }
    }

    static void StartChef(CanteenState state, string name, string has, string need1, string need2)
    {
        Task.Run(async () => {
            while (true)
            {
                state.Kitchen.TryCook(name, need1, need2);
                
                if (state.Kitchen.ChefDiegoState.Contains("🍳 Cooking...") && name == "Chef Diego" ||
                    state.Kitchen.ChefLeoState.Contains("🍳 Cooking...") && name == "Chef Leo" ||
                    state.Kitchen.ChefMarioState.Contains("🍳 Cooking...") && name == "Chef Mario")
                {
                    await Task.Delay(10000);
                    await state.ServiceArea.FoodBuffer.Writer.WriteAsync(1);
                    state.Log($"{name} finished cooking!");
                }
                
                await Task.Delay(200);
            }
        });
    }
}