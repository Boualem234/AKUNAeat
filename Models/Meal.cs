using AKUNAeat.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AKUNAeat.Models
{
    public abstract class Meal
    {
        private int mealId;
        private string name;
        private decimal price;
        private Restaurant restaurant;

        public Restaurant Restaurant
        {
            get { return restaurant; }
            set { restaurant = value; }
        }

        public int MealId
        {
            get { return mealId; }
            set { mealId = value; }
        }

        [Display(Name = "Nom")]
        [Required(ErrorMessage = "Veuillez saisir le nom du meal")]
        [StringLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères.")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Le nom ne peut contenir que des lettres, des chiffres et des espaces.")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [Display(Name = "Prix")]
        [Required(ErrorMessage = "Veuillez saisir le prix du meal")]
        [Range(0.01, 10000.00, ErrorMessage = "Le prix doit être compris entre 0.01 et 10000.00.")]
        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        public override string ToString()
        {
            return $"{Name} - {Price:C}";
        }

        public Meal() { }

        public Meal(int meal_id,string name, decimal price,Restaurant r)
        {
            MealId = meal_id;
            Name = name;
            Price = price;
            Restaurant = r;
        }

        public static async Task<List<Meal>> GetMealsAsync(int restaurantId, IMealDAL mealDAL)
        {
            return await mealDAL.GetMealsAsync(restaurantId);
        }
    }
}
