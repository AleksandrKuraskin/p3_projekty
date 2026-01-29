using MiniCanteen.Config;
using MiniCanteen.Config.Assets;
using MiniCanteen.Config.Enums;
using MiniCanteen.Models;
using MiniCanteen.Models.Areas.Kitchen;
using Spectre.Console;

namespace MiniCanteen.Core;

public static class ConsoleRenderer
{
    // Tworzymy strukturę Layoutu raz na początku
    public static Layout CreateLayout()
    {
        return new Layout("Root")
            .SplitRows(
                // GÓRA: Podział 1:1
                new Layout("Top").SplitColumns(
                    new Layout("Kitchen"),
                    new Layout("Service")
                ),
                // DÓŁ: Podział 3:1:1 (Dining : Entrance : Logs)
                new Layout("Bottom").SplitColumns(
                    new Layout("Dining").Ratio(3),
                    new Layout("Entrance").Ratio(1),
                    new Layout("Logs").Ratio(1)
                )
            );
    }

    // Aktualizujemy tylko zawartość (Update) istniejących okien
    public static void UpdateLayout(Layout layout, CanteenState state)
    {
        // 1. Kuchnia
        layout["Kitchen"].Update(RenderKitchen(state.Kitchen));
        
        // 2. Service
        layout["Service"].Update(RenderService(state.ServiceArea));
        
        // 3. Dining Area (Duże okno - Ratio 3)
        layout["Dining"].Update(RenderDining(state.DiningArea));
        
        // 4. Entrance (Małe okno - Ratio 1)
        layout["Entrance"].Update(RenderEntrance(state.Entrance));
        
        // 5. Logs (Małe okno - Ratio 1)
        layout["Logs"].Update(RenderLogs(state));
    }

    private static Panel RenderKitchen(Kitchen kitchen)
    {
        var table = new Spectre.Console.Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .Expand();
            
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

        var rows = new Grid().AddColumn();
        rows.AddRow(table);
        rows.AddRow(new Rule($"[grey]Stół: {ingContent}[/]").LeftJustified());

        return new Panel(rows)
            .Header("🔪 Kitchen")
            .BorderColor(Color.Red)
            .Expand();
    }

    private static Panel RenderService(Models.Areas.ServiceArea.ServiceArea service)
    {
        var bar = new BarChart()
            .Label("Bufet")
            .CenterLabel()
            .AddItem("Posiłki", service.CurrentFoodCount, Color.Yellow)
            .WithMaxValue(10);

        // Używamy Align.Center, żeby wykres był ładnie na środku
        return new Panel(Align.Center(bar, VerticalAlignment.Middle))
            .Header("🔔 Service Area")
            .BorderColor(Color.Yellow)
            .Expand();
    }

    private static Panel RenderDining(Models.Areas.DiningArea.DiningArea dining)
    {
        // Grid 2x2 dla stolików, żeby wykorzystać szerokość (Ratio 3)
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
            .Header("🍝 Dining Area")
            .BorderColor(Color.Blue)
            .Expand();
    }

    private static Panel RenderEntrance(Models.Areas.Entrance.Entrance entrance)
    {
        var queueBar = Formatter.CreateProgressBar(entrance.WaitingCount, 4, "green");
        
        var content = new Grid().AddColumn();
        content.AddRow(new Markup($"[bold]Host:[/] {entrance.HostStatus}"));
        content.AddRow(new Rule());
        content.AddRow(new Markup($"[dim]Kolejka ({entrance.WaitingCount}/4):[/]"));
        content.AddRow(new Markup($"{queueBar}"));
        content.AddRow(new Rule());
        content.AddRow(new Markup($"[green]Seated: {entrance.TotalSeated}[/]"));
        content.AddRow(new Markup($"[red]Left:   {entrance.TotalWalkedAway}[/]"));

        return new Panel(Align.Center(content, VerticalAlignment.Middle))
            .Header("🚪 Entrance")
            .BorderColor(Color.Green)
            .Expand();
    }

    private static Panel RenderLogs(CanteenState state)
    {
        // Logi biorą całą dostępną wysokość layoutu
        var logs = state.SystemLog.ToArray();
        // Bierzemy ostatnie 15 (Layout sam utnie co niepotrzebne)
        var visibleLogs = logs.TakeLast(20); 
        var text = string.Join("\n", visibleLogs);
        
        return new Panel(new Markup(text).Overflow(Overflow.Ellipsis))
            .Header("📜 Logs")
            .BorderColor(Color.Grey)
            .Expand();
    }
}