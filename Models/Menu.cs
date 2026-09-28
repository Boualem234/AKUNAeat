using AKUNAeat.Interfaces;

namespace AKUNAeat.Models
{
    public class Menu : Meal
    {
        private string description;

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private List<Dish> dishes;

        public List<Dish> Dishes
        {
            get { return dishes; }
            set { dishes = value; }
        }

        public Menu() 
        {
            dishes = new List<Dish>();
        }

        public Menu(int meal_id, string name, decimal price, Restaurant r, string description, Dish dish) : base(meal_id, name, price, r)
        {
            Description = description;
            Dishes = new List<Dish>();
            AddDish(dish);
        }

        public void AddDish(Dish dish)
        {
            if (dish != null)
            {
                Dishes.Add(dish);
            }
        }


        public static async Task<Menu?> GetMenuByIdAsync(int mealId, IMealDAL dal)
        {
            return await dal.GetMenuByIdAsync(mealId);
        }

        public static async Task<List<Menu>> GetMenusByRestaurantIdAsync(int restaurantId, IMealDAL dal)
        {
            return await dal.GetMenusByRestaurantIdAsync(restaurantId);
        }

        public async Task<bool> DeleteMenuAsync(int restaurantId, IMealDAL dal)
        {
            return await dal.DeleteMenuAsync(this.MealId, restaurantId);
        }

        public async Task<(bool, int)> AddMenuAsync(int restaurantId, IMealDAL dal)
        {
            return await dal.InsertMenuAsync(this, restaurantId);
        }

        public async Task<bool> InsertMenuDishesAsync(int menuId, IMealDAL dal)
        {
            return await dal.InsertMenuDishesAsync(this, menuId);
        }

        public async Task<bool> ModifyMenuAsync(IMealDAL dal)
        {
            return await dal.ModifyMenuAsync(this);
        }

    }
}
