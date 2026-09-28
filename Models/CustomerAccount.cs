using AKUNAeat.Interfaces;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Models
{
    public class CustomerAccount : Account
    {
        private string adress;
            
        private List<Order> orders;

        public List<Order> Orders
        {
            get { return orders; }
            set { orders = value; }
        }

        [Required(ErrorMessage = "Veuillez saisir une adresse")]
        [Display(Name = "Adresse du client")]
        [StringLength(100, ErrorMessage = "L'adresse ne doit pas dépasser 100 caractères.")]
        [RegularExpression("^\\d{1,5}\\s[A-Za-z0-9À-ÿ\\s'.-]+,\\s?\\d{4,6}\\s[A-Za-zÀ-ÿ\\s'-]+$",
        ErrorMessage = "L'adresse doit être au format 'numéro de rue, code postal ville' (ex: 123 rue de la Paix, 75001 Paris)")]
        public string Adress
        {
            get { return adress; }
            set { adress = value; }
        }

        public CustomerAccount() 
        {
            orders = new List<Order>();
        }

        public CustomerAccount(int accountId,string firstName, string lastName, string email, string password, string adress) : base(accountId,firstName,lastName,email,password)
        {
            this.adress = adress;
            this.orders = new List<Order>();
        }

        public async override Task<(bool, int)> CreateAccountAsync(IAccountDAL dal)
        {
            return await dal.InsertCustomerAsync(this);
        }

        public static async Task<(bool, int)> FindCustomerAccountAsync(string email, string password ,IAccountDAL dal)
        {
            return await dal.FindAccountAsync(email,password,true);
        }

        public static async Task<CustomerAccount?> GetCustomerAsync(int customerId, IAccountDAL dal)
        {
            return await dal.GetCustomerByIdAsync(customerId);
        }
    }
}
