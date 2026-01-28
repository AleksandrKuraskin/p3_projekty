using System.Threading.Channels;

namespace MiniCanteen.Models.Areas.ServiceArea;

public class ServiceArea
{
    private const int InitCapacity = 5;
    private int _currentCount = 0;
    
    public Channel<int> FoodBuffer { get; } = Channel.CreateBounded<int>(InitCapacity);

    public int CurrentFoodCount => _currentCount;
    public int SpaceLeft => InitCapacity - _currentCount;
    public int TotalDelivered { get; set; } = 0;
    
    public string WaiterGabrielaState { get; set; } = "💤 Idle";
    public string WaiterSofiaState { get; set; } = "💤 Idle";
    
    public async Task AddFoodAsync(CancellationToken token)
    {
        await FoodBuffer.Writer.WriteAsync(1, token);
        Interlocked.Increment(ref _currentCount);
    }
    
    public async Task<int> TakeFoodAsync(CancellationToken token)
    {
        var food = await FoodBuffer.Reader.ReadAsync(token);
        Interlocked.Decrement(ref _currentCount);
        return food;
    }
}