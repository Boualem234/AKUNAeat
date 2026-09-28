using AKUNAeat.Models;
using System.ComponentModel.DataAnnotations;

public class MenuDishViewModel
{
    public int MenuId { get; set; }
    public string Name { get; set; }

    public decimal Price { get; set; }

    public string Description { get; set; }

    public List<int> SelectedDishIds { get; set; }

    public MenuDishViewModel()
    {
        SelectedDishIds = new List<int>();
    }

    public MenuDishViewModel(Menu menu)
    {
        MenuId = menu.MealId;
        Name = menu.Name;
        Price = menu.Price;
        Description = menu.Description;
        SelectedDishIds = new List<int>();
    }

    public void AddDish(Dish dish)
    {
        if (!SelectedDishIds.Contains(dish.MealId))
        {
            SelectedDishIds.Add(dish.MealId);
        }
    }

}
