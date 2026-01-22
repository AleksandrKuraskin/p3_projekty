namespace MiniCanteen.Models.Areas.Kitchen;

public class Kitchen
{
    public bool DroppedTomato { get; private set; }
    public bool DroppedCheese { get; private set; }
    public bool DroppedChili { get; private set; }
    
    public string ChefLeoState { get; private set; } = "💤 Idle";
    public string ChefMarioState { get; private set; } = "💤 Idle";
    public string ChefDiegoState { get; private set; } = "💤 Idle";
    
    private readonly object _lock = new object();
    
    public void SupplierPutIngredients()
    {
        lock (_lock)
        {
            DroppedTomato = false; DroppedCheese = false; DroppedTomato = false;
            
            var rand = new Random().Next(0, 3);
            switch (rand)
            {
                case 0:
                    DroppedCheese = true; DroppedChili = true;
                    break;
                case 1:
                    DroppedTomato = true; DroppedChili = true;
                    break;
                case 2:
                    DroppedTomato = true; DroppedCheese = true;
                    break;
            }

            Monitor.PulseAll(_lock);
        }
    }

    public void TryCook(string chefName, string needed1, string needed2)
    {
        lock (_lock)
        {
            var canCook = (chefName == "Chef Leo" && DroppedCheese && DroppedChili) ||
                          (chefName == "Chef Mario" && DroppedTomato && DroppedChili) ||
                          (chefName == "Chef Diego" && DroppedTomato && DroppedCheese);
            
            if (canCook)
            {
                DroppedTomato = false; DroppedCheese = false; DroppedChili = false;
                
                UpdateChefStatus(chefName, "🍳 Cooking...");
                Monitor.PulseAll(_lock);
            }
            else
            {
                UpdateChefStatus(chefName, "💤 Idle");
            }
        }
    }
    
    private void UpdateChefStatus(string name, string status)
    {
        switch (name)
        {
            case "Chef Leo":
                ChefLeoState = status;
                break;
            case "Chef Mario":
                ChefMarioState = status;
                break;
            case "Chef Diego":
                ChefDiegoState = status;
                break;
        }
    }
}