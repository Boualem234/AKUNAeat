using AKUNAeat.DAL;
using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using AKUNAeat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AKUNAeat.Controllers
{
    public class RestaurantController : Controller
    {
        private IRestaurantDAL restaurantDAL;
        private IMealDAL mealDAL;
        private IOrderDAL orderDAL;

        public RestaurantController(IRestaurantDAL restaurantDAL, IMealDAL mealDAL, IOrderDAL orderDAL)
        {
            this.restaurantDAL = restaurantDAL;
            this.mealDAL = mealDAL;
            this.orderDAL = orderDAL;
        }
        private void SetUserIdsToViewDataRestaurantController()
        {
            ViewData["CustomerAccountId"] = HttpContext.Session.GetInt32("CustomerAccountId");
            ViewData["RestaurantOwnerAccountId"] = HttpContext.Session.GetInt32("RestaurantOwnerAccountId");
        }

        private bool CheckIdRestaurantOwner()
        {
            return HttpContext.Session.GetInt32("RestaurantOwnerAccountId") != null;
        }

        public async Task<IActionResult> IndexRestaurantOwner()
        {

            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            int accountId = HttpContext.Session.GetInt32("RestaurantOwnerAccountId").Value;
            List<Restaurant> restaurants = await Restaurant.GetRestaurantsByAccountIDAsync(accountId, restaurantDAL);

            return View(restaurants);
        }

        public IActionResult ConnectExistingRestaurant(int restaurant_id)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            HttpContext.Session.SetInt32("RestaurantId", restaurant_id);

            return RedirectToAction("IndexRestaurantManagement");

        }

        public IActionResult IndexRestaurantManagement()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            return View();
        }

        public IActionResult ChangeRestaurant()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            HttpContext.Session.Remove("RestaurantId");

            return RedirectToAction("IndexRestaurantOwner");

        }

        public IActionResult AddService()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            return View();
        }

        public async Task<IActionResult> ManageMenus()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            List<Menu> menus = await Menu.GetMenusByRestaurantIdAsync(HttpContext.Session.GetInt32("RestaurantId").Value, mealDAL);

            return View(menus);
        }

        public async Task<IActionResult> ModifyMenu(int menu_id)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            if (ViewData["RestaurantOwnerAccountId"] == null)
            {
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;

            if (restaurantId == 0)
            {
                return View();
            }

            Menu? menu = await Menu.GetMenuByIdAsync(menu_id, mealDAL);

            if (menu != null)
            {
                ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);

                MenuDishViewModel vm = new MenuDishViewModel(menu);

                foreach (Dish dish in menu.Dishes)
                {
                    vm.AddDish(dish);
                }

                ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;

                return View(vm);
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> ModifyMenu(MenuDishViewModel vm)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            if (ViewData["RestaurantOwnerAccountId"] == null)
            {
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;

            Restaurant? r = await Restaurant.GetRestaurantAsync(restaurantId, restaurantDAL);
            Menu menu = new Menu(vm.MenuId, vm.Name, vm.Price, r, vm.Description, null);
            menu.Dishes = vm.SelectedDishIds.Select(id => new Dish { MealId = id }).ToList();


            if (restaurantId == 0)
            {
                TempData["ErrorMessage"] = "Restaurant ID not found in session.";
                return RedirectToAction("IndexRestaurantOwner");
            }

            if (r != null)
            {
                bool success = await menu.ModifyMenuAsync(mealDAL);

                if (success)
                {
                    TempData["SuccessMessage"] = "Menu modifié avec succès !";
                    return RedirectToAction("ManageMenus");
                }
                else
                {
                    ModelState.AddModelError("", "Erreur lors de la modification du menu.");
                    ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                    ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;

                    return View(vm);
                }
            }
            else
            {
                ModelState.AddModelError("", "Restaurant not found.");
                ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;

                return View(vm);
            }
        }

        public IActionResult ConfirmDeleteMenu(int menu_id)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            TempData["menuId"] = menu_id;

            return View();
        }


        public async Task<IActionResult> DeleteMenu(int menu_id)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;

            if (restaurantId == 0)
            {
                TempData["ErrorMessage"] = "Restaurant ID not found in session.";
                return RedirectToAction("IndexRestaurantOwner");
            }

            Menu? menu = await Menu.GetMenuByIdAsync(menu_id, mealDAL);
            bool success = await menu.DeleteMenuAsync(restaurantId, mealDAL);

            if (success)
            {
                TempData["SuccessMessage"] = "Menu supprimé avec succès !";
                return RedirectToAction("ManageMenus");
            }
            else
            {
                TempData["ErrorMessage"] = "Erreur lors de la suppression du menu.";
                return RedirectToAction("ManageMenus");
            }

        }

        public IActionResult NumberDishes()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult SetDishCount(int dishCount)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            TempData["DishCount"] = dishCount;
            return RedirectToAction("AddMenu");
        }


        public async Task<IActionResult> AddMenu()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            ViewBag.DishCount = TempData["DishCount"];
            ViewBag.AllDishes = await Dish.GetDishesAsync(HttpContext.Session.GetInt32("RestaurantId").Value, mealDAL);

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AddMenu(MenuDishViewModel vm)
        {

            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;

            if (ModelState.IsValid)
            {
                if (restaurantId == 0)
                {
                    ModelState.AddModelError("", "Restaurant ID not found in session.");
                    ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;
                    ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                    return View(vm);
                }

                Restaurant? r = await Restaurant.GetRestaurantAsync(restaurantId, restaurantDAL);
                Menu menu = new Menu(0, vm.Name, vm.Price,r, vm.Description, null);

                menu.Dishes = vm.SelectedDishIds.Select(id => new Dish { MealId = id }).ToList();

                (bool menuAdd,int menuId) = await menu.AddMenuAsync(restaurantId, mealDAL);

                if (!menuAdd)
                {
                    ModelState.AddModelError("", "Erreur lors de l’insertion du menu.");
                    ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;
                    ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                    return View(menu);
                }

                bool success = await menu.InsertMenuDishesAsync(menuId, mealDAL);

                if (!success)
                {
                    ModelState.AddModelError("", "Erreur lors de l’insertion des plats du menu.");
                    ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;
                    ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Menu ajouté avec succès !";
                return RedirectToAction("ManageMenus");
            }
            else
            {
                ViewBag.DishCount = vm.SelectedDishIds?.Count ?? 0;
                ViewBag.AllDishes = await Dish.GetDishesAsync(restaurantId, mealDAL);
                return View(vm);
            }
        }

        public IActionResult AddDish()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AddDish(Dish dish)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();


            int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;
            if (restaurantId == 0)
            {
                ModelState.AddModelError("", "Restaurant ID not found in session.");
                return View(dish);
            }

            Restaurant? r = await Restaurant.GetRestaurantAsync(restaurantId, restaurantDAL);
            r.AddMeal(dish);

            if (r != null)
            {

                bool success = await dish.AddDishAsync(restaurantId, mealDAL);

                if (success)
                {
                    TempData["SuccessMessage"] = "Dish ajouté avec succès !";
                    return RedirectToAction("IndexRestaurantManagement");
                }
                else
                {
                    ModelState.AddModelError("", "Erreur lors de l'ajout du service.");
                    return View(dish);
                }
            }
            else
            {
                ModelState.AddModelError("", "Restaurant not found.");
                return View(dish);
            }

        }


        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AddService(Service service)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            if (service.EndTime <= service.StartTime)
            {
                ModelState.AddModelError("", "L'heure de fin doit être après l'heure de début et le service ne peut pas durer plus de 24h");
                return View();
            }

            SetUserIdsToViewDataRestaurantController();

            if (ModelState.IsValid)
            {
                int restaurantId = HttpContext.Session.GetInt32("RestaurantId").Value;
                if (restaurantId == 0)
                {
                    ModelState.AddModelError("", "Restaurant ID not found in session.");
                    return View(service);
                }

                Restaurant? r = await Restaurant.GetRestaurantAsync(restaurantId, restaurantDAL);

                if (r != null)
                {
                    r.AddService(service);

                    bool success = await service.AddServiceAsync(restaurantId, restaurantDAL);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Service ajouté avec succès !";
                        return RedirectToAction("IndexRestaurantManagement");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Erreur lors de l'ajout du service.");
                        return View(service);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Restaurant not found.");
                    return View(service);
                }
            }
            else
            {
                return View(service);
            }
        }

        public IActionResult AddRestaurant()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AddRestaurant(RestaurantServiceViewModel vm)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            if (ModelState.IsValid)
            {
                Restaurant r = vm.Restaurant;
                Service s = vm.Service;

                r.AddService(s);

                (bool success,int restaurantId) = await r.AddRestaurantAsync(HttpContext.Session.GetInt32("RestaurantOwnerAccountId").Value,s, restaurantDAL);

                if (success)
                {
                    TempData["SuccessMessage"] = "Restaurant et service ajoutés avec succès !";

                    HttpContext.Session.SetInt32("RestaurantId", restaurantId);

                    TempData["RestaurantId"] = restaurantId;

                    return RedirectToAction("IndexRestaurantManagement");
                }
                else
                {
                    return View(vm);
                }
            }
            else
            {
                return View(vm);
            }
        }

        public async Task<IActionResult> HandleOrders()
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            int restaurantId = HttpContext.Session.GetInt32("RestaurantId") ?? 0;

            if (restaurantId == 0)
            {
                ModelState.AddModelError("", "Restaurant ID not found in session.");
                return RedirectToAction("IndexRestaurantManagement");
            }

            List<Order> orders = await Order.GetOrdersByRestaurantIdAsync(restaurantId, orderDAL);

            return View(orders);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderNumber, Status newStatus)
        {
            if (!CheckIdRestaurantOwner())
            {
                TempData["ErrorMessage"] = "Veuillez vous connecter pour accéder a cette fonctionnalité.";
                return RedirectToAction("LoginRestaurantOwner", "Home");
            }

            SetUserIdsToViewDataRestaurantController();

            Order order = new Order(orderNumber, newStatus,DateTime.Now, false, null, null, null, null);

            bool success = await order.UpdateOrderStatusAsync(newStatus, orderDAL);

            if (success)
            {
                TempData["SuccessMessage"] = "Statut de la commande mis à jour avec succès !";
            }
            else
            {
                TempData["ErrorMessage"] = "Erreur lors de la mise à jour du statut de la commande.";
            }

            return RedirectToAction("HandleOrders");
        }




    }
}
