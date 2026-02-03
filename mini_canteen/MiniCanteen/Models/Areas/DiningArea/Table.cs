namespace MiniCanteen.Models.Areas.DiningArea;

public class Table
{
    public string Name { get; }
    public Student[] Students { get; } = new Student[4];
    
    public SemaphoreSlim[] Forks { get; } = new SemaphoreSlim[4];

    public Table(string name)
    {
        Name = name;
        for (var i = 0; i < 4; i++) Forks[i] = new SemaphoreSlim(1, 1);
    }
}