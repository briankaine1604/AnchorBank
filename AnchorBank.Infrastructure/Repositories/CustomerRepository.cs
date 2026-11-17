using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Database;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AnchorBank.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public CustomerRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> TryCreateAsync(Customer customer)
        {
            using var connection = _connectionFactory.CreateConnection(); //using ensures the connection is disposed of when it leaves its scope, even if an exception occurs.
            const string sql = """
            INSERT INTO Customers (Id, FirstName, LastName, Email, PhoneNumber, PasswordHash)
            VALUES (@Id, @FirstName, @LastName, @Email, @PhoneNumber, @PasswordHash);
            """;

            try
            {
                await connection.ExecuteAsync(sql, customer);
                return true;
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            {
                // The unique index on Email is the source of truth for duplicates.
                return false;
            }
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT Id, FirstName, LastName, Email, PhoneNumber, PasswordHash
                FROM Customers
                WHERE Email = @Email;
                """;
            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Email = email });
        }

        public async Task<Customer?> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = """
                SELECT Id, FirstName, LastName, Email, PhoneNumber, PasswordHash
                FROM Customers
                WHERE Id = @Id;
                """;
            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
        }
    }
}
