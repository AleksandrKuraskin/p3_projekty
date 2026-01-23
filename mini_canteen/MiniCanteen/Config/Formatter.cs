using MiniCanteen.Config.Assets;
using MiniCanteen.Config.Enums;

namespace MiniCanteen.Config;

public class Formatter
{
    public static string FormatIngredient(string icon, bool hasItem, string bgColor)
    {
        return hasItem 
            ? $"[bold] {icon} [/] " 
            : $"[{Theme.TextGeneric}] {icon} [/] ";
    }
    
    public static string FormatChefStatus(ChefStatus state)
    {
        return state switch
        {
            ChefStatus.Cooking => $"[bold {Theme.TextAction}]{Icons.ChefCooking} Cooking[/]",
            ChefStatus.Idle => $"[{Theme.TextIdle}]{Icons.ChefIdle} Sleeping[/]",
            ChefStatus.Waiting => $"[{Theme.TextIdle}]{Icons.ChefWaiting} Waiting[/]",
            _ => state.ToString()
        };
    }

    public static string GetIngredientIcon(IngredientType type)
    {
        return type switch
        {
            IngredientType.Tomato => Icons.TomatoIcon,
            IngredientType.Cheese => Icons.CheeseIcon,
            IngredientType.Chili => Icons.ChiliIcon,
            _ => "?"
        };
    }

    public static string CreateProgressBar(int value, int max, string color)
    {
        var bars = "";
        for (var i = 0; i < max; i++)
        {
            bars += (i < value) ? $"[bold {color}]|[/]" : $"[{color}]-[/]";
        }
        return bars;
    }
}