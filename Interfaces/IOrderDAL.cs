using AKUNAeat.Models;
using AKUNAeat.ViewModels;
using System.Threading.Tasks;

namespace AKUNAeat.Interfaces
{
    public interface IOrderDAL
    {
        Task<bool> InsertOrderAsync(Order order);

        Task<Dictionary<Meal, int>> GetMealsByOrderNumberAsync(int orderNumber,Restaurant r);

        Task<List<Order>> GetOrdersWithServiceByRestaurantIdAsync(int restaurantId);

        Task<bool> UpdateOrderStatusAsync(Order order, Status newStatus);

        Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId);

    }
}
