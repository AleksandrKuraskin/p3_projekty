using MiniCanteen.Abstractions;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;

namespace MiniCanteen.Models.Entities;

public class Chef(
    string name,
    IngredientType ingredient,
    KitchenBoard board,
    ServingCounter counter,
    Action<string> logger
    ) : IEntity(name, logger)
{
    private readonly IngredientType _myIngredient = ingredient;
    private readonly KitchenBoard _board = board;
    private readonly ServingCounter _counter = counter;

    public override async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetStatus(EntityState.Waiting, $"Waiting for items...", "👀");
            
            var gotIngredients = false;
            while (!gotIngredients && !token.IsCancellationRequested)
            {
                gotIngredients = await Task.Run(() => _board.TryTakeIngredients(_myIngredient), token);
            }

            if (token.IsCancellationRequested) break;

            SetStatus(EntityState.Working, "Cooking...", "🍳");
            await SimulateWork(2000, 4000, token);

            SetStatus(EntityState.Working, "Plating...", "🥡");
            try 
            {
                _counter.MealsBuffer.Add("Meal", token);
                Logger($"[green]{Name}[/] cooked a meal.");
            }
            catch (OperationCanceledException) { break; }
        }
    }
    
    public IngredientType Ingredient => _myIngredient;
}