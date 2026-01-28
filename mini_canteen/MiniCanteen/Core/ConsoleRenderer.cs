using MiniCanteen.Config;
using MiniCanteen.Config.Assets;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models;
using MiniCanteen.Models.Areas.Kitchen;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace MiniCanteen.Core;

public static class ConsoleRenderer
{
    public static IRenderable CreateLayout(CanteenState state) => new Grid();

    public static Grid UpdateView(CanteenState state)
    {
        var mainGrid = new Grid();
        mainGrid.AddColumn();
        
        var topGrid = new Grid();
        topGrid.AddColumn();
        topGrid.AddColumn();
        
        topGrid.AddRow(
            RenderKitchen(state.Kitchen), 
            RenderService(state.ServiceArea)
        );
        
        var bottomGrid = new Grid();
        bottomGrid.AddColumn();
        bottomGrid.AddColumn();
        bottomGrid.AddColumn();
        
        bottomGrid.AddRow(
            RenderDining(state.DiningArea),
            RenderEntrance(state.Entrance),
            RenderLogs(state)
        );
        
        mainGrid.AddRow(topGrid);
        mainGrid.AddRow(bottomGrid);

        return mainGrid;
    }

    private static Panel RenderKitchen(Kitchen kitchen)
    {
        var table = new Spectre.Console.Table().Border(TableBorder.None).HideHeaders().Expand();
        table.AddColumn("Chef");
        table.AddColumn("Status");
        table.AddColumn("Pot");

        foreach (var chef in kitchen.Chefs)
        {
            table.AddRow(
                $"[bold]{chef.Name}[/]", 
                Formatter.FormatChefStatus(chef.Status),
                Formatter.GetIngredientIcon(chef.Ingredient)
            );
        }
        
        var ingContent = Formatter.FormatIngredient(Icons.TomatoIcon, kitchen.DroppedTomato, "red") + " " +
                         Formatter.FormatIngredient(Icons.CheeseIcon, kitchen.DroppedCheese, "yellow") + " " +
                         Formatter.FormatIngredient(Icons.ChiliIcon, kitchen.DroppedChili, "red");
        
        var content = new Grid().AddColumn();
        content.AddRow(table);
        content.AddRow(new Rule($"[grey]Table: {ingContent}[/]").LeftJustified());

        return new Panel(content)
            .Header("🔪 KITCHEN")
            .BorderColor(Color.Red)
            .Expand();
    }

    private static Panel RenderService(Models.Areas.ServiceArea.ServiceArea service)
    {
        var bar = new BarChart()
            .Width(30)
            .Label("Food Buffer")
            .CenterLabel()
            .AddItem("Food Count", service.CurrentFoodCount, Color.Yellow)
            .AddItem("Free Space", service.SpaceLeft, Color.Grey);
        
        var content = new Padder(bar, new Padding(2, 1));

        return new Panel(content)
            .Header("🔔 SERVICE AREA")
            .BorderColor(Color.Yellow)
            .Expand();
    }

    private static Panel RenderDining(Models.Areas.DiningArea.DiningArea dining)
    {
        var grid = new Grid().AddColumn().AddColumn();
        grid.Expand();
        
        foreach (var table in dining.Tables)
        {
            var tObj = new Spectre.Console.Table()
                .Title($"[blue]{table.Name}[/]")
                .Border(TableBorder.Rounded)
                .Expand();
                
            tObj.AddColumn("Student");
            tObj.AddColumn("State");

            foreach (var s in table.Students)
            {
                var color = s.State == "Eating" ? "green" : (s.State == "Waiting" ? "yellow" : "grey");
                tObj.AddRow(s.Name, $"[{color}]{s.Icon} {s.State}[/]");
            }
            grid.AddRow(tObj);
        }

        return new Panel(grid)
            .Header("🍝 DINING AREA")
            .BorderColor(Color.Blue)
            .Expand();
    }

    private static Panel RenderEntrance(Models.Areas.Entrance.Entrance entrance)
    {
        var queueBar = Formatter.CreateProgressBar(entrance.WaitingCount, 5, "green");
        
        var content = new Grid().AddColumn();
        content.AddRow(new Markup($"[bold]Host Status:[/] {entrance.HostStatus}"));
        content.AddRow(new Rule());
        content.AddRow(new Markup($"[bold]Queue:[/] {queueBar}"));
        content.AddRow(new Markup($"[dim]Waiting: {entrance.WaitingCount}/4[/]"));
        content.AddRow(new Rule());
        content.AddRow(new Markup($"[green]Served: {entrance.TotalSeated}[/]"));
        content.AddRow(new Markup($"[red]Walked Away:  {entrance.TotalWalkedAway}[/]"));

        return new Panel(content)
            .Header("🚪 ENTRANCE")
            .BorderColor(Color.Green)
            .Expand();
    }

    private static Panel RenderLogs(CanteenState state)
    {
        var logs = state.SystemLog.ToArray();
        var text = string.Join("\n", logs);
        
        return new Panel(new Markup(text).Overflow(Overflow.Ellipsis))
            .Header("📜 LOGS")
            .BorderColor(Color.Grey)
            .Expand();
    }
}