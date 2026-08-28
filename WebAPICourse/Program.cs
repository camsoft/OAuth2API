using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using WebAPICourse.Data;
using WebAPICourse.Models;
using WebAPICourse.Repositories;
using WebAPICourse.Services;

using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the EF Core DbContext, pointing it at SQL Server LocalDB using the
// "DefaultConnection" connection string from appsettings.json.
// AddDbContext registers AppDbContext as a "Scoped" service by default, meaning
// a new instance is created for each incoming HTTP request.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // Registers the OpenIddict entity sets (applications, authorizations, scopes,
    // tokens) with this DbContext so the Authorization Server can persist OAuth 2.0
    // data using the same EF Core provider/connection as the rest of the app.
    options.UseOpenIddict();
});

// Program.cs
// NOTE: The repository is now Scoped (not Singleton) because it depends on AppDbContext,
// which is itself Scoped. A Singleton service cannot safely depend on a Scoped service.
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// OAuth 2.0 Authorization Server + Resource Server, both hosted in this same API,
// using OpenIddict. Replaces the previous hand-rolled JwtBearer/TokenService setup:
// - AddCore wires OpenIddict's application/authorization/scope/token stores to
//   our existing AppDbContext (see AppDbContext.OnModelCreating -> UseOpenIddict()).
// - AddServer configures the standard OAuth 2.0 endpoints and enables the
//   Authorization Code + PKCE flow (for the SPA client) plus refresh tokens.
// - AddValidation lets this same API validate the access tokens it just issued,
//   acting as the Resource Server for the Products/Categories controllers.
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetRevocationEndpointUris("connect/revoke")
               .SetEndSessionEndpointUris("connect/logout");

        // Authorization Code + PKCE is the recommended flow for our SPA (a public
        // client with no client secret). Refresh tokens let the SPA silently renew
        // its access token instead of forcing the user through the login screen again.
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess);

        // Development-only signing/encryption certificates - generated on the fly
        // and NOT suitable for production (mirrors the old "dev-only" Jwt:Key).
        // In production, register real X.509 certificates instead.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        // Since the Authorization Server lives in this same process, we can validate
        // tokens locally instead of introspecting them against a remote endpoint.
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();

// The UI now runs as a completely separate project/origin (e.g. http://localhost:5173
// during "npm run dev"), so the browser enforces CORS on every request. This policy
// allows only the origins listed under "Cors:AllowedOrigins" in appsettings.json to
// call this API. We don't need AllowCredentials() here because the JWT is sent as a
// normal "Authorization: Bearer <token>" header, not as a cookie.
const string UiCorsPolicy = "UiCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(UiCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Automatically apply any pending EF Core migrations at startup.
// This is convenient for a learning project so students don't have to remember to run
// "dotnet ef database update" manually - the database and tables are created/updated
// the first time the app runs. In a production application you would typically apply
// migrations as part of your deployment pipeline instead of on every app startup.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    // Seed 2 demo users the first time the app runs, if the Users table is empty.
    // Passwords are hashed here (not stored in HasData) because PasswordHasher
    // generates a random salt each time it runs.
    if (!dbContext.Users.Any())
    {
        var passwordHasher = new PasswordHasher<User>();

        var admin = new User { Username = "admin", Role = "Admin", PasswordHash = string.Empty };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");

        var member = new User { Username = "member", Role = "Member", PasswordHash = string.Empty };
        member.PasswordHash = passwordHasher.HashPassword(member, "Member123!");

        dbContext.Users.AddRange(admin, member);
        dbContext.SaveChanges();
    }

    // Register the SPA as a public OAuth 2.0 client (Authorization Code + PKCE,
    // no client secret since it can't keep one confidential in the browser).
    // This runs on every startup but is idempotent - it only creates the
    // application registration if it doesn't already exist.
    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var spaClientConfig = builder.Configuration.GetSection("OAuthClients:Spa");
    var spaClientId = spaClientConfig["ClientId"]!;

    if (await applicationManager.FindByClientIdAsync(spaClientId) is null)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = spaClientId,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Explicit,
            DisplayName = spaClientConfig["DisplayName"] ?? spaClientId,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + "offline_access",
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange,
            },
        };

        foreach (var redirectUri in spaClientConfig.GetSection("RedirectUris").Get<string[]>() ?? [])
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        foreach (var postLogoutRedirectUri in spaClientConfig.GetSection("PostLogoutRedirectUris").Get<string[]>() ?? [])
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri));
        }

        await applicationManager.CreateAsync(descriptor);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Shows the full exception/stack trace in the response instead of a bare 500,
    // so login/API failures are actionable during development.
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(UiCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
