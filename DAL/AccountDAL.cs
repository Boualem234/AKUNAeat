using AKUNAeat.Interfaces;
using AKUNAeat.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Identity;


namespace AKUNAeat.DAL
{
    public class AccountDAL : IAccountDAL
    {
        private string connectionString;
        private readonly PasswordHasher<Account> _passwordHasher;

        public AccountDAL(string connectionString)
        {
            this.connectionString = connectionString;
            _passwordHasher = new PasswordHasher<Account>();
        }

        public async Task<(bool,int)> InsertCustomerAsync(CustomerAccount account)
        {
            int accountId = await InsertAccountAsync(account);

            if (accountId == 0) return (false, 0);

            string query = "INSERT INTO CustomerAccount (account_id, address) VALUES (@account_id, @address)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("account_id", accountId);
                cmd.Parameters.AddWithValue("address", account.Adress);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return (res > 0, accountId);
            }
        }

        public async Task<(bool, int)> InsertRestaurantOwnerAsync(RestaurantOwnerAccount account)
        {
            int accountId = await InsertAccountAsync(account);
            if (accountId == 0) return (false, 0);

            string query = "INSERT INTO RestaurantOwnerAccount (account_id) VALUES (@account_id)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("account_id", accountId);

                await connection.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();

                return (res > 0, accountId);
            }
        }

        private async Task<int> InsertAccountAsync(Account account)
        {
            // Hasher le mot de passe avant insertion
            string hashedPassword = _passwordHasher.HashPassword(account, account.Password);

            int insertedId = 0;
            string query = @"INSERT INTO Account (firstName, lastName, email, password) OUTPUT INSERTED.account_id 
                     VALUES (@firstName, @lastName, @email, @password)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("firstName", account.FirstName);
                cmd.Parameters.AddWithValue("lastName", account.LastName);
                cmd.Parameters.AddWithValue("email", account.Email);
                cmd.Parameters.AddWithValue("password", hashedPassword);

                await connection.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();
                insertedId = Convert.ToInt32(result);
            }

            return insertedId;
        }


        public async Task<(bool, int)> FindAccountAsync(string email, string password, bool isCustomer)
        {
            bool isFind = false;
            string query = "";
            int accountId = 0;
            string? storedHash = null;

            if (isCustomer)
            {
                query = @"SELECT a.account_id, a.password FROM Account AS a 
                  JOIN CustomerAccount AS ca ON a.account_id = ca.account_id
                  WHERE a.email = @Email";
            }
            else
            {
                query = @"SELECT a.account_id, a.password FROM Account AS a 
                  JOIN RestaurantOwnerAccount AS ra ON a.account_id = ra.account_id
                  WHERE a.email = @Email";
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("Email", email);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        accountId = (int)reader["account_id"];
                        storedHash = reader["password"] as string;
                    }
                }
            }

            if (!string.IsNullOrEmpty(storedHash))
            {
                // --- Partie à garder en production ---
                // Si le mot de passe stocké ressemble à un hash (commence par "AQAAAA"), on utilise la vérification sécurisée
                if (storedHash.StartsWith("AQAAAA"))
                {
                    var result = _passwordHasher.VerifyHashedPassword(null, storedHash, password);
                    isFind = result == PasswordVerificationResult.Success;
                }
                else
                {
                    // --- Fin partie production ---

                    // --- Partie à utiliser uniquement pour les tests/démonstrations ---
                    // Sinon, on compare en clair (uniquement pour permettre la connexion avec des comptes injectés par script)
                    // À SUPPRIMER en production !
                    isFind = storedHash == password;
                }

            }

            return (isFind, isFind ? accountId : 0);
        }




        public async Task<bool> DoesEmailExistAsync(string email)
        {
            string query = "SELECT CASE WHEN EXISTS (SELECT 1 FROM Account WHERE email = @Email) THEN 1 ELSE 0 END";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("Email", email);

                await connection.OpenAsync();

                int exists = (int)await cmd.ExecuteScalarAsync();

                return exists == 1;
            }
        }

        public async Task<CustomerAccount?> GetCustomerByIdAsync(int customerId)
        {
            CustomerAccount? customer = null;

            string query = @"SELECT a.account_id, a.firstName, a.lastName, a.email, a.password, ca.address
                            FROM Account AS a
                            JOIN CustomerAccount AS ca ON a.account_id = ca.account_id
                            WHERE ca.account_id = @account_id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@account_id", customerId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int account_id = reader.GetInt32("account_id");
                        string firstName = reader.GetString("firstName");
                        string lastName = reader.GetString("lastName");
                        string email = reader.GetString("email");
                        string password = reader.GetString("password");
                        string address = reader.GetString("address");

                        customer = new CustomerAccount(account_id, firstName, lastName, email, password, address);
                    }
                }
            }

            return customer;
        }

        public async Task<RestaurantOwnerAccount?> GetRestaurantOwnerAccountAsync(int accountId)
        {
            RestaurantOwnerAccount? restaurantOwner = null;

            string query = @"SELECT a.account_id, a.firstName, a.lastName, a.email, a.password
                            FROM Account AS a
                            JOIN RestaurantOwnerAccount AS ra ON a.account_id = ra.account_id
                            WHERE ra.account_id = @account_id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@account_id", accountId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int account_id = reader.GetInt32("account_id");
                        string firstName = reader.GetString("firstName");
                        string lastName = reader.GetString("lastName");
                        string email = reader.GetString("email");
                        string password = reader.GetString("password");

                        restaurantOwner = new RestaurantOwnerAccount(account_id, firstName, lastName, email, password);
                    }
                }
            }

            return restaurantOwner;
        }

    }
}
