using AKUNAeat.DAL;
using AKUNAeat.Interfaces;

namespace AKUNAeat.Models
{
	public class RestaurantOwnerAccount : Account
	{
		private List<Restaurant> restaurants;

		public List<Restaurant> Restaurants
		{
			get { return restaurants; }
			set { restaurants = value; }
		}

		public RestaurantOwnerAccount() 
		{
            restaurants = new List<Restaurant>();
        }
        public RestaurantOwnerAccount(int accountId,string firstName, string lastName, string email, string password) : base(accountId,firstName, lastName, email, password)
		{
			this.restaurants = new List<Restaurant>();
		}

		public void AddRestaurant(Restaurant restaurant)
		{
			if (restaurant != null)
			{
				restaurants.Add(restaurant);
			}
		}

        public async override Task<(bool, int)> CreateAccountAsync(IAccountDAL dal)
        {
            return await dal.InsertRestaurantOwnerAsync(this);
        }

        public static async Task<(bool, int)> FindRestaurantOwnerAccountAsync(string email, string password, IAccountDAL dal)
        {
            return await dal.FindAccountAsync(email,password, false);
        }

		public static async Task<RestaurantOwnerAccount?> GetRestaurantOwnerAccountAsync(int accountId, IAccountDAL dal)
		{
			return await dal.GetRestaurantOwnerAccountAsync(accountId);
        }

    }

}
