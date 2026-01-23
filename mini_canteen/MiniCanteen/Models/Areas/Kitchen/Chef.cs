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

    public void StartWork()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                bool success = _kitchen.TryTakeIngredients(Ingredient);

                if (success)
                {
                    Status = ChefStatus.Cooking;
                    
                    await Task.Delay(10000); 
                    
                    await _serviceArea.FoodBuffer.Writer.WriteAsync(1);
                        
                    Status = ChefStatus.Idle;
                }
                else
                {
                    Status = ChefStatus.Idle;
                    await Task.Delay(200);
                }
            }
        });
    }
}