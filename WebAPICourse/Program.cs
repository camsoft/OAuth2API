using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPICourse.Data;
using WebAPICourse.Models;
using WebAPICourse.Repositories;
using WebAPICourse.Services;

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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Program.cs
// NOTE: The repository is now Scoped (not Singleton) because it depends on AppDbContext,
// which is itself Scoped. A Singleton service cannot safely depend on a Scoped service.
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Bearer authentication - validates tokens issued by our own TokenService
// using a locally configured signing key (no external Authority/OAuth provider).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
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
