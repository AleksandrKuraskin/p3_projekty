using System.Collections.Concurrent;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models;
using MiniCanteen.Abstractions;
using MiniCanteen.Models.Entities;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;

using Spectre.Console;

namespace MiniCanteen.Core;

public class CanteenManager
{
    public ConcurrentQueue<string> Logs { get; } = new();

    public Kitchen Kitchen { get; } = new();
    public ServingCounter Counter { get; } = new();
    
    public BlockingCollection<Student> Orders { get; } = new(); 
    
    public Supplier Supplier { get; }
    public Host Host { get; }
    public List<Chef> Chefs { get; } = new();
    public List<Waitress> Waitresses { get; } = new();
    
    private readonly List<Student> _students = new();
    public IEnumerable<Student> Students => _students.ToArray();

    private readonly SemaphoreSlim[] _forks;

    public CanteenManager()
    {
        Action<string> logger = Log;

        Supplier = new Supplier(Kitchen, logger);
        Host = new Host(diningCapacity: 8, queueCapacity: 6, logger);

        Chefs.Add(new Chef("Mario", IngredientType.Tomato, Kitchen, Counter, logger));
        Chefs.Add(new Chef("Luigi", IngredientType.Cheese, Kitchen, Counter, logger));
        Chefs.Add(new Chef("Wario", IngredientType.Chili, Kitchen, Counter, logger));

        Waitresses.Add(new Waitress("Julia", Counter, Orders, logger));
        Waitresses.Add(new Waitress("Anna", Counter, Orders, logger));

        _forks = new SemaphoreSlim[8];
        for(int i=0; i<8; i++) _forks[i] = new SemaphoreSlim(1,1);
    }

    public async Task StartSimulation(CancellationToken token)
    {
        var tasks = new List<Task>();
        
        // --- KLUCZOWA POPRAWKA ---
        // Ponieważ Host i Kelnerki używają metod blokujących (BlockingCollection.Take),
        // musimy uruchomić ich pętle RunAsync na ThreadPoolu (Task.Run), 
        // aby nie zablokowały wątku głównego przed pierwszym await.

        tasks.Add(Task.Run(() => Supplier.RunAsync(token), token));
        tasks.Add(Task.Run(() => Host.RunAsync(token), token));
        
        foreach (var chef in Chefs)
        {
            tasks.Add(Task.Run(() => chef.RunAsync(token), token));
        }

        foreach (var waitress in Waitresses)
        {
            tasks.Add(Task.Run(() => waitress.RunAsync(token), token));
        }

        // Generator studentów ma "await Delay" na początku, więc nie blokuje, 
        // ale dla spójności też wrzucamy w Task.Run
        tasks.Add(Task.Run(() => StudentGenerator(token), token));

        await Task.WhenAll(tasks);
    }

    private async Task StudentGenerator(CancellationToken token)
    {
        var names = new[] { "Kant", "Platon", "Nietzsche", "Sokrates", "Descartes", "Hume" };
        var rnd = new Random();

        try 
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(rnd.Next(2000, 4000), token);

                lock(_students)
                {
                    _students.RemoveAll(s => s.CurrentStatus.State == EntityState.Success);
                }

                if (_students.Count < 12)
                {
                    var name = names[rnd.Next(names.Length)] + " " + rnd.Next(100);
                    int seatIdx = rnd.Next(0, 8); 
                    
                    var student = new Student(name, _forks[seatIdx], _forks[(seatIdx + 1) % 8], Orders, Log);
                    
                    if (Host.TryAddToQueue(student))
                    {
                        lock(_students) _students.Add(student);
                        
                        // Studenta też uruchamiamy w tle
                        _ = Task.Run(() => student.RunAsync(token), token);
                        
                        Log($"[cyan]Entrance[/]: {name} joined queue.");
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Log(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        Logs.Enqueue($"[grey]{time}[/] {message}");
        if (Logs.Count > 20) Logs.TryDequeue(out _);
    }
}