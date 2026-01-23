using Spectre.Console;
using Spectre.Console.Rendering;

using MiniCanteen.Config;
using MiniCanteen.Config.Assets;

using MiniCanteen.Models;
using MiniCanteen.Models.Areas.DiningArea;
using MiniCanteen.Models.Areas.Entrance;
using MiniCanteen.Models.Areas.Kitchen;
using MiniCanteen.Models.Areas.ServiceArea;

namespace MiniCanteen.UI;

public static class ConsoleRenderer
{
    public static void ShowDashboard(CanteenState state)
    {
        
        // --- 1. HEADER (System Stats) ---
            var headerGrid = new Grid().AddColumns(4);
            headerGrid.AddRow(
                $"[bold {Theme.BorderKitchen}]MiNI Canteen[/]",
                $"[{Theme.TextGeneric}]uptime:[/] [bold]{state.SystemLog.Count * 100000}s[/]"
            );

            // --- 2. LEFT COLUMN MODULES ---

            // MODULE: KITCHEN (Pizza Station)
            // Visualizes the "Cigarette Smokers" problem logic
            var kitchen = state.Kitchen;

            // Render the "Memory Blocks" (Ingredients on Table)
            var ingredientBar = "";
            ingredientBar += Formatter.FormatIngredient(Icons.TomatoIcon, kitchen.DroppedTomato, Theme.TextGeneric);
            ingredientBar += Formatter.FormatIngredient(Icons.CheeseIcon,   kitchen.DroppedCheese,   Theme.TextGeneric);
            ingredientBar += Formatter.FormatIngredient(Icons.ChiliIcon,    kitchen.DroppedChili,    Theme.TextGeneric);

            var kitchenTable = new Spectre.Console.Table().Border(TableBorder.None).HideHeaders().Expand();
            kitchenTable.AddColumn("Item");
            kitchenTable.AddColumn("Status");

            kitchenTable.AddRow($"[{Theme.HeaderKitchen}]Table Items[/]", ingredientBar);

            // Render Chef Rows Dynamically from the list
            foreach(var chef in kitchen.Chefs)
            {
                var infiniteIcon = Formatter.GetIngredientIcon(chef.Ingredient);
                
                var chefLabel = $"[{Theme.TextGeneric}]{Icons.ChefProfile} {chef.Name} (Has {infiniteIcon})[/]";
                var status = Formatter.FormatChefStatus(chef.Status);
                
                kitchenTable.AddRow(chefLabel, status);
            }

            var kitchenPanel = new Panel(kitchenTable)
                .Header($"[bold {Theme.BorderKitchen}] cpu0 : Pizza Station [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Expand();


            // MODULE: ENTRANCE (Sleeping Barber)
            var entrance = state.Entrance;
            var queueBar = Formatter.CreateProgressBar(entrance.WaitingCount, 4, Theme.BorderKitchen);
            
            var entranceGrid = new Grid().AddColumns(2);
            entranceGrid.AddRow($"[{Theme.HeaderEntrance}]Host Proc:[/]", $"[bold {Theme.TextAction}]{entrance.HostStatus}[/]");
            entranceGrid.AddRow($"[{Theme.HeaderEntrance}]Queue Use:[/]", queueBar + $" [{Theme.TextGeneric}]({entrance.WaitingCount}/4)[/]");
            entranceGrid.AddRow($"[{Theme.HeaderEntrance}]Balking:[/]",   $"[bold {Theme.TextFail}]{entrance.TotalWalkedAway}[/]");
            
            var entrancePanel = new Panel(entranceGrid)
                .Header($"[bold {Theme.BorderKitchen}] mem : Entrance [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Expand();


            // --- 3. RIGHT COLUMN MODULES ---

            // MODULE: THE PASS (Producer-Consumer Channel)
            var bufCount = state.ServiceArea.FoodBuffer.Reader.Count;
            var serviceBar = Formatter.CreateProgressBar(bufCount, 5, "orchid1");

            var serviceGrid = new Grid().AddColumns(2);
            serviceGrid.AddRow($"[{Theme.HeaderService}]Buffer I/O:[/]", serviceBar + $" [{Theme.TextGeneric}]({bufCount}/5)[/]");
            serviceGrid.AddRow($"[{Theme.HeaderService}]Total Tx:[/]",   $"[bold]{state.ServiceArea.TotalDelivered}[/]");
            // Hardcoded waiter strings for now (can be refactored to objects later)
            serviceGrid.AddRow($"[{Theme.HeaderService}]Jeeves:[/]",     state.ServiceArea.WaiterGabrielaState.Replace("Idle", $"[{Theme.TextGeneric}]idle[/]"));
            serviceGrid.AddRow($"[{Theme.HeaderService}]Alice:[/]",      state.ServiceArea.WaiterSofiaState.Replace("Idle", $"[{Theme.TextGeneric}]idle[/]"));

            var passPanel = new Panel(serviceGrid)
                .Header($"[bold {Theme.BorderKitchen}] net : The Pass [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Expand();


            // MODULE: DINING ROOM (Dining Philosophers)
            var diningTable = new Spectre.Console.Table().Border(TableBorder.Simple).Expand();
            diningTable.AddColumn($"[{Theme.TextGeneric}]PID[/]");     // Name
            diningTable.AddColumn($"[{Theme.TextGeneric}]State[/]");   // Icon
            diningTable.AddColumn($"[{Theme.TextGeneric}]L-Res[/]");   // Left Fork
            diningTable.AddColumn($"[{Theme.TextGeneric}]R-Res[/]");   // Right Fork

            void AddStudentRow(Student p)
            {
                var lFork = Monitor.IsEntered(p.LeftFork) ? $"[{Theme.TextWarning}]LOCK[/]" : $"[{Theme.TextGeneric}]FREE[/]";
                var rFork = Monitor.IsEntered(p.RightFork) ? $"[{Theme.TextWarning}]LOCK[/]" : $"[{Theme.TextGeneric}]FREE[/]";
                
                var stateColor = p.Icon.Contains("😋") ? Theme.TextAction : (p.Icon.Contains("🤤") ? Theme.TextIdle : Theme.HeaderGeneric);
                
                diningTable.AddRow(
                    $"[{Theme.HeaderDining}]{p.Name}[/]", 
                    $"[{stateColor}]{p.Icon}[/]", 
                    lFork, 
                    rFork
                );
            }

            foreach(var p in state.DiningArea.Tables[0].Students) AddStudentRow(p);
            diningTable.AddRow($"[{Theme.TextGeneric}]──────[/]", "", "", ""); 
            foreach(var p in state.DiningArea.Tables[1].Students) AddStudentRow(p);

            var diningPanel = new Panel(diningTable)
                .Header($"[bold {Theme.BorderKitchen}] proc : Dining [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Expand();


            // --- 4. TERMINAL LOGS ---
            // Only show last 5 lines to keep layout stable
            var logContent = string.Join("\n", state.SystemLog.TakeLast(5));
            var logPanel = new Panel(new Markup(logContent))
                .Header($"[bold {Theme.TextGeneric}] term : logs [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey39)
                .Expand();


            // --- LAYOUT COMPOSITION ---
            var mainGrid = new Grid();
            mainGrid.AddColumn(); // Left Col
            mainGrid.AddColumn(); // Right Col

            var leftStack = new Grid().AddColumn();
            leftStack.AddRow(kitchenPanel);
            leftStack.AddRow(entrancePanel);

            var rightStack = new Grid().AddColumn();
            rightStack.AddRow(passPanel);
            rightStack.AddRow(diningPanel);

            mainGrid.AddRow(leftStack, rightStack);

            // Render Rules & Grid
            var headerRule = new Rule { Style = Style.Parse(Theme.BorderKitchen) };
            
            AnsiConsole.Write(headerRule);
            AnsiConsole.Write(headerGrid);
            AnsiConsole.Write(new Rule { Style = Style.Parse(Theme.TextGeneric) });
            AnsiConsole.Write(mainGrid);
            AnsiConsole.Write(logPanel);
    }

    private static string GetChefColor(string state) => state.Contains("COOKING") ? $"[green]{state}[/]" : $"[grey]{state}[/]";

    private static Panel RenderTable(MiniCanteen.Models.Areas.DiningArea.Table t)
    {
        var g = new Grid().AddColumns(3);
        g.AddRow(new Text(""), new Markup($"{t.Students[0].Icon} {t.Students[0].Name}"), new Text(""));
        g.AddRow(new Markup(IsForkTaken(t.Forks[3]) ? "❌" : "󰒤"), new Text(""), new Markup(IsForkTaken(t.Forks[1]) ? "❌" : "󰒤"));
        g.AddRow(new Markup($"{t.Students[3].Icon} {t.Students[3].Name}"), new Markup($"[bold]{t.Name.Substring(0,7)}[/]"), new Markup($"{t.Students[1].Icon} {t.Students[1].Name}"));
        g.AddRow(new Markup(IsForkTaken(t.Forks[2]) ? "❌" : "󰒤"), new Text(""), new Markup(IsForkTaken(t.Forks[0]) ? "❌" : "󰒤"));
        g.AddRow(new Text(""), new Markup($"{t.Students[2].Icon} {t.Students[2].Name}"), new Text(""));
        return new Panel(g)
        {
            Border = BoxBorder.None,
        };
    }

    private static bool IsForkTaken(object fork) => Monitor.IsEntered(fork);
}