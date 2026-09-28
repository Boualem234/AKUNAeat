using AKUNAeat.API;
using AKUNAeat.DAL;
using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using AKUNAeat.Session;
using AKUNAeat.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics;  //TEMPORAIRE
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;



namespace AKUNAeat.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IRestaurantDAL restaurantDAL;
        private readonly IMealDAL mealDAL;
        private readonly IOrderDAL orderDAL;
        private readonly IAccountDAL accountDAL;

        public CustomerController(IRestaurantDAL restaurantDAL, IMealDAL mealDAL, IOrderDAL orderDAL, IAccountDAL accountDAL)
        {
            this.restaurantDAL = restaurantDAL;
            this.mealDAL = mealDAL;
            this.orderDAL = orderDAL;
            this.accountDAL = accountDAL;
        }

        private void SetUserIdsToViewDataCustomerController()
        {
            ViewData["CustomerAccountId"] = HttpContext.Session.GetInt32("CustomerAccountId");
            ViewData["RestaurantOwnerAccountId"] = HttpContext.Session.GetInt32("RestaurantOwnerAccountId");
        }

        private bool CheckIdCustomer()
        {
            return HttpContext.Session.GetInt32("CustomerAccountId") != null;
        }

        //RESTAURANTS 

        public async Task<IActionResult> ListRestaurants(int page = 1, int pageSize = 10)
        {
            SetUserIdsToViewDataCustomerController();

            List<Restaurant> restaurants = await Restaurant.GetRestaurantsAsync(restaurantDAL);
            List<Restaurant> paginatedRestaurants = restaurants
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)restaurants.Count / pageSize);

            return View(paginatedRestaurants);
        }

        public async Task<IActionResult> DetailsRestaurant(int id)
        {
            SetUserIdsToViewDataCustomerController();

            Restaurant? restaurant = await Restaurant.GetRestaurantAsync(id, restaurantDAL);

            if (restaurant == null)
            {
                TempData["ErrorMessage"] = "Restaurant introuvable.";
                return RedirectToAction("ListRestaurants");
            }

            return View(restaurant);
        }

        //MENUS

        public async Task<IActionResult> MenusList(int restaurantId)
        {
            SetUserIdsToViewDataCustomerController();

            if(restaurantId == 0)
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner un restaurant.";
                return RedirectToAction("ListRestaurants");
            }

            List<Menu> menus = await Menu.GetMenusByRestaurantIdAsync(restaurantId, mealDAL);

            ViewData["RestaurantId"] = restaurantId;

            return View(menus);
        }

        public async Task<IActionResult> MenuDetails(int mealId)
        {

            SetUserIdsToViewDataCustomerController();

            Menu? menu = await Menu.GetMenuByIdAsync(mealId, mealDAL);

            if (menu == null)
            {
                TempData["ErrorMessage"] = "Menu introuvable.";
                return RedirectToAction("MenusList");
            }

            ViewData["RestaurantId"] = menu.Restaurant.RestaurantId;

            return View(menu);
        }

        //DISHES

        public async Task<IActionResult> DishesList(int restaurantId)
        {
            SetUserIdsToViewDataCustomerController();

            List<Dish> dishes = await Dish.GetDishesAsync(restaurantId, mealDAL);

            ViewData["RestaurantId"] = restaurantId;
            return View(dishes);
        }

        //ORDER

        public async Task<IActionResult> PlaceOrder(int restaurantId)
        {

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            List<Meal> meals = await Meal.GetMealsAsync(restaurantId, mealDAL);

            if (meals == null || meals.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucun plat disponible.";
                return RedirectToAction("ListRestaurants");
            }

            HttpContext.Session.SetInt32("RestaurantIdSelected", restaurantId);

            MealSelectionViewModel mealSelectionViewModel = new MealSelectionViewModel
            {
                Selections = meals.Select(m => new MealQuantitySelection { Meal = m, Quantity = 0 }).ToList()
            };

            return View(mealSelectionViewModel);
        }

        //doc microsoft; configure la sérialisation JSON pour que le
        //type abstrait Meal puisse être correctement désérialisé en ses types dérivés Menu ou Dish
        static void PolymorphicTypeResolver(JsonTypeInfo typeInfo) 
        {
            if (typeInfo.Type == typeof(Meal))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    DerivedTypes ={
                        new JsonDerivedType(typeof(Menu), "Menu"),
                        new JsonDerivedType(typeof(Dish), "Dish")
                    }
                };
            }
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult SubmitMealQuantities(MealSelectionViewModel model)
        {
            if (model == null || !model.IsValid())
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner au moins un plat.";
                return RedirectToAction("PlaceOrder", new { restaurantId = HttpContext.Session.GetInt32("RestaurantIdSelected").Value });
            }

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            Dictionary<string, int> selectedMeals = new Dictionary<string, int>();

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicTypeResolver }
                },
            };

            foreach (MealQuantitySelection selection in model.Selections)
            {
                if (selection.Quantity > 0)
                {
                    string serializedMeal = JsonSerializer.Serialize(selection.Meal, options);
                    selectedMeals.Add(serializedMeal, selection.Quantity);
                }
            }

            string selectedMealsJson = JsonSerializer.Serialize(selectedMeals, options);
            HttpContext.Session.SetString("SelectedMeals", selectedMealsJson);

            return RedirectToAction("TakeService");
        }

        public async Task<IActionResult> TakeService()
        {

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            List<Service> services = await Service.GetServicesByRestaurantId(HttpContext.Session.GetInt32("RestaurantIdSelected").Value, restaurantDAL);

            if (services == null || services.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucun service disponible.";
                return RedirectToAction("ListRestaurants");
            }

            return View(services);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]

        public async Task<IActionResult> Delivery(Service s)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner un service.";
                return RedirectToAction("TakeService");
            }

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            DateTime now = DateTime.Now;
            DateTime serviceStartToday = now.Date + s.StartTime;
            DateTime serviceEndToday = now.Date + s.EndTime;

            if (now > serviceStartToday - TimeSpan.FromHours(2) || now > serviceEndToday)
            {
                TempData["ErrorMessage"] = "Vous ne pouvez plus commander pour ce service. Il faut commander au plus tard 2 heures avant le début du service.";
                return RedirectToAction("DetailsRestaurant", new { id = s.ServiceId });
            }

            SetUserIdsToViewDataCustomerController();

            HttpContext.Session.SetString("Service", JsonSerializer.Serialize(s));

            CustomerAccount? customer = await CustomerAccount.GetCustomerAsync(HttpContext.Session.GetInt32("CustomerAccountId").Value, accountDAL);
            Restaurant? restaurant = await Restaurant.GetRestaurantAsync(HttpContext.Session.GetInt32("RestaurantIdSelected").Value, restaurantDAL);
            string customerAddress = customer.Adress;
            string restaurantAddress = restaurant.Address;

            OpenRouteServiceDistance service = new OpenRouteServiceDistance("5b3ce3597851110001cf62488ef6adcab5b94c6baee1a596c46d5961");
            double distance = await service.GetDistanceAsync(customerAddress, restaurantAddress);
            Console.WriteLine(distance);

            TempData["distance"] = distance;

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]

        public async Task<IActionResult> OrderSummary(string Option)
        {

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            bool isDelivery= false;

            if (Option == "false")
            {
                isDelivery = false;
            }
            else if (Option == "true")
            {
                isDelivery = true;
            }

            HttpContext.Session.SetString("IsDelivery", isDelivery.ToString());

            string selectedMealsJson = HttpContext.Session.GetString("SelectedMeals");
            if (string.IsNullOrEmpty(selectedMealsJson))
            {
                TempData["ErrorMessage"] = "Aucune sélection de plats trouvée.";
                return RedirectToAction("ListRestaurants");
            }

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicTypeResolver }
                }
            };

            Dictionary<string, int> stringSelected = JsonSerializer.Deserialize<Dictionary<string, int>>(selectedMealsJson, options);
            Dictionary<Meal, int> selectedMeals = stringSelected.ToDictionary(kvp => JsonSerializer.Deserialize<Meal>(kvp.Key, options),kvp => kvp.Value);

            if (selectedMeals == null || selectedMeals.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucun plat sélectionné.";
                return RedirectToAction("ListRestaurants");
            }

            Service s = JsonSerializer.Deserialize<Service>(HttpContext.Session.GetString("Service"));

            if (s == null)
            {
                TempData["ErrorMessage"] = "Aucun service sélectionné.";
                return RedirectToAction("ListRestaurants");
            }

            CustomerAccount? customer = await CustomerAccount.GetCustomerAsync(HttpContext.Session.GetInt32("CustomerAccountId").Value, accountDAL);

            Restaurant? restaurant = await Restaurant.GetRestaurantAsync(HttpContext.Session.GetInt32("RestaurantIdSelected").Value, restaurantDAL);

            Order o = new Order(0, Status.InPreparation,DateTime.Now,isDelivery,s, customer, selectedMeals, restaurant);

            return View(o);
        }


        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> OrderConfirmation()
        {
            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { PolymorphicTypeResolver }
                }
            };

            CustomerAccount? customer = await CustomerAccount.GetCustomerAsync(HttpContext.Session.GetInt32("CustomerAccountId").Value, accountDAL);
            Restaurant? restaurant = await Restaurant.GetRestaurantAsync(HttpContext.Session.GetInt32("RestaurantIdSelected").Value, restaurantDAL);
            string selectedMealsJson = HttpContext.Session.GetString("SelectedMeals");
            Dictionary<string, int> stringSelected = JsonSerializer.Deserialize<Dictionary<string, int>>(selectedMealsJson, options);
            Dictionary<Meal, int> selectedMeals = stringSelected.ToDictionary(kvp => JsonSerializer.Deserialize<Meal>(kvp.Key, options), kvp => kvp.Value);
            Service s = JsonSerializer.Deserialize<Service>(HttpContext.Session.GetString("Service"), options);
            bool isDelivery = bool.Parse(HttpContext.Session.GetString("IsDelivery"));

            Order o = new Order(0, Status.InPreparation, DateTime.Now, isDelivery, s, customer, selectedMeals, restaurant);

            if (o == null)
            {
                TempData["ErrorMessage"] = "Erreur lors de la création de la commande.";
                return RedirectToAction("ListRestaurants");
            }

            await o.SaveOrderAsync(orderDAL);

            TempData["SuccessMessage"] = "Commande passée avec succès.";
            return RedirectToAction("ListRestaurants");
        }

        public IActionResult CancelOrder()
        {

            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }
            
            SetUserIdsToViewDataCustomerController();

            HttpContext.Session.Remove("SelectedMeals");
            HttpContext.Session.Remove("Service");

            TempData["ErrorMessage"] = "Commande annulée.";
            return RedirectToAction("ListRestaurants");
        }

        public async Task<IActionResult> ViewStatutOrder()
        {
            if (!CheckIdCustomer())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginCustomer", "Home");
            }

            SetUserIdsToViewDataCustomerController();

            List<Order> orders = await Order.GetOrdersWithDetailsByCustomerIdAsync(HttpContext.Session.GetInt32("CustomerAccountId").Value, orderDAL);

            if (orders == null || orders.Count == 0)
            {
                TempData["ErrorMessage"] = "Aucune commande trouvée.";
                return RedirectToAction("ListRestaurants");
            }

            return View(orders);
        }

    }
   
}
