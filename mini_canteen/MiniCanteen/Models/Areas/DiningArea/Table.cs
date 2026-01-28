namespace MiniCanteen.Models.Areas.DiningArea;

public class Table
{
    public string Name { get; }
    public Student[] Students { get; } = new Student[4];
    
    public SemaphoreSlim[] Forks { get; } = new SemaphoreSlim[4];

    public Table(string name, string p1, string p2, string p3, string p4)
    {
        Name = name;
        for (var i = 0; i < 4; i++) Forks[i] = new SemaphoreSlim(1, 1);

        Students[0] = new Student(p1, Forks[0], Forks[1]);
        Students[1] = new Student(p2, Forks[1], Forks[2]);
        Students[2] = new Student(p3, Forks[2], Forks[3]);
        Students[3] = new Student(p4, Forks[3], Forks[0]); 
    }
}