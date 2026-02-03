using MiniCanteen.Abstractions;
using MiniCanteen.Config.Assets;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models.Areas.Kitchen;

namespace MiniCanteen.Models.Entities;

public class Supplier(Kitchen kitchen, Action<string> logger) : Entity("Supplier", logger)
{
    protected override (string Message, string Icon) GetStateConfig(EntityState state) => state switch
    {
        EntityState.Idle => ("Resting", Icons.ChefIdle),
        EntityState.Working => ("Delivering", Icons.Truck),
        _ => (state.ToString(), "?")
    };

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetStatus(EntityState.Idle);
            await SimulateWork(2000, 4000, token);

            SetStatus(EntityState.Working);
            var excluded = (IngredientType)Random.Shared.Next(3);
            await Task.Run(() => kitchen.PlaceIngredients(excluded), token);
            Logger($"[blue]{Name}[/] delivered ingredients.");
        }
    }
}