namespace MiniCanteen.Models.Areas.Kitchen;

using MiniCanteen.Config.Enums;
using MiniCanteen.Models.Areas.ServiceArea;

public class Kitchen
{
    public bool DroppedTomato { get; private set; }
    public bool DroppedCheese { get; private set; }
    public bool DroppedChili { get; private set; }

    public List<Chef> Chefs { get; private set; } = new List<Chef>();
    
    private readonly object _lock = new object();

    public Kitchen(ServiceArea serviceArea)
    {
        Chefs.Add(new Chef("Leo", IngredientType.Tomato, this, serviceArea));
        
        Chefs.Add(new Chef("Mario", IngredientType.Cheese, this, serviceArea));
        
        Chefs.Add(new Chef("Diego", IngredientType.Chili, this, serviceArea));
    }
    
    public void SupplierPutIngredients()
    {
        lock (_lock)
        {

            while (DroppedTomato || DroppedCheese || DroppedChili)
            {
                Monitor.Wait(_lock);
            }
            
            var excludedItem = (IngredientType)new Random().Next(0, 3);
            
            DroppedTomato = (excludedItem != IngredientType.Tomato);
            DroppedCheese = (excludedItem != IngredientType.Cheese);
            DroppedChili  = (excludedItem != IngredientType.Chili);

            Monitor.PulseAll(_lock);
        }
    }
    
    public bool TryTakeIngredients(IngredientType chefHas)
    {
        lock (_lock)
        {
            var canCook = false;

            switch (chefHas)
            {
                case IngredientType.Tomato:
                    if (DroppedCheese && DroppedChili) canCook = true;
                    break;
                
                case IngredientType.Cheese:
                    if (DroppedTomato && DroppedChili) canCook = true;
                    break;
                
                case IngredientType.Chili:
                    if (DroppedTomato && DroppedCheese) canCook = true;
                    break;
                default:
                    break;
            }

            if (canCook)
            {
                DroppedTomato = false;
                DroppedCheese = false;
                DroppedChili = false;
                
                Monitor.PulseAll(_lock);
                return true;
            }
        
            return false;
        }
    }
}