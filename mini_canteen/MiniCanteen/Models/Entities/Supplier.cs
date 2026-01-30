using MiniCanteen.Abstractions;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models.Areas.Kitchen;

namespace MiniCanteen.Models.Entities;

public class Supplier(KitchenBoard board, Action<string> logger) : IEntity("Supplier", logger)
{
    private readonly KitchenBoard _board = board;

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetStatus(EntityState.Waiting, "Waiting...", "🚚");
            await SimulateWork(1000, 3000, token);

            SetStatus(EntityState.Working, "Delivering...", "📦");
            
            var excluded = (IngredientType)Random.Shared.Next(3);
            await Task.Run(() => _board.PlaceIngredients(excluded), token);
            
            Logger($"[blue]Supplier[/] delivered ingredients (No {excluded}).");
        }
    }
}