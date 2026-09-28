using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using Microsoft.Data.SqlClient;
using System.Data;


namespace AKUNAeat.DAL
{
    public class RestaurantDAL : IRestaurantDAL
    {
        private string connectionString;

        public RestaurantDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<(bool, int)> InsertRestaurantWithServiceAsync(Restaurant restaurant, int accountId, Service s)
        {
            string insertRestaurantQuery = @"INSERT INTO Restaurant 
            (account_id, name, address, cuisineType, description, phoneNumber, openingHours, closingHours) 
            OUTPUT INSERTED.restaurant_id VALUES (@account_id, @name, @address, @cuisineType, @description, @phoneNumber, @openingHours, @closingHours);";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(insertRestaurantQuery, connection);
                cmd.Parameters.AddWithValue("@account_id", accountId);
                cmd.Parameters.AddWithValue("@name", restaurant.Name);
                cmd.Parameters.AddWithValue("@address", restaurant.Address);
                cmd.Parameters.AddWithValue("@cuisineType", restaurant.CuisineType);
                cmd.Parameters.AddWithValue("@description", restaurant.Description);
                cmd.Parameters.AddWithValue("@phoneNumber", restaurant.PhoneNumber);
                cmd.Parameters.AddWithValue("@openingHours", restaurant.OpeningHours);
                cmd.Parameters.AddWithValue("@closingHours", restaurant.ClosingHours);

                await connection.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    int insertedRestaurantId = Convert.ToInt32(result);

                    return (await InsertServiceAsync(s, insertedRestaurantId),insertedRestaurantId);
                }
                else
                {
                    return (false,0); 
                }
            }
        }

        public async Task<bool> InsertServiceAsync(Service service, int restaurantId)
        {
            string query = @"INSERT INTO Service (restaurant_id, startTime, endTime, serviceType) 
                            VALUES (@restaurant_id, @startTime, @endTime, @serviceType);";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@restaurant_id", restaurantId);
                cmd.Parameters.AddWithValue("@startTime", service.StartTime);
                cmd.Parameters.AddWithValue("@endTime", service.EndTime);
                cmd.Parameters.AddWithValue("@serviceType", service.ServiceType.ToString());

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return res > 0;
            }
        }



        public async Task<Restaurant?> GetRestaurantWithServicesByIdAsync(int restaurantId)
        {
            Restaurant? restaurant = null;

            string query = @"SELECT r.restaurant_id, r.name, r.address, r.cuisineType, r.description, r.phoneNumber,
                    r.openingHours, r.closingHours, s.service_id, s.startTime, s.endTime, s.serviceType
                    FROM Restaurant r
                    LEFT JOIN Service s ON s.restaurant_id = r.restaurant_id
                    WHERE r.restaurant_id = @restaurant_id;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@restaurant_id", restaurantId);

                try
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Initialize Restaurant if not already done
                            if (restaurant == null)
                            {
                                int restaurant_id = reader.GetInt32("restaurant_id");
                                string name = reader.GetString("name");
                                string address = reader.GetString("address");
                                string cuisineType = reader.GetString("cuisineType");
                                string description = reader.GetString("description");
                                string phoneNumber = reader.GetString("phoneNumber");
                                TimeSpan openingHours = reader.GetTimeSpan(reader.GetOrdinal("openingHours"));
                                TimeSpan closingHours = reader.GetTimeSpan(reader.GetOrdinal("closingHours"));

                                restaurant = new Restaurant(restaurant_id, name, address, cuisineType, description, phoneNumber, openingHours, closingHours, null);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("service_id")))
                            {
                                int serviceId = reader.GetInt32("service_id");
                                TimeSpan startTime = reader.GetTimeSpan(reader.GetOrdinal("startTime"));
                                TimeSpan endTime = reader.GetTimeSpan(reader.GetOrdinal("endTime"));
                                string serviceTypeString = reader.GetString("serviceType");

                                if (Enum.TryParse<ServiceType>(serviceTypeString, out ServiceType serviceType))
                                {
                                    Service service = new Service(serviceId, startTime, endTime, serviceType);
                                    restaurant.AddService(service);
                                }
                                else
                                {
                                    Console.WriteLine($"Invalid ServiceType '{serviceTypeString}' for service_id {serviceId}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving restaurant: {ex.Message}");
                }
            }

            return restaurant;
        }

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            string query = "SELECT * FROM Restaurant";
            List<Restaurant> restaurants = new List<Restaurant>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int restaurant_id = reader.GetInt32("restaurant_id");
                        string name = reader.GetString("name");
                        string address = reader.GetString("address");
                        string cuisineType = reader.GetString("cuisineType");
                        string description = reader.GetString("description");
                        string phoneNumber = reader.GetString("phoneNumber");
                        TimeSpan openingHours = reader.GetTimeSpan(reader.GetOrdinal("openingHours"));
                        TimeSpan closingHours = reader.GetTimeSpan(reader.GetOrdinal("closingHours"));

                        Service service = new Service(); 

                        Restaurant r = new Restaurant(restaurant_id, name, address, cuisineType, description, phoneNumber, openingHours, closingHours, service);

                        restaurants.Add(r);
                    }
                }
            }

            return restaurants;
        }


        public async Task<List<Restaurant>> GetRestaurantByAccountIdAsync(int accountId)
        {
            List<Restaurant> restaurants = new List<Restaurant>();

            string query = @"SELECT * FROM Restaurant WHERE account_id = @account_id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@account_id", accountId);

                await connection.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32("restaurant_id");
                        string name = reader.GetString("name");
                        string address = reader.GetString("address");
                        string cuisineType = reader.GetString("cuisineType");
                        string description = reader.GetString("description");
                        string phoneNumber = reader.GetString("phoneNumber");
                        TimeSpan openingHours = reader.GetTimeSpan(reader.GetOrdinal("openingHours"));
                        TimeSpan closingHours = reader.GetTimeSpan(reader.GetOrdinal("closingHours"));


                        Service service = new Service();
                        Restaurant r = new Restaurant(id, name, address, cuisineType,description,phoneNumber,openingHours,closingHours, service);

                        restaurants.Add(r);
                    }
                }

                return restaurants;
            }
        }

        public async Task<List<Service>> GetServicesByRestaurantIdAsync(int restaurantId)
        {
            string query = @"SELECT service_id, startTime, endTime, serviceType 
                            FROM Service WHERE restaurant_id = @restaurantId";

            List<Service> services = new List<Service>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@restaurantId", restaurantId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int serviceId = reader.GetInt32("service_id");
                        TimeSpan startTime = reader.GetTimeSpan(reader.GetOrdinal("startTime"));
                        TimeSpan endTime = reader.GetTimeSpan(reader.GetOrdinal("endTime"));
                        ServiceType serviceType = Enum.Parse<ServiceType>(reader.GetString("serviceType"));

                        Service service = new Service(serviceId, startTime, endTime, serviceType);

                        services.Add(service);
                    }
                }
            }

            return services;
        }

    }

}
