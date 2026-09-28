using AKUNAeat.Models;

namespace AKUNAeat.ViewModels
{

    public class MealSelectionViewModel
    {
        public List<MealQuantitySelection> Selections { get; set; }

        public bool IsValid()
        {
            bool success = false;

            foreach (var selection in Selections)
            {
                if (selection.Quantity > 0)
                {
                    success = true;
                }
            }
            return success;
        }

    }

    public class MealQuantitySelection
    {
        public Meal Meal { get; set; } = new Menu();
        public int Quantity { get; set; }
    }

}