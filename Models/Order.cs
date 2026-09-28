using AKUNAeat.Interfaces;
using AKUNAeat.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Models
{
    public enum Status
    {
        ReadyForPickup,
        InPreparation,
        OrderCompleted
    }
    public class Order
    {
        private int orderNumber;
        private Status status;
        private DateTime orderTime;
        private bool isDelivery;
        private Dictionary<Meal, int> meals;
        private Service service;
        private CustomerAccount customer;
        private Restaurant restaurant;

        public Restaurant Restaurant
        {
            get { return restaurant; }
            set { restaurant = value; }
        }

        public Dictionary<Meal, int> Meals
        {
            get { return meals; }
            set { meals = value; }
        }

        public Service Service
        {
            get { return service; }
            set { service = value; }
        }

        public CustomerAccount Customer
        {
            get { return customer; }
            set { customer = value; }
        }

        public int OrderNumber
        {
            get { return orderNumber; }
            set { orderNumber = value; }
        }

        public Status Status
        {
            get { return status; }
            set { status = value; }
        }

        public bool IsDelivery
        {
            get { return isDelivery; }
            set { isDelivery = value; }
        }

        public DateTime OrderTime
        {
            get { return orderTime; }
            set { orderTime = value; }
        }

        public Order()
        {
            meals = new Dictionary<Meal, int>();
        }

        public Order(int orderNumber, Status status, DateTime orderTime, bool isDelivery, Service service, CustomerAccount customer,Meal m,int quantity, Restaurant restaurant)
        {
            OrderNumber = orderNumber;
            Status = status;
            OrderTime = orderTime;
            IsDelivery = isDelivery;
            Service = service;
            Customer = customer;
            Meals = new Dictionary<Meal, int>();
            AddMeal(m, quantity);
            Restaurant = restaurant;
        }

        public Order(int orderNumber, Status status, DateTime orderTime, bool isDelivery, Service service, CustomerAccount customer, Dictionary<Meal,int> list, Restaurant restaurant)
        {
            OrderNumber = orderNumber;
            Status = status;
            OrderTime = orderTime;
            IsDelivery = isDelivery;
            Service = service;
            Customer = customer;
            Meals = list;
            Restaurant = restaurant;
        }

        public void AddMeal(Meal meal, int quantity)
        {
            if (meals.ContainsKey(meal))
            {
                meals[meal] += quantity;
            }
            else
            {
                meals.Add(meal, quantity);
            }
        }

        public decimal CalculateTotalPrice()
        {
            decimal totalPrice = 0;
            foreach (var item in meals)
            {
                totalPrice += item.Key.Price * item.Value;
            }

            if (IsDelivery)
            {
                totalPrice += 4; 
            }

            return totalPrice;
        }

        public async Task<bool> SaveOrderAsync(IOrderDAL dal)
        {
            return await dal.InsertOrderAsync(this);
        }

        public async Task<bool> UpdateOrderStatusAsync(Status newStatus, IOrderDAL orderDAL)
        {
            return await orderDAL.UpdateOrderStatusAsync(this, newStatus);
        }

        public static async Task<List<Order>> GetOrdersByRestaurantIdAsync(int restaurantId, IOrderDAL orderDAL)
        {
            return await orderDAL.GetOrdersWithServiceByRestaurantIdAsync(restaurantId);
        }

        public static async Task<List<Order>> GetOrdersWithDetailsByCustomerIdAsync(int customerId, IOrderDAL orderDAL)
        {
            return await orderDAL.GetOrdersByCustomerIdAsync(customerId);
        }




    }
}
