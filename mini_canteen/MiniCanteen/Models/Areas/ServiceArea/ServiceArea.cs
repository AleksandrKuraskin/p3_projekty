using System.Threading.Channels;

namespace MiniCanteen.Models.Areas.ServiceArea;

public class ServiceArea
{
    private const int InitCapacity = 5;
    
    public Channel<int> FoodBuffer { get; } = Channel.CreateBounded<int>(InitCapacity);
    
    public int ItemsInBuffer => InitCapacity - (int)GetCapacityRemaining();
    public int TotalDelivered { get; set; } = 0;
    
    public string WaiterGabrielaState { get; set; } = "💤 Idle";
    public string WaiterSofiaState { get; set; } = "💤 Idle";

    private double GetCapacityRemaining()
    {
        // Reflection hack or tracking manually required for exact count in Channel, 
        // but for simple UI we can track manually in logic loop.
        return 0; 
    }
        
    // Simple counter helper
    public int MealCount = 0;
}