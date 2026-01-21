using MiniCanteen.Models;
using Spectre.Console;

using MiniCanteen.Models.Areas.DiningArea;
using MiniCanteen.Models.Areas.Entrance;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;
using Spectre.Console.Rendering;

namespace MiniCanteen.UI;

public static class ConsoleRenderer
{
    public static void ShowDashboard(CanteenState state)
    {
        
        var headerRule = new Rule(){ Title = "[bold yellow]🎓 C.S. CONCURRENCY RESTAURANT[/]"};
        headerRule.Style = Style.Parse("yellow");
        
        var logText = string.Join("\n", state.SystemLog);
        var logPanel = new Panel(logText)
            {
                Border = BoxBorder.Rounded,
            }
            .Header("📜 SYSTEM EVENTS LOG")
            .Expand();
        
        var k = state.Kitchen;
        var ingredients = $"{(k.HasMushroom ? "🍄 Mushroom " : "")}{(k.HasCheese ? "🧀 Cheese " : "")}{(k.HasPepperoni ? " Pepperoni" : "")}";
        if (string.IsNullOrWhiteSpace(ingredients)) ingredients = "[grey]Empty[/]";

        var kitchenContent = new Markup(
            $"[bold]Ingredients on Table (Supplier):[/]\n{ingredients}\n\n" +
            "[bold]Chefs (Smokers Problem):[/]\n" +
            $"👨‍🍳 [bold]Chef Diego (Needs 🍅+🧀):[/]   {GetChefColor(k.ChefDiegoState)}\n" +
            $"👨‍🍳 [bold]Chef Leo (Needs 🍞+🧀):[/]   {GetChefColor(k.ChefLeoState)}\n" +
            $"👨‍🍳 [bold]Chef Mario (Needs 🍅+🍞):[/]  {GetChefColor(k.ChefMarioState)}"
        );
        var kitchenPanel = new Panel(kitchenContent).Header("👨‍🍳 KITCHEN").BorderColor(Color.Red).Expand();
        
        var bufferVis = "";
        var count = state.ServiceArea.FoodBuffer.Reader.Count;
        for(var i=0; i<5; i++) bufferVis += (i < count) ? "🍕" : "[grey]_[/]";

        var passContent = new Markup(
            $"[bold]Buffer (Channel):[/]\n{bufferVis} ({count}/5)\n\n" +
            "[bold]Waiters (Consumer):[/]\n" +
            $"🤵 [bold]Gabriela:[/] {state.ServiceArea.WaiterGabrielaState}\n" +
            $"💁‍♀️ [bold]Sofia:[/]  {state.ServiceArea.WaiterSofiaState}\n\n" +
            $"[bold]Stats:[/]\n📦 Delivered: {state.ServiceArea.TotalDelivered}"
        );
        var passPanel = new Panel(passContent).Header("🛎️ THE SERVICE AREA").BorderColor(Color.Yellow).Expand();
        
        var diningGrid = new Grid().AddColumn().AddColumn();
        diningGrid.AddRow(
            RenderTable(state.DiningArea.Tables[0]), 
            RenderTable(state.DiningArea.Tables[1])
        );
        var diningPanel = new Panel(diningGrid).Header("🍽️ DINING HALL").BorderColor(Color.Blue).Expand();
        
        var e = state.Entrance;
        var chairs = "🪑 ";
        for(var i=0; i<4; i++) chairs += (i < e.WaitingCount) ? "👤 " : "[grey]Empty [/]";

        var entranceContent = new Markup(
            $"[bold]Host Status:[/]\n{e.HostStatus}\n\n" +
            $"[bold]Waiting Queue:[/]\n{chairs}\n\n" +
            $"[bold]Metrics (Balking):[/]\n🛑 Walked Away: [red]{e.TotalWalkedAway}[/]\n✅ Seated: [green]{e.TotalSeated}[/]"
        );
        var entrancePanel = new Panel(entranceContent).Header("🚪 ENTRANCE").BorderColor(Color.White).Expand();
        
        var row1 = new Layout("Row1").SplitColumns(
            new Layout("Kitchen").Ratio(1).Update(kitchenPanel),
            new Layout("Pass").Ratio(1).Update(passPanel)
        );
        var row2 = new Layout("Row2").SplitColumns(
            new Layout("Dining").Ratio(1).Update(diningPanel),
            new Layout("Entrance").Ratio(1).Update(entrancePanel)
        );
        var mainLayout = new Layout("Root").SplitRows(
            new Layout("Logs").Ratio(1).Update(logPanel),
            new Layout("Middle").Ratio(3).Update(row1),
            new Layout("Bottom").Ratio(3).Update(row2)
        );

        AnsiConsole.Write(headerRule);
        AnsiConsole.Write(mainLayout);
    }

    private static string GetChefColor(string state) => state.Contains("COOKING") ? $"[green]{state}[/]" : $"[grey]{state}[/]";

    private static Panel RenderTable(MiniCanteen.Models.Areas.DiningArea.Table t)
    {
        var g = new Grid().AddColumns(3);
        g.AddRow(new Text(""), new Markup($"{t.Students[0].Icon} {t.Students[0].Name}"), new Text(""));
        g.AddRow(new Markup(IsForkTaken(t.Forks[3]) ? "❌" : "🔱"), new Text(""), new Markup(IsForkTaken(t.Forks[1]) ? "❌" : "🔱"));
        g.AddRow(new Markup($"{t.Students[3].Icon} {t.Students[3].Name}"), new Markup($"[bold]{t.Name.Substring(0,7)}[/]"), new Markup($"{t.Students[1].Icon} {t.Students[1].Name}"));
        g.AddRow(new Markup(IsForkTaken(t.Forks[2]) ? "❌" : "🔱"), new Text(""), new Markup(IsForkTaken(t.Forks[0]) ? "❌" : "🔱")); // Visual approximation
        g.AddRow(new Text(""), new Markup($"{t.Students[2].Icon} {t.Students[2].Name}"), new Text(""));
        return new Panel(g)
        {
            Border = BoxBorder.None,
        };
    }

    private static bool IsForkTaken(object fork) => Monitor.IsEntered(fork);
}