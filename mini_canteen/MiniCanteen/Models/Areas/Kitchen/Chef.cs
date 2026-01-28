namespace MiniCanteen.Models.Areas.Kitchen;

using MiniCanteen.Config.Enums;
using MiniCanteen.Models.Areas.ServiceArea;

public class Chef(string name, IngredientType ingredient,
    Kitchen kitchen, ServiceArea serviceArea)
{
    public string Name { get; } = name;
    public IngredientType Ingredient { get; } = ingredient;

    // State
    public ChefStatus Status { get; private set; } = ChefStatus.Idle;

    private readonly Kitchen _kitchen = kitchen;
    private readonly ServiceArea _serviceArea = serviceArea;

    public async Task StartWorkAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Status = ChefStatus.Waiting;
                
                var success = await Task.Run(() => _kitchen.TryTakeIngredients(Ingredient), token);

                if (success)
                {
                    Status = ChefStatus.Cooking;

                    await Task.Delay(10000, token); 
                
                    await _serviceArea.AddFoodAsync(token);
                    Status = ChefStatus.Idle;
                }
                else
                {
                    await Task.Delay(100, token);
                }
            }
        }
        catch (OperationCanceledException) {}
    }
}