using AnchorBank.Domain.Entities;

namespace AnchorBank.Domain.Tests
{
    public class CustomerTests
    {
        [Fact]
        public void TrimsWhitespace()
        {

            var customer = new Customer(
            "  Brian  ",
            "  Ikeogu  ",
            "  brian@example.com  ",
            "  +234****5678  ",
            "PBKDF2-SHA256$600000$c2FsdA==$aGFzaA==");

            Assert.Equal("Brian", customer.FirstName);
            Assert.Equal("Ikeogu", customer.LastName);
            Assert.Equal("brian@example.com", customer.Email);
            Assert.Equal("+234****5678", customer.PhoneNumber);

        }

        [Fact]
        public void RejectsEmptyFirstName()
        {
            // Arrange
            string firstName = "";
            string lastName = "Ikeogu";
            string email = "brian@example.com";
            string phoneNumber = "+234****5678";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new Customer(firstName, lastName, email, phoneNumber, "PBKDF2-SHA256$600000$c2FsdA==$aGFzaA=="));
        }

        [Fact]
        public void RejectsMissingPasswordHash()
        {
            Assert.Throws<ArgumentException>(() =>
                new Customer("Brian", "Ikeogu", "brian@example.com", "+234****5678", ""));
        }
    }
}
