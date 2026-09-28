using AKUNAeat.Models;
using AKUNAeat.ViewModels;
using Microsoft.Data.SqlClient;
using AKUNAeat.Interfaces;
using System.Data;


namespace AKUNAeat.DAL
{
    public class OrderDAL : IOrderDAL
    {
        private string connectionString;

        public OrderDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<bool> InsertOrderAsync(Order order)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string insertOrderQuery = @"INSERT INTO Orders 
                (restaurant_id, account_id, orderTime, isDelivery, status) 
                OUTPUT INSERTED.orderNumber VALUES (@restaurantId, @customerId, @orderTime, @isDelivery, @status);";

                SqlCommand cmd = new SqlCommand(insertOrderQuery, connection);
                cmd.Parameters.AddWithValue("@restaurantId", order.Restaurant.RestaurantId);
                cmd.Parameters.AddWithValue("@customerId", order.Customer.AccountId);
                cmd.Parameters.AddWithValue("@orderTime", order.OrderTime);
                cmd.Parameters.AddWithValue("@isDelivery", order.IsDelivery);
                cmd.Parameters.AddWithValue("@status", order.Status.ToString());

                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    int insertedOrderNumber = Convert.ToInt32(result);

                    if (!await InsertOrderMealsAsync(insertedOrderNumber, order.Meals))
                    {
                        return false;
                    }
                    if (!await InsertOrderServicesAsync(insertedOrderNumber, order.Service))
                    {
                        return false;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<bool> InsertOrderMealsAsync(int orderNumber, Dictionary<Meal,int> selectedMeals)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO meal_orders (orderNumber, meal_id, quantity) VALUES (@OrderNumber, @MealId, @Quantity)";

                foreach (var selectMeal in selectedMeals)
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OrderNumber", orderNumber);
                        command.Parameters.AddWithValue("@MealId", selectMeal.Key.MealId);
                        command.Parameters.AddWithValue("@Quantity", selectMeal.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private async Task<bool> InsertOrderServicesAsync(int orderNumber, Service s)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO orders_service (orderNumber, service_id) VALUES (@OrderNumber, @ServiceId)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@OrderNumber", orderNumber);
                        command.Parameters.AddWithValue("@ServiceId", s.ServiceId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return false;
                        }
                    }
            }

            return true;
        }

