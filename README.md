# WebAPICourse - OAuth 2.0 with OpenIddict

A training project for learning how to secure an ASP.NET Core Web API using **OAuth 2.0 / OpenID Connect** with the **Authorization Code + PKCE** flow, powered by [OpenIddict](https://documentation.openiddict.com/).

This repository hosts **both** the OAuth 2.0 Authorization Server and the protected API (Resource Server) in a single project, to keep the course setup simple. A companion React SPA (`OAuth2UI`) demonstrates how a browser-based client consumes this API.

## What this project demonstrates

- Replacing hand-rolled JWT login (`/api/auth/login` + a hardcoded signing key) with a spec-compliant OAuth 2.0 Authorization Server.
- The **Authorization Code + PKCE** flow, the recommended grant type for public clients (SPAs, mobile apps) that cannot keep a client secret confidential.
- Refresh tokens, so a client can silently renew its access token without forcing the user to log in again.
- Token revocation and logout (`/connect/revoke`, `/connect/logout`).
- Role-based authorization (`Admin` vs `Member`) enforced on API endpoints via `[Authorize(Roles = "...")]`.
- A server-rendered sign-in page, styled to match the companion SPA, illustrating that OAuth clients never see the user's password directly.

## Tech stack

- **.NET 11** / ASP.NET Core Web API
- **OpenIddict** (`OpenIddict.AspNetCore`, `OpenIddict.EntityFrameworkCore`) - OAuth 2.0 / OIDC server implementation
- **Entity Framework Core** with **SQL Server** (LocalDB by default)
- **Swagger / OpenAPI** for exploring the non-OAuth endpoints

## Project structure

```
WebAPICourse/
  Controllers/
	AuthorizationController.cs   OAuth 2.0 endpoints: /connect/authorize, /connect/token, /connect/logout
	ProductsController.cs        Example protected resource (CRUD), some actions require [Authorize]
	CategoriesController.cs      Example protected resource
  Data/
	AppDbContext.cs               EF Core DbContext (app entities + OpenIddict's own tables)
  Migrations/                     EF Core migrations, applied automatically on startup
  Models/                         Domain models (Product, Category, User)
  Repositories/ & Services/       Simple repository/service layers backing the controllers
  Program.cs                      OpenIddict server/validation configuration, CORS, DB seeding
  appsettings.json                Connection string, OAuth client registration, CORS origins
  WebAPICourse.http                Manual request examples, including the full OAuth flow
```

## Prerequisites

- [.NET 11 SDK (preview)](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio, or via SQL Server Express/Developer edition)
- Visual Studio 2026 (or any editor of your choice) with the ASP.NET and web development workload
- (Optional, for the full end-to-end experience) the companion SPA project, **OAuth2UI** (React + Vite + TypeScript)

## Getting started

1. **Clone the repository.**

2. **Restore and build.**

   ```powershell
   cd WebAPICourse
   dotnet restore
   dotnet build
   ```

3. **Run the API.** The Authorization Server + Resource Server are one and the same app:

   ```powershell
   dotnet run --launch-profile https
   ```

   Or, from Visual Studio, select the **https** launch profile and press F5.

   On first run, the app will:
   - Apply all pending EF Core migrations (creating the database if it doesn't exist).
   - Seed two demo users (see below).
   - Register the SPA as an OAuth client (see `OAuthClients:Spa` in `appsettings.json`).

4. The API will be available at:
   - `https://localhost:7257`
   - `http://localhost:5201`

   Swagger UI is available at `/swagger` in the Development environment.

### Seeded demo users

| Username | Password    | Role   |
|----------|-------------|--------|
| `admin`  | `Admin123!` | Admin  |
| `member` | `Member123!`| Member |

## OAuth 2.0 endpoints

| Endpoint             | Purpose                                                             |
|-----------------------|----------------------------------------------------------------------|
| `GET /connect/authorize` | Starts the login flow; renders a sign-in form for the resource owner |
| `POST /connect/authorize`| Handles the sign-in form submission                                 |
| `POST /connect/token`    | Exchanges an authorization code (or refresh token) for tokens        |
| `POST /connect/revoke`   | Revokes an access or refresh token (used on logout)                  |
| `POST /connect/logout`   | Ends the authenticated session                                       |

See [`WebAPICourse.http`](WebAPICourse/WebAPICourse.http) for a full walkthrough of the Authorization Code + PKCE flow, including how to manually generate a PKCE `code_verifier`/`code_challenge` pair and exercise each endpoint with sample requests.

### Registered OAuth client (SPA)

Configured under `OAuthClients:Spa` in `appsettings.json`:

- **Client ID**: `webapicourse-spa`
- **Client type**: Public (no client secret - uses PKCE instead)
- **Grant types**: Authorization Code, Refresh Token
- **Redirect URIs**: `http://localhost:5173/callback`, `https://localhost:5173/callback`
- **Scopes**: `openid`, `profile`, `roles`, `offline_access`

If you change the SPA's dev server port, update `RedirectUris` / `PostLogoutRedirectUris` here (and `Cors:AllowedOrigins`) to match.

## Protected resource endpoints

- `GET /products`, `GET /products/{id}`, `GET /categories` - publicly readable.
- `POST /products` - requires any authenticated user (`[Authorize]`).
- Admin-only actions (e.g. deleting a product) - require the `Admin` role (`[Authorize(Roles = "Admin")]`).

Send the access token returned from `/connect/token` as a standard bearer token:

```
Authorization: Bearer <access_token>
```

## Configuration notes

- **Connection string** (`ConnectionStrings:DefaultConnection`) defaults to a local SQL Server LocalDB instance. Update this if you're using a different SQL Server instance.
- **CORS** (`Cors:AllowedOrigins`) only allows the SPA's dev server origins by default (`http://localhost:5173`, `https://localhost:5173`). Add any additional client origins here.
- **Development-only certificates**: OpenIddict is configured with `AddDevelopmentEncryptionCertificate()` / `AddDevelopmentSigningCertificate()`, which are auto-generated and **not suitable for production**. A production deployment should register real X.509 certificates instead.
- **`DisableCsrfProtection`**: this API is a pure bearer-token API (no cookies), so it isn't vulnerable to CSRF; this setting avoids false-positive antiforgery failures from .NET's automatic CSRF middleware.

## Companion SPA

A React + Vite + TypeScript SPA (`OAuth2UI`) demonstrates a public client consuming this API via the Authorization Code + PKCE flow (redirect-based login, `/callback` route, token storage in `sessionStorage`, and silent refresh). Point its `VITE_API_URL` environment variable at this API's base URL (`https://localhost:7257` by default) to run it end-to-end.

## Learning notes

This project intentionally combines the Authorization Server and Resource Server in one process for simplicity. In many real-world deployments, these are separate services (e.g. a dedicated identity provider like Entra ID, Auth0, or a standalone IdentityServer/OpenIddict instance, in front of one or more independent APIs). The core protocol concepts demonstrated here - Authorization Code + PKCE, token exchange, refresh, and revocation - apply the same way regardless of that split.
