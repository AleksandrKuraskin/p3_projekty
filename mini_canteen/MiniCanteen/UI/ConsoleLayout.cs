using MiniCanteen.Core;
using Spectre.Console;
using MiniCanteen.Config.Assets; // Zakładam, że Icons.cs został
using MiniCanteen.Models.Entities;

namespace MiniCanteen.UI;

public static class ConsoleLayout
{
    public static Layout Build()
    {
        return new Layout("Root")
            .SplitRows(
                new Layout("Top").SplitColumns(
                    new Layout("Kitchen").Ratio(2),
                    new Layout("Service").Ratio(1)
                ),
                new Layout("Bottom").SplitColumns(
                    new Layout("Dining").Ratio(2),
                    new Layout("Entrance").Ratio(1),
                    new Layout("Logs").Ratio(1)
                )
            );
    }

    public static void Update(Layout layout, CanteenManager mgr)
    {
        layout["Kitchen"].Update(RenderKitchen(mgr));
        layout["Service"].Update(RenderService(mgr));
        layout["Dining"].Update(RenderDining(mgr));
        layout["Entrance"].Update(RenderEntrance(mgr));
        layout["Logs"].Update(RenderLogs(mgr));
    }

    private static Panel RenderKitchen(CanteenManager mgr)
    {
        var table = new Table().Border(TableBorder.None).Expand().HideHeaders();
        table.AddColumn("Role");
        table.AddColumn("Name");
        table.AddColumn("Status");

        // Supplier
        table.AddRow("🚚 Supplier", mgr.Supplier.Name, mgr.Supplier.CurrentStatus.ToMarkup());
        table.AddEmptyRow();

        // Chefs
        foreach (var chef in mgr.Chefs)
        {
            var ingIcon = chef.Ingredient switch {
                Config.Enums.IngredientType.Tomato => "🍅",
                Config.Enums.IngredientType.Cheese => "🧀",
                _ => "🌶️"
            };
            table.AddRow($"👨‍🍳 Chef ({ingIcon})", chef.Name, chef.CurrentStatus.ToMarkup());
        }

        var (t, c, ch) = mgr.Kitchen.GetState();
        var boardStr = $"[{(t?"red":"grey")}]🍅[/] [{(c?"yellow":"grey")}]🧀[/] [{(ch?"red":"grey")}]🌶️[/]";

        var grid = new Grid().AddColumn();
        grid.AddRow(table);
        grid.AddRow(new Rule($"[white]Table Ingredients: {boardStr}[/]"));

        return new Panel(grid).Header("🔪 Kitchen").BorderColor(Color.Red).Expand();
    }

    private static Panel RenderService(CanteenManager mgr)
    {
        var grid = new Grid().AddColumn();
        
        // Waitresses
        foreach(var w in mgr.Waitresses)
        {
            grid.AddRow(new Markup($"[magenta]💁‍♀️ {w.Name}[/]: {w.CurrentStatus.ToMarkup()}"));
        }
        
        grid.AddRow(new Rule());

        // Buffets
        var passChart = new BarChart()
            .Label("Kitchen Pass")
            .AddItem("Meals", mgr.Counter.MealsReady, Color.Yellow)
            .WithMaxValue(3);
            
        var buffetChart = new BarChart()
            .Label("Main Buffet")
            .AddItem("Meals", mgr.Counter.MealsReady, Color.Green)
            .WithMaxValue(10);

        grid.AddRow(new Columns(passChart, buffetChart));

        return new Panel(grid).Header("🔔 Service").BorderColor(Color.Yellow).Expand();
    }

    private static Panel RenderDining(CanteenManager mgr)
    {
        // Wyświetlamy studentów w formie siatki
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("Student");
        table.AddColumn("Action");
        
        // Filtrujemy tylko tych, którzy już weszli (nie są w kolejce)
        var seatedStudents = mgr.Students.Where(s => !mgr.Host.EntranceQueue.Contains(s)).ToList();

        if (!seatedStudents.Any())
        {
            return new Panel(Align.Center(new Markup("[grey]Empty Dining Hall[/]"), VerticalAlignment.Middle))
                .Header("Dining Area").BorderColor(Color.Blue).Expand();
        }

        foreach (var s in seatedStudents)
        {
            table.AddRow(s.Name, s.CurrentStatus.ToMarkup());
        }

        return new Panel(table).Header("🍝 Dining Area").BorderColor(Color.Blue).Expand();
    }

    private static Panel RenderEntrance(CanteenManager mgr)
    {
        var grid = new Grid().AddColumn();
        grid.AddRow($"[bold]Host Status:[/] {mgr.Host.CurrentStatus.ToMarkup()}");
        
        var qCount = mgr.Host.EntranceQueue.Count;
        var bar = "";
        for(int i=0; i<5; i++) bar += i < qCount ? "[red]👤[/]" : "[grey]_[/]";
        
        grid.AddRow(new Markup($"Queue: {bar}"));
        grid.AddRow($"Occupancy: {8 - mgr.Host.CurrentOccupancy}/8"); // CurrentCount to wolne miejsca

        return new Panel(grid).Header("🚪 Entrance").BorderColor(Color.Green).Expand();
    }

    private static Panel RenderLogs(CanteenManager mgr)
    {
        var text = string.Join("\n", mgr.Logs.TakeLast(10));
        return new Panel(new Markup(text)).Header("📜 Logs").BorderColor(Color.Grey).Expand();
    }
}