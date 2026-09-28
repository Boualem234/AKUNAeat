using AKUNAeat.Models;

namespace AKUNAeat.Interfaces
{
    public interface IMealDAL
    {
        Task<(bool, int)> InsertMenuAsync(Menu menu, int restaurantId);
        Task<bool> InsertMenuDishesAsync(Menu menu, int menuId);
        Task<bool> InsertDishAsync(Dish dish, int restaurantId);
        Task<List<Dish>> GetDishesOfRestaurantAsync(int restaurantId);
        Task<bool> DeleteMenuAsync(int menuId, int restaurantId);
        Task<List<Menu>> GetMenusByRestaurantIdAsync(int restaurantId);
        Task<bool> ModifyMenuAsync(Menu m);
        Task<Menu?> GetMenuByIdAsync(int menuId);
        Task<List<Meal>> GetMealsAsync(int restaurantId);

    }
}
