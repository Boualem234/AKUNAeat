using AKUNAeat.Models;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Interfaces
{
    public interface IAccountDAL
    {
        Task<(bool, int)> InsertCustomerAsync(CustomerAccount account);
        Task<(bool, int)> InsertRestaurantOwnerAsync(RestaurantOwnerAccount account);

        Task<(bool, int)> FindAccountAsync(string email,string password, bool isCustomer);

        Task<bool> DoesEmailExistAsync(string email);

        Task<CustomerAccount?> GetCustomerByIdAsync(int customerId);
        Task<RestaurantOwnerAccount?> GetRestaurantOwnerAccountAsync(int accountId);
    }
}
