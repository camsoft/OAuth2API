using WebAPICourse.Models;

namespace WebAPICourse.Services
{
    // Responsible for turning an authenticated User into a signed JWT that the
    // client can send back on future requests (Authorization: Bearer {token}).
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
