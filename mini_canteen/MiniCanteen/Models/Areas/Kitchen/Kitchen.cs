using MiniCanteen.Config.Enums;

namespace MiniCanteen.Models.Areas.Kitchen;

public class KitchenBoard
{
    private bool _hasTomato;
    private bool _hasCheese;
    private bool _hasChili;
    private readonly object _lock = new();

    private bool IsEmpty => !_hasTomato && !_hasCheese && !_hasChili;

    public void PlaceIngredients(IngredientType excluded)
    {
        lock (_lock)
        {
            while (!IsEmpty)
            {
                Monitor.Wait(_lock);
            }

            _hasTomato = excluded != IngredientType.Tomato;
            _hasCheese = excluded != IngredientType.Cheese;
            _hasChili = excluded != IngredientType.Chili;
            
            Monitor.PulseAll(_lock);
        }
    }
    
    public bool TryTakeIngredients(IngredientType chefHas)
    {
        lock (_lock)
        {
            if (IsEmpty) return false;

            var canCook = false;
            switch (chefHas)
            {
                case IngredientType.Tomato:
                    if (_hasCheese && _hasChili) canCook = true;
                    break;
                case IngredientType.Cheese:
                    if (_hasTomato && _hasChili) canCook = true;
                    break;
                case IngredientType.Chili:
                    if (_hasTomato && _hasCheese) canCook = true;
                    break;
                default:
                    break;
            }

            if (canCook)
            {
                _hasTomato = _hasCheese = _hasChili = false;
                Monitor.PulseAll(_lock);
                return true;
            }
            
            Monitor.Wait(_lock, 100); 
            return false;
        }
    }

    public (bool t, bool c, bool ch) GetState() 
    {
        lock(_lock) return (_hasTomato, _hasCheese, _hasChili);
    }
}