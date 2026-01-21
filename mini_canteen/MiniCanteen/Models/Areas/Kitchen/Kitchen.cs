namespace MiniCanteen.Models.Areas.Kitchen;

public class Kitchen
{
    public bool HasMushroom { get; set; }
    public bool HasCheese { get; set; }
    public bool HasPepperoni { get; set; }
    
    public string ChefLeoState { get; set; } = "💤 Idle";
    public string ChefMarioState { get; set; } = "💤 Idle";
    public string ChefDiegoState { get; set; } = "💤 Idle";
    
    private readonly object _lock = new object();
    
    public void SupplierPutIngredients()
    {
        lock (_lock)
        {
            HasMushroom = false; HasCheese = false; HasPepperoni = false;
            
            var rand = new Random().Next(0, 3);
            switch (rand)
            {
                case 0:
                    HasCheese = true; HasPepperoni = true;
                    break;
                case 1:
                    HasMushroom = true; HasPepperoni = true;
                    break;
                case 2:
                    HasMushroom = true; HasCheese = true;
                    break;
            }

            Monitor.PulseAll(_lock);
        }
    }

    public void TryCook(string chefName, string needed1, string needed2)
    {
        lock (_lock)
        {
            var canCook = (chefName == "Chef Leo" && HasCheese && HasPepperoni) ||
                          (chefName == "Chef Mario" && HasMushroom && HasPepperoni) ||
                          (chefName == "Chef Diego" && HasMushroom && HasCheese);
            
            if (canCook)
            {
                HasMushroom = false; HasCheese = false; HasPepperoni = false;
                
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