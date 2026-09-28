using AKUNAeat.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Models
{
    public abstract class Account
    {
        private int accountId;
        private string firstName;
        private string lastName;
        private string email;
        private string password;

        public int AccountId
        {
            get { return accountId; }
            set { accountId = value; }
        }

        
        [Required(ErrorMessage = "Veuillez saisir un prénom")]
        [Display(Name = "Prénom")]
        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }


        [Required(ErrorMessage = "Veuillez saisir un Nom")]
        [Display(Name = "Nom")]
        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Veuillez saisir une Adresse mail")]
        [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
        [Display(Name = "Adresse mail")]

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        [Required(ErrorMessage = "Veuillez saisir mot de passe")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Le mot de passe doit contenir au moins 5 caractères.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Le mot de passe doit contenir au moins une majuscule et un chiffre.")]
        [Display(Name = "Mot de passe")]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public Account(int accountId,string firstName, string lastName, string email, string password)
        {
            this.accountId = accountId;
            this.firstName = firstName;
            this.lastName = lastName;
            this.email = email;
            this.password = password;
        }

        public Account() { }

        public static Task<bool> DoesEmailExistAsync(string email, IAccountDAL dal)
        {
            return dal.DoesEmailExistAsync(email);
        }

        public abstract Task<(bool, int)> CreateAccountAsync(IAccountDAL dal);

    }
}
