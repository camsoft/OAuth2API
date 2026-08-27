namespace WebAPICourse.Models
{
    // A minimal user account used to authenticate against the API and issue JWTs.
    // This intentionally stays simple - just enough to demonstrate login + role checks.
    // It does NOT use ASP.NET Core Identity's full membership system (that's out of
    // scope for this beginner course).
    public class User
    {
        public int Id { get; set; }

        public required string Username { get; set; }

        // Never store plaintext passwords - this holds the output of
        // Microsoft.AspNetCore.Identity's PasswordHasher<User>.
        public required string PasswordHash { get; set; }

        // Simple role string used with [Authorize(Roles = "Admin")].
        // Expected values: "Admin" or "Member".
        public required string Role { get; set; }
    }
}
