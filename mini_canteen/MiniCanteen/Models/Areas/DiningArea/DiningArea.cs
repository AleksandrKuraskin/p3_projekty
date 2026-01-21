namespace MiniCanteen.Models.Areas.DiningArea;

public class DiningArea
{
    public List<Table> Tables { get; } = new List<Table>();

    public DiningArea()
    {
        // Setup 2 Tables
        Tables.Add(new Table("Table 1 (Greeks)", "Soc", "Pla", "Ari", "Pyt"));
        Tables.Add(new Table("Table 2 (Moderns)", "Kan", "Nie", "Des", "Hum"));
    }
}