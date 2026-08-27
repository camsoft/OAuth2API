namespace WebAPICourse.Models
{
    // The shape of the JSON body clients send to POST /api/auth/login.
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