        public async Task<List<Order>> GetOrdersWithServiceByRestaurantIdAsync(int restaurantId)
        {
            string query = @"
                SELECT 
                o.orderNumber, o.status, o.isDelivery, o.orderTime,
                a.account_id, a.firstName, a.lastName, ca.address AS deliveryAddress,
                s.service_id, s.startTime, s.endTime, s.serviceType,
                r.restaurant_id, r.name AS restaurantName
                FROM Orders o
                JOIN CustomerAccount ca ON o.account_id = ca.account_id
                JOIN Account a ON ca.account_id = a.account_id
                JOIN orders_service os ON o.orderNumber = os.orderNumber
                JOIN Service s ON os.service_id = s.service_id
                JOIN Restaurant r ON o.restaurant_id = r.restaurant_id
                WHERE o.restaurant_id = @RestaurantId
                ORDER BY o.orderTime DESC";

            List<Order> orders = new List<Order>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@RestaurantId", restaurantId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int customerId = reader.GetInt32("account_id");
                        string firstName = reader.GetString("firstName");
                        string lastName = reader.GetString("lastName");
                        CustomerAccount customer = new CustomerAccount(customerId, firstName, lastName, "", "", "");

                        int serviceId = reader.GetInt32(reader.GetOrdinal("service_id"));
                        TimeSpan startTime = reader.GetTimeSpan(reader.GetOrdinal("startTime"));
                        TimeSpan endTime = reader.GetTimeSpan(reader.GetOrdinal("endTime"));
                        ServiceType serviceType = Enum.Parse<ServiceType>(reader.GetString(reader.GetOrdinal("serviceType")));
                        Service service = new Service(serviceId, startTime, endTime, serviceType);

                        int restId = reader.GetInt32(reader.GetOrdinal("restaurant_id"));
                        string restName = reader.GetString(reader.GetOrdinal("restaurantName"));
                        Restaurant restaurant = new Restaurant(restId, restName, "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, null);

                        int orderNumber = reader.GetInt32("orderNumber");
                        Status status = Enum.Parse<Status>(reader.GetString("status"));
                        bool isDelivery = reader.GetBoolean("isDelivery");
                        DateTime orderTime = reader.GetDateTime("orderTime");

                        Dictionary<Meal, int> meals = await GetMealsByOrderNumberAsync(orderNumber, restaurant);
                        Order order = new Order(orderNumber, status, orderTime, isDelivery, service, customer, meals, restaurant);

                        orders.Add(order);
                    }
                }
            }

            return orders;
        }


        public async Task<Dictionary<Meal, int>> GetMealsByOrderNumberAsync(int orderNumber, Restaurant r)
        {
            var mealsWithQuantities = new Dictionary<Meal, int>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string dishQuery = @"
                    SELECT m.meal_id, m.name, m.price, mo.quantity, d.composition
                    FROM Meal m
                    JOIN Dish d ON d.meal_id = m.meal_id
                    JOIN meal_orders mo ON mo.meal_id = m.meal_id
                    WHERE mo.orderNumber = @OrderNumber;";

                using (SqlCommand cmd = new SqlCommand(dishQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32("meal_id");
                            string name = reader.GetString("name");
                            decimal price = reader.GetDecimal("price");
                            int quantity = reader.GetInt32("quantity");
                            string composition = reader.GetString("composition");

                            var dish = new Dish(id, name, price, r, composition);
                            mealsWithQuantities[dish] = quantity;
                        }
                    }
                }

                string menuQuery = @"
                    SELECT m.meal_id, m.name, m.price, mo.quantity, me.description
                    FROM Meal m
                    JOIN Menu me ON me.meal_id = m.meal_id
                    JOIN meal_orders mo ON mo.meal_id = m.meal_id
                    WHERE mo.orderNumber = @OrderNumber;";

                using (SqlCommand cmd = new SqlCommand(menuQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32("meal_id");
                            string name = reader.GetString("name");
                            decimal price = reader.GetDecimal("price");
                            int quantity = reader.GetInt32("quantity");
                            string description = reader.GetString("description");

                            var menu = new Menu(id, name, price, r, description, null);
                            mealsWithQuantities[menu] = quantity;
                        }
                    }
                }
            }

            return mealsWithQuantities;
        }


        public async Task<bool> UpdateOrderStatusAsync(Order order, Status newStatus)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Orders SET status = @Status WHERE orderNumber = @OrderNumber";

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Status", newStatus.ToString());
                cmd.Parameters.AddWithValue("@OrderNumber", order.OrderNumber);

                await connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
        }

        public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            string query = @" SELECT o.orderNumber, o.status, o.isDelivery, o.orderTime,
               r.name AS restaurantName, r.restaurant_id,
               s.service_id, s.startTime, s.endTime, s.serviceType
                FROM Orders o
                JOIN Restaurant r ON o.restaurant_id = r.restaurant_id
                JOIN orders_service os ON o.orderNumber = os.orderNumber
                JOIN Service s ON os.service_id = s.service_id
                WHERE o.account_id = @CustomerId
                ORDER BY o.orderTime DESC";

            List<Order> orders = new List<Order>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int orderNumber = reader.GetInt32("orderNumber");
                        Status status = Enum.Parse<Status>(reader.GetString("status"));
                        bool isDelivery = reader.GetBoolean("isDelivery");
                        DateTime orderTime = reader.GetDateTime("orderTime");

                        int service_id = reader.GetInt32("service_id");
                        TimeSpan startTime = reader.GetTimeSpan(reader.GetOrdinal("startTime"));
                        TimeSpan endTime = reader.GetTimeSpan(reader.GetOrdinal("endTime"));
                        ServiceType serviceType = Enum.Parse<ServiceType>(reader.GetString("serviceType"));

                        int restaurant_id = reader.GetInt32("restaurant_id");
                        string restaurantName = reader.GetString("restaurantName");

                        Restaurant restaurant = new Restaurant(restaurant_id, restaurantName, "", "", "", "", TimeSpan.Zero, TimeSpan.Zero, null);
                        Service service = new Service(service_id, startTime, endTime, serviceType);
                        CustomerAccount customer = new CustomerAccount(customerId,"","","","","");

                        Dictionary<Meal, int> meals = await GetMealsByOrderNumberAsync(orderNumber, restaurant);
                        Order order = new Order(orderNumber, status, orderTime, isDelivery, service, customer, meals, restaurant);

                        orders.Add(order);
                    }
                }
            }

            return orders;
        }

    }


}
