using AKUNAeat.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AKUNAeat.Models
{
    public class Dish : Meal
    {
        private string composition;

        [Display(Name = "Composition")]
        [Required(ErrorMessage = "Veuillez saisir la composition du plat")]
        [StringLength(200, ErrorMessage = "La composition ne peut pas dépasser 200 caractères.")]
        public string Composition
        {
            get { return composition; }
            set { composition = value; }
        }

        public Dish() { }

        public Dish(int id, string name, string composition)
        {
            MealId = id;
            Name = name;
            Composition = composition;
        }

        public Dish(int meal_id, string name, decimal price, Restaurant r, string composition) : base(meal_id, name, price, r)
        {
            Composition = composition;
        }

        public static async Task<List<Dish>> GetDishesAsync(int restaurantId, IMealDAL dal)
        {
            return await dal.GetDishesOfRestaurantAsync(restaurantId);
        }
        public async Task<bool> AddDishAsync(int restaurantId, IMealDAL dal)
        {
            return await dal.InsertDishAsync(this, restaurantId);
        }
    }
}
