using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Security;

public interface IAccessTokenGenerator
{
    AccessToken Generate(Customer customer);
}
