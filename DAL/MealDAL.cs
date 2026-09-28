using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AKUNAeat.DAL
{
    public class MealDAL : IMealDAL
    {
        private string connectionString;

        public MealDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<(bool, int)> InsertMenuAsync(Menu menu, int restaurantId)
        {
            int mealId = await InsertMealAsync(menu, restaurantId);
            if (mealId == 0) return (false, mealId);

            string query = @"INSERT INTO Menu (meal_id, description) 
                            VALUES (@meal_id, @description);";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@meal_id", mealId);
                cmd.Parameters.AddWithValue("@description", menu.Description);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return (res > 0, mealId);
            }
        }

        public async Task<bool> InsertMenuDishesAsync(Menu menu, int menuId)
        {
            string query = @"INSERT INTO Menu_Dish (menu_id, dish_id) 
                     VALUES (@menu_id, @dish_id);";

            List<Dish> selectedDishes = menu.Dishes;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (Dish dish in selectedDishes)
                {
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@menu_id", menuId);
                        cmd.Parameters.AddWithValue("@dish_id", dish.MealId);

                        int res = await cmd.ExecuteNonQueryAsync();

                        if (res <= 0)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }



        public async Task<bool> InsertDishAsync(Dish dish, int restaurantId)
        {
            int mealId = await InsertMealAsync(dish, restaurantId);
            if (mealId == 0) return false;

            string query = @"INSERT INTO Dish (meal_id,composition)
                     VALUES (@meal_id,@composition);";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@meal_id", mealId);
                cmd.Parameters.AddWithValue("@composition", dish.Composition);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return res > 0;
            }
        }

        private async Task<int> InsertMealAsync(Meal meal, int restaurantId)
        {
            int insertedId = 0;
            string query = @"INSERT INTO Meal (name, price, restaurant_id) OUTPUT INSERTED.meal_id VALUES (@name, @price, @restaurantId)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("name", meal.Name);
                cmd.Parameters.AddWithValue("price", meal.Price);
                cmd.Parameters.AddWithValue("restaurantId", restaurantId);

                await connection.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();
                insertedId = Convert.ToInt32(result);
            }

            return insertedId;
        }

        public async Task<List<Dish>> GetDishesOfRestaurantAsync(int restaurantId)
        {
            string query = @"SELECT * FROM Meal AS m JOIN Dish AS d ON m.meal_id = d.meal_id
                            WHERE restaurant_id = @restaurant_id";

            List<Dish> dishes = new List<Dish>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@restaurant_id", restaurantId);

                await connection.OpenAsync();

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int mealId = reader.GetInt32("meal_id");
                    string name = reader.GetString("name");
                    decimal price = reader.GetDecimal("price");
                    string composition = reader.GetString("composition");

                    Service s = new Service();
                    Restaurant restaurant = new Restaurant(restaurantId,"","","","","",TimeSpan.Zero,TimeSpan.Zero,s);

                    Dish dish = new Dish(mealId, name, price,restaurant, composition); 

                    dishes.Add(dish);
                }
            }

            return dishes;
        }


        public async Task<bool> DeleteMenuAsync(int menuId, int restaurantId)
        {
            if (menuId == 0) return false;

            string query = @"DELETE FROM Orders WHERE orderNumber IN 
                    (SELECT orderNumber FROM meal_orders WHERE meal_id = @menuId);
                    DELETE FROM Meal WHERE meal_id = @menuId;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@menuId", menuId);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }

        }

        public async Task<List<Menu>> GetMenusByRestaurantIdAsync(int restaurantId)
        {
            List<Menu> menus = new List<Menu>();

            string query = @"SELECT m.meal_id, m.description, ml.meal_id, ml.name, ml.price
                            FROM Meal ml
                            JOIN Menu m ON m.meal_id = ml.meal_id
                            WHERE ml.restaurant_id = @restaurant_id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@restaurant_id", restaurantId);

                await connection.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int mealId = reader.GetInt32("meal_id");
                        string name = reader.GetString("name");
                        decimal price = reader.GetDecimal("price");
                        string description = reader.GetString("description");

                        Service s = new Service();
                        Restaurant restaurant = new Restaurant(restaurantId, "", "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, s);
                        Dish dish = new Dish();

                        Menu menu = new Menu(mealId, name, price,restaurant, description, dish);

                        menus.Add(menu);
                    }
                }
            }

            return menus;
        }

        public async Task<bool> ModifyMenuAsync(Menu m)
        {
            if (m.MealId == 0) return false;
            bool modifyMealIsOk = await ModifyMealAsync(m);

            if (!modifyMealIsOk) return false;

            string query = @"UPDATE Menu
                SET description = @description
                WHERE meal_id = @menuId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@menuId", m.MealId);
                cmd.Parameters.AddWithValue("@description", m.Description);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                if (res == 0) return false;
            }
            bool modifyMenuDishesIsOk = await ModifyMenuDishesAsync(m);
            if (!modifyMenuDishesIsOk) return false;

            return true;
        }

        private async Task<bool> ModifyMealAsync(Menu m)
        {
            string query = @"UPDATE Meal
                SET name = @name, price = @price
                WHERE meal_id = @menuId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@menuId", m.MealId);
                cmd.Parameters.AddWithValue("@name", m.Name);
                cmd.Parameters.AddWithValue("@price", m.Price);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return res > 0;
            }
        }

        private async Task<bool> ModifyMenuDishesAsync(Menu menu)
        {
            string deleteQuery = @"DELETE FROM Menu_Dish WHERE menu_id = @menu_id;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, connection))
                {
                    deleteCmd.Parameters.AddWithValue("@menu_id", menu.MealId);
                    int deleteRes = await deleteCmd.ExecuteNonQueryAsync();
                }
            }
            bool insertResult = await InsertMenuDishesAsync(menu, menu.MealId);
            if (!insertResult) return false;

            return true;
        }


        public async Task<Menu?> GetMenuByIdAsync(int menuId)
        {
            string query = @"SELECT m.meal_id, m.name, m.price, mn.description, m.restaurant_id
                            FROM Meal m
                            JOIN Menu mn ON m.meal_id = mn.meal_id
                            WHERE m.meal_id = @menuId";

            Menu? menu = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@menuId", menuId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {

                            int mealId = reader.GetInt32("meal_id");
                            string name = reader.GetString("name");
                            decimal price = reader.GetDecimal("price");
                            string description = reader.GetString("description");
                            int restaurantId = reader.GetInt32("restaurant_id");

                            Service s = new Service();
                            Restaurant restaurant = new Restaurant(restaurantId, "", "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, s);
                            Dish dish = new Dish();

                            menu = new Menu(mealId, name, price, restaurant, description, dish);
                        }
                    }
                }

                if (menu != null)
                {
                    menu.Dishes = await GetDishesForMenuAsync(menuId);
                }
            }

            return menu;

        }

        private async Task<List<Dish>> GetDishesForMenuAsync(int menuId)
        {
            string query = @"SELECT d.meal_id,m.name,d.composition,m.price,m.restaurant_id FROM Dish d
                     JOIN Menu_Dish md ON d.meal_id = md.dish_id
                     JOIN Meal m ON d.meal_id = m.meal_id
                     WHERE md.menu_id = @menuId";

            List<Dish> dishes = new List<Dish>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@menuId", menuId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int mealId = reader.GetInt32("meal_id");
                        string name = reader.GetString("name");
                        decimal price = reader.GetDecimal("price");
                        string composition = reader.GetString("composition");

                        int restaurantId = reader.GetInt32("restaurant_id");

                        Restaurant restaurant = new Restaurant(restaurantId, "", "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, new Service());

                        Dish dish = new Dish(mealId, name, price,restaurant, composition);

                        dishes.Add(dish);
                    }
                }
            }

            return dishes;
        }

        public async Task<List<Meal>> GetMealsAsync(int restaurantId)
        {
            var meals = new List<Meal>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string menuQuery = @"SELECT* FROM Meal ml JOIN Menu m ON m.meal_id = ml.meal_id
                                    WHERE ml.restaurant_id = @restaurant_id";

                using (SqlCommand menuCmd = new SqlCommand(menuQuery, connection))
                {
                    menuCmd.Parameters.AddWithValue("@restaurant_id", restaurantId);

                    using (SqlDataReader reader = await menuCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int mealId = reader.GetInt32(reader.GetOrdinal("meal_id"));
                            string name = reader.GetString(reader.GetOrdinal("name"));
                            decimal price = reader.GetDecimal(reader.GetOrdinal("price"));
                            string description = reader.GetString(reader.GetOrdinal("description"));

                            Restaurant restaurant = new Restaurant(restaurantId, "", "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, new Service());
                            Menu menu = new Menu(mealId, name, price, restaurant,description,null);

                            meals.Add(menu);
                        }
                    }
                }

                string dishQuery = @"SELECT * FROM Meal m JOIN Dish d ON d.meal_id = m.meal_id
                                    WHERE m.restaurant_id = @restaurant_id;";

                using (SqlCommand dishCmd = new SqlCommand(dishQuery, connection))
                {
                    dishCmd.Parameters.AddWithValue("@restaurant_id", restaurantId);

                    using (SqlDataReader reader = await dishCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int mealId = reader.GetInt32(reader.GetOrdinal("meal_id"));
                            string name = reader.GetString(reader.GetOrdinal("name"));
                            decimal price = reader.GetDecimal(reader.GetOrdinal("price"));
                            string composition = reader.GetString(reader.GetOrdinal("composition"));

                            Restaurant restaurant = new Restaurant(restaurantId, "", "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, new Service());
                            Dish dish = new Dish(mealId, name, price, restaurant, composition);

                            meals.Add(dish);
                        }
                    }
                }
            }

            return meals;
        }

    }


}
