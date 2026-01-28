using MiniCanteen.Models;

namespace MiniCanteen.Core;

public class CanteenSimulation
{
    public CanteenState State { get; } = new CanteenState();
    public async Task StartAsync(CancellationToken token)
    {
        var tasks = new List<Task>();
        
        tasks.Add(Task.Run(async () => {
            try{
                while (!token.IsCancellationRequested) 
                {
                    await Task.Delay(5000, token); 
                
                    State.Kitchen.SupplierPutIngredients();
                    State.Log("[blue]🚚 Supplier:[/]: Dropping ingredients.");
                }
            }
            catch (OperationCanceledException) {}
        }, token));
        
        foreach (var chef in State.Kitchen.Chefs)
        {
            tasks.Add(chef.StartWorkAsync(token));
        }
        
        tasks.Add(Task.Run(async () => {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(new Random().Next(1000, 3000), token);
                    State.Log(
                        State.Entrance.TryEnterQueue() ?
                            "[green]ENTRANCE[/]: Student entered the queue." :
                            "[red]DOORS[/]: Student left (the queue is full)."
                        );
                }
            }
            catch(OperationCanceledException){}
            
        }, token));

        tasks.Add(Task.Run(async () => {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (State.Entrance.WaitingCount > 0)
                    {
                        await State.Entrance.SeatCustomerAsync(token);
                        State.Log("[cyan]HOST[/]: Student seated!");
                    }
                    await Task.Delay(500, token);
                }
            }
            catch(OperationCanceledException){}
            
        }, token));
        
        foreach (var table in State.DiningArea.Tables)
        {
            foreach (var student in table.Students)
            {
                tasks.Add(student.StartCycleAsync(token));
            }
        }
        
        await Task.WhenAll(tasks);
    }
}