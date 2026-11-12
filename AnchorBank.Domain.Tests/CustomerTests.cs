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
            "  +2348012345678  ");

            Assert.Equal("Brian", customer.FirstName);
            Assert.Equal("Ikeogu", customer.LastName);
            Assert.Equal("brian@example.com", customer.Email);
            Assert.Equal("+2348012345678", customer.PhoneNumber);

        }

        [Fact]
        public void RejectsEmptyFirstName()
        {
            // Arrange
            string firstName = "";
            string lastName = "Ikeogu";
            string email = "brian@example.com";
            string phoneNumber = "+2348012345678";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new Customer(firstName, lastName, email, phoneNumber));
        }
    }
}
