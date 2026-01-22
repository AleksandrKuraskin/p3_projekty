namespace MiniCanteen.Models.Areas.Kitchen;

public class Chef
{
    public string Name { get; }
        public string InfiniteIngredient { get; } // Icon (e.g., 🍄)
        
        // What this chef needs to cook
        public bool NeedsMushrooms { get; }
        public bool NeedsCheese { get; }
        public bool NeedsChili { get; }

        // State
        public bool IsCooking { get; private set; }
        public string Status { get; private set; } = "💤 Sleeping";

        private readonly Kitchen _kitchen;
        private readonly ThePass _pass;

        public Chef(string name, string infiniteIcon, 
                    bool needsMushrooms, bool needsCheese, bool needsChili,
                    Kitchen kitchen, ThePass pass)
        {
            Name = name;
            InfiniteIngredient = infiniteIcon;
            NeedsMushrooms = needsMushrooms;
            NeedsCheese = needsCheese;
            NeedsChili = needsChili;
            _kitchen = kitchen;
            _pass = pass;
        }

        public void StartWork()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    // 1. Try to take ingredients from the table
                    // This is a fast check. If successful, ingredients are removed from table immediately.
                    bool success = _kitchen.TryTakeIngredients(NeedsMushrooms, NeedsCheese, NeedsChili);

                    if (success)
                    {
                        // 2. COOKING (The long blocking task)
                        IsCooking = true;
                        Status = "🍳 COOKING";
                        
                        // We simulate work here. 
                        // Note: The Supplier can now refill the table while we are in this delay!
                        await Task.Delay(10000); 

                        // 3. Deliver to Pass
                        await _pass.FoodBuffer.Writer.WriteAsync(1);
                        
                        // 4. Reset
                        IsCooking = false;
                        Status = "💤 Sleeping";
                    }
                    else
                    {
                        Status = "💤 Sleeping";
                        await Task.Delay(200); // Wait a bit before checking table again
                    }
                }
            });
        }
}