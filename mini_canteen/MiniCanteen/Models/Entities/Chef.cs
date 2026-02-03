using MiniCanteen.Abstractions;
using MiniCanteen.Config.Enums;
using MiniCanteen.Config.Assets;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;

namespace MiniCanteen.Models.Entities;

public class Chef(
    string name,
    IngredientType ingredient,
    Kitchen board,
    ServingCounter counter,
    Action<string> logger
    ) : Entity(name, logger)
{
    private readonly Kitchen _board = board;
    private readonly ServingCounter _counter = counter;
    
    public readonly IngredientType Ingredient = ingredient;
    protected override (string Message, string Icon) GetStateConfig(EntityState state) => state switch
    {
        EntityState.Idle => ("Idle...", Icons.ChefIdle),
        EntityState.Waiting => ("Waiting for ingredient...", Icons.ChefWaiting),
        EntityState.Working => ("Cooking...", Icons.ChefCooking),
        EntityState.Success => ("Plating...", Icons.ChefPlating),
        _ => (state.ToString(), "?")
    };

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetStatus(EntityState.Waiting);
            
            var gotIngredients = false;
            while (!gotIngredients && !token.IsCancellationRequested)
            {
                gotIngredients = await Task.Run(() => _board.TryTakeIngredients(Ingredient), token);
            }

            if (token.IsCancellationRequested) break;

            SetStatus(EntityState.Working);
            await SimulateWork(2000, 4000, token);

            SetStatus(EntityState.Success);
            try 
            {
                _counter.Counter.Add("Meal", token);
                Logger($"[green]{Name}[/] cooked a meal.");
            }
            catch (OperationCanceledException) { break; }
        }
    }
}