using AKUNAeat.DAL;
using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using AKUNAeat.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AKUNAeat.Controllers
{
    public class HomeController : Controller
    {
        private IAccountDAL accountDAL;
        public HomeController(IAccountDAL accountDAL)
        {
            this.accountDAL = accountDAL;
        }

        private void SetUserIdsToViewDataHomeController()
        {
            ViewData["CustomerAccountId"] = HttpContext.Session.GetInt32("CustomerAccountId");
            ViewData["RestaurantOwnerAccountId"] = HttpContext.Session.GetInt32("RestaurantOwnerAccountId");
        }

        public IActionResult Index()
        {
            SetUserIdsToViewDataHomeController();

            return View();
        }

        public IActionResult LoginCustomer()
        {
            SetUserIdsToViewDataHomeController();

            return View();
        }

        public IActionResult LoginRestaurantOwner()
        {
            SetUserIdsToViewDataHomeController();

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> LoginCustomer(LoginViewModel account)
        {
            SetUserIdsToViewDataHomeController();

            if (ModelState.IsValid)
            {
                (bool success, int accountId) = await CustomerAccount.FindCustomerAccountAsync(account.Email, account.Password, accountDAL);

                if (success)
                {
                    HttpContext.Session.SetInt32("CustomerAccountId", accountId);
                    return RedirectToAction("ListRestaurants", "Customer");
                }
                else
                {
                    ModelState.AddModelError("Email", "Adresse email ou mot de passe incorrect.");
                    return View();
                }
            }
            else
            {
                return View(account);
            }
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> LoginRestaurantOwner(LoginViewModel account)
        {
            SetUserIdsToViewDataHomeController();

            if(ModelState.IsValid)
            {
                (bool success, int accountId) = await RestaurantOwnerAccount.FindRestaurantOwnerAccountAsync(account.Email, account.Password, accountDAL);

                if (success)
                {
                    HttpContext.Session.SetInt32("RestaurantOwnerAccountId", accountId);
                    return RedirectToAction("IndexRestaurantOwner", "Restaurant");
                }
                else
                {
                    ModelState.AddModelError("Email", "Adresse email ou mot de passe incorrect.");
                    return View();
                }
            }
            else
            {
                return View(account);
            }

        }

        public IActionResult CreateCustomerAccount()
        {
            SetUserIdsToViewDataHomeController();

            return View();
        }

        public IActionResult CreateRestaurantAccount()
        {
            SetUserIdsToViewDataHomeController();

            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> CreateCustomerAccount(CustomerAccount accountC)
        {
            SetUserIdsToViewDataHomeController();

            if (ModelState.IsValid)
            {
                bool emailExists = await CustomerAccount.DoesEmailExistAsync(accountC.Email, accountDAL);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Cette adresse email est déjà utilisée.");
                    return View(accountC);
                }

                (bool success, int accountId) = await accountC.CreateAccountAsync(accountDAL);

                if (success)
                {
                    HttpContext.Session.SetInt32("CustomerAccountId", accountId);
                    return RedirectToAction("ListRestaurants", "Customer");
                }
                else
                {
                    return View(accountC);
                }
            }
            else
            {
                return View(accountC);
            }
        }




        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> CreateRestaurantAccount(RestaurantOwnerAccount accountR)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await RestaurantOwnerAccount.DoesEmailExistAsync(accountR.Email, accountDAL);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Cette adresse email est déjà utilisée.");
                    return View(accountR);
                }

                (bool success, int accountId) = await accountR.CreateAccountAsync(accountDAL);

                if(success)
                {
                    HttpContext.Session.SetInt32("RestaurantOwnerAccountId", accountId);
                    return RedirectToAction("IndexRestaurantOwner", "Restaurant");
                }
                else
                {
                    return View(accountR);
                }
            }
            else
            {
                return View(accountR);
            }
        }
        public IActionResult Logout()
        {
            SetUserIdsToViewDataHomeController();
            return View();
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult ConfirmLogout()
        {

            HttpContext.Session.Clear(); 
            return RedirectToAction("Index", "Home"); 
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
