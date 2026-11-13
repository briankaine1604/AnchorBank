namespace AnchorBank.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; private set; }

        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        public string Email { get; private set; }

        public string PhoneNumber { get; private set; }

        public string? PasswordHash { get; private set; }

        public Customer(string firstName, string lastName, string email, string phoneNumber, string passwordHash)
            : this(Guid.NewGuid(), firstName, lastName, email, phoneNumber, RequireHash(passwordHash))
        {
        }

        // passwordHash is null only for customers registered before passwords were stored.
        public Customer(Guid id, string firstName, string lastName, string email, string phoneNumber, string? passwordHash)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Customer ID must be provided", nameof(id));
            }

            Id = id;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("First name must be provided",nameof(firstName));
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException("Last name must be provided", nameof(lastName));
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email must be provided", nameof(email));
            }
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number must be provided", nameof(phoneNumber));
            }
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Email = email.Trim();
            PhoneNumber = phoneNumber.Trim();
            PasswordHash = passwordHash;
        }

        private static string RequireHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("Password hash must be provided", nameof(passwordHash));
            }

            return passwordHash;
        }
    }

    
}
