namespace MiniCanteen.Models.Areas.Entrance;

public class Entrance
{
    private const int MaxChairs = 4;
    
    public int WaitingCount => MaxChairs - _chairs.CurrentCount;
    public int TotalSeated{ get; private set; } = 0;
    public int TotalWalkedAway { get; private set; } = 0;
    public string HostStatus { get; private set; } = "💤 Idle";
    
    private readonly SemaphoreSlim _chairs = new SemaphoreSlim(MaxChairs, MaxChairs);
    
    public bool TryEnterQueue()
    {
        if (_chairs.Wait(0))
        {
            return true;
        }
        else
        {
            TotalWalkedAway++;
            return false;
        }
    }

    public void SeatCustomer()
    {
        HostStatus = "👋 Seating Guest...";
        Thread.Sleep(500);
        _chairs.Release();
        TotalSeated++;
        HostStatus = "💤 Idle";
    }
}