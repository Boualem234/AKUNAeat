using AKUNAeat.Models;

namespace AKUNAeat.Interfaces
{
    public interface IRestaurantDAL
    {
        Task<(bool, int)> InsertRestaurantWithServiceAsync(Restaurant restaurant, int accountId, Service s);
        Task<bool> InsertServiceAsync(Service service, int restaurantId);
        Task<Restaurant?> GetRestaurantWithServicesByIdAsync(int restaurantId);
        Task<List<Restaurant>> GetRestaurantsAsync();
        Task<List<Restaurant>> GetRestaurantByAccountIdAsync(int accountId);

        Task<List<Service>> GetServicesByRestaurantIdAsync(int restaurantId);


    }
}
