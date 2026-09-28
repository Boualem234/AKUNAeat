using AKUNAeat.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Models
{
    public class Restaurant
    {
        private int restaurantId;
        private string name;
        private string address;
        private string cuisineType;
        private string description;
        private string phoneNumber;
        private TimeSpan openingHours;
        private TimeSpan closingHours;

        private List<Meal> meals;
        private List<Service> services;

        public List<Service> Services
        {
            get { return services; }
            set { services = value; }
        }
        public List<Meal> Meals
        {
            get { return meals; }
            set { meals = value; }
        }

        public int RestaurantId
        {
            get { return restaurantId; }
            set { restaurantId = value; }
        }

        [Display(Name = "Nom du restaurant")]
        [Required(ErrorMessage = "Veuillez saisir le nom du restaurant")]
        [StringLength(50, ErrorMessage = "Le nom du restaurant ne doit pas dépasser 50 caractères.")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [Display(Name = "Adresse du restaurant")]
        [Required(ErrorMessage = "Veuillez saisir l'adresse du restaurant")]
        [RegularExpression("^\\d{1,5}\\s[A-Za-z0-9À-ÿ\\s'.-]+,\\s?\\d{4,6}\\s[A-Za-zÀ-ÿ\\s'-]+$",
        ErrorMessage = "L'adresse doit être au format 'numéro de rue, code postal ville' (ex: 123 rue de la Paix, 75001 Paris)")]
        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        [Display(Name = "Type de cuisine")]
        [Required(ErrorMessage = "Veuillez saisir le type de cuisine")]
        public string CuisineType
        {
            get { return cuisineType; }
            set { cuisineType = value; }
        }

        [Display(Name = "Description du restaurant")]
        [Required(ErrorMessage = "Veuillez saisir une description")]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        [Display(Name = "Numéro de téléphone")]
        [Required(ErrorMessage = "Veuillez saisir le numéro de téléphone")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Le numéro de téléphone doit contenir 10 chiffres.")]
        public string PhoneNumber
        {
            get { return phoneNumber; }
            set { phoneNumber = value; }
        }

        [Display(Name = "Heures d'ouverture")]
        [Required(ErrorMessage = "Veuillez saisir les heures d'ouverture")]
        public TimeSpan OpeningHours
        {
            get { return openingHours; }
            set { openingHours = value; }
        }

        [Display(Name = "Heures de fermeture")]
        [Required(ErrorMessage = "Veuillez saisir les heures de fermeture")]
        public TimeSpan ClosingHours
        {
            get { return closingHours; }
            set { closingHours = value; }
        }

        public Restaurant()
        {
            meals = new List<Meal>();
            services = new List<Service>();
        }

        public Restaurant(int restaurantId,string name, string address, string cuisineType, string description, string phoneNumber, TimeSpan openingHours, TimeSpan closingHours,Service s)
        {
            this.restaurantId = restaurantId;
            this.name = name;
            this.address = address;
            this.cuisineType = cuisineType;
            this.description = description;
            this.phoneNumber = phoneNumber;
            this.openingHours = openingHours;
            this.closingHours = closingHours;
            meals = new List<Meal>();
            services = new List<Service>();
            AddService(s);
        }

        public void AddService(Service service)
        {
            if (service != null)
            {
                services.Add(service);
            }
        }
        public void AddMeal(Meal meal)
        {
            if (meal != null)
            {
                meals.Add(meal);
            }
        }

        public static async Task<Restaurant?> GetRestaurantAsync(int restaurantId, IRestaurantDAL dal)
        {
            return await dal.GetRestaurantWithServicesByIdAsync(restaurantId);
        }

        public async Task<(bool, int)> AddRestaurantAsync(int accountId, Service s, IRestaurantDAL dal)
        {
            return await dal.InsertRestaurantWithServiceAsync(this, accountId, s);
        }

        public static async Task<List<Restaurant>> GetRestaurantsByAccountIDAsync(int accountId, IRestaurantDAL dal)
        {
            return await dal.GetRestaurantByAccountIdAsync(accountId);
        }

        public static async Task<List<Restaurant>> GetRestaurantsAsync(IRestaurantDAL dal)
        {
            return await dal.GetRestaurantsAsync();
        }
    }
}
