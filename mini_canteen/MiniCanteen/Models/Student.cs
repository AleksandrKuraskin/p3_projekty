namespace MiniCanteen.Models;

public class Student(string name, object left, object right)
{
    public string Name { get; } = name;
    public string State { get; set; } = "Thinking";
    public string Icon { get; set; } = "🧐";
    public object LeftFork { get; } = left;
    public object RightFork { get; } = right;
}