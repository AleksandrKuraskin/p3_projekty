using System.Collections.Concurrent;

using MiniCanteen.Models.Areas.DiningArea;
using MiniCanteen.Models.Areas.Entrance;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;

namespace MiniCanteen.Models;

public class CanteenState
{
    public Entrance Entrance { get; } = new Entrance();
    public ServiceArea ServiceArea { get; } = new ServiceArea();
    public Kitchen Kitchen { get; }
    public DiningArea DiningArea { get; } = new DiningArea();

    public ConcurrentQueue<string> SystemLog { get; } = new ConcurrentQueue<string>();

    public CanteenState()
    {
        Kitchen = new Kitchen(ServiceArea);
    }
    
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        SystemLog.Enqueue($"[grey]{timestamp}[/] {message}");
        if (SystemLog.Count > 10) SystemLog.TryDequeue(out _);
    }
}