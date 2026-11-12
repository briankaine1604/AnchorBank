using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Database;
using Dapper;

namespace AnchorBank.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public CustomerRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateAsync(Customer customer)
        {
            using var connection = _connectionFactory.CreateConnection(); //using ensures the connection is disposed of when it leaves its scope, even if an exception occurs.
            const string sql = """
            INSERT INTO Customers (Id, FirstName, LastName, Email, PhoneNumber)
            VALUES (@Id, @FirstName, @LastName, @Email, @PhoneNumber);
            """;

            await connection.ExecuteAsync(sql, customer);

        }

        public async Task<Customer?> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = """"
                SELECT * FROM Customers WHERE Id = @Id;
                """";
            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
        }
    }
}
