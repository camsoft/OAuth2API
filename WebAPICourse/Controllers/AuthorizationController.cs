using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using WebAPICourse.Data;
using WebAPICourse.Models;

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace WebAPICourse.Controllers
{
    // Implements the OAuth 2.0 Authorization Server endpoints (/connect/authorize and
    // /connect/token) using OpenIddict. This replaces the old AuthController.Login
    // action: instead of our own code issuing a JWT directly, we now follow the
    // standard Authorization Code + PKCE flow - the SPA redirects the user here to
    // sign in, and then exchanges the resulting code for tokens at /connect/token.
    [ApiController]
    [AllowAnonymous]
    public class AuthorizationController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthorizationController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET /connect/authorize - shows a simple login form so the resource owner
        // (the student logging into the SPA) can authenticate with this Authorization
        // Server. The original OAuth request parameters (client_id, redirect_uri,
        // scope, state, code_challenge, etc.) are preserved in the query string so
        // they're still present when the form posts back below.
        [HttpGet("~/connect/authorize")]
        public IActionResult Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

            // OpenIddict reads request parameters from the query string for GET but
            // from the form body for POST - it does NOT fall back to the query string
            // on POST. So even though the original OAuth parameters are preserved in
            // the form's "action" URL, they must also be resubmitted as hidden fields
            // so AuthorizePost() below can see client_id, redirect_uri, etc.
            //
            // The markup/colors below intentionally mirror the OAuth2UI SPA's design
            // tokens (see OAuth2UI/src/index.css and LoginForm.css) so this
            // server-rendered page doesn't feel out of place when the browser is
            // redirected here from the React app.
            var html = $$"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Sign in - WebAPICourse</title>
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <style>
                        :root {
                            --text: #6b6375;
                            --text-h: #08060d;
                            --bg: #fff;
                            --border: #e5e4e7;
                            --accent: #aa3bff;
                            --shadow: rgba(0, 0, 0, 0.1) 0 10px 15px -3px, rgba(0, 0, 0, 0.05) 0 4px 6px -2px;
                            --sans: system-ui, "Segoe UI", Roboto, sans-serif;
                        }

                        @media (prefers-color-scheme: dark) {
                            :root {
                                --text: #9ca3af;
                                --text-h: #f3f4f6;
                                --bg: #16171d;
                                --border: #2e303a;
                                --accent: #c084fc;
                                --shadow: rgba(0, 0, 0, 0.4) 0 10px 15px -3px, rgba(0, 0, 0, 0.25) 0 4px 6px -2px;
                            }
                        }

                        * {
                            box-sizing: border-box;
                        }

                        body {
                            margin: 0;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            min-height: 100vh;
                            font: 16px/145% var(--sans);
                            color: var(--text);
                            background: var(--bg);
                        }

                        .login-form {
                            display: flex;
                            flex-direction: column;
                            gap: 8px;
                            width: 100%;
                            max-width: 340px;
                            margin: 0 auto;
                            padding: 32px;
                            border: 1px solid var(--border);
                            border-radius: 12px;
                            background: var(--bg);
                            box-shadow: var(--shadow);
                        }

                        .login-form h1 {
                            margin: 0 0 16px;
                            font-size: 24px;
                            letter-spacing: -0.24px;
                            color: var(--text-h);
                            text-align: center;
                        }

                        .login-form label {
                            font-size: 14px;
                            color: var(--text-h);
                            font-weight: 600;
                        }

                        .login-form input[type="text"],
                        .login-form input[type="password"] {
                            padding: 10px 12px;
                            margin-bottom: 8px;
                            border: 1px solid var(--border);
                            border-radius: 8px;
                            font: inherit;
                            color: var(--text-h);
                            background: var(--bg);
                        }

                        .login-form input:focus-visible {
                            outline: 2px solid var(--accent);
                            outline-offset: 1px;
                        }

                        .login-form button {
                            margin-top: 12px;
                            padding: 10px 16px;
                            border-radius: 8px;
                            border: none;
                            background: var(--accent);
                            color: #fff;
                            font-weight: 600;
                            cursor: pointer;
                        }

                        .login-form__hint {
                            margin-top: 16px;
                            font-size: 12px;
                            text-align: center;
                            color: var(--text);
                        }

                        .login-form__hint code {
                            font-family: ui-monospace, Consolas, monospace;
                        }
                    </style>
                </head>
                <body>
                    <form class="login-form" method="post" action="{{Request.Path}}{{Request.QueryString}}">
                        <h1>Sign in to WebAPICourse</h1>
                        <input type="hidden" name="client_id" value="{{request.ClientId}}" />
                        <input type="hidden" name="response_type" value="{{request.ResponseType}}" />
                        <input type="hidden" name="redirect_uri" value="{{request.RedirectUri}}" />
                        <input type="hidden" name="scope" value="{{request.Scope}}" />
                        <input type="hidden" name="state" value="{{request.State}}" />
                        <input type="hidden" name="code_challenge" value="{{request.CodeChallenge}}" />
                        <input type="hidden" name="code_challenge_method" value="{{request.CodeChallengeMethod}}" />
                        <label for="username">Username</label>
                        <input id="username" type="text" name="username" autocomplete="username" required />
                        <label for="password">Password</label>
                        <input id="password" type="password" name="password" autocomplete="current-password" required />
                        <button type="submit">Sign in</button>
                        <p class="login-form__hint">
                            Try <code>admin</code> / <code>Admin123!</code> or <code>member</code> / <code>Member123!</code>
                        </p>
                    </form>
                </body>
                </html>
                """;

            return Content(html, "text/html");
        }

        // POST /connect/authorize - handles the login form submission above. On
        // success, signs in with a principal describing the authenticated user and
        // their granted scopes; OpenIddict then redirects back to the client with an
        // authorization code.
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AuthorizePost([FromForm] string username, [FromForm] string password)
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user is null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid username or password.",
                    }));
            }

            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParametersAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, user.Id.ToString())
                    .SetClaim(Claims.Name, user.Username)
                    .SetClaim(Claims.Role, user.Role);

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(static claim => claim.Type switch
            {
                Claims.Name or Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken],
                _ => [Destinations.AccessToken],
            });

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // POST /connect/token - exchanges an authorization code (or refresh token)
        // for an access token. OpenIddict already validated the code/PKCE verifier
        // before this action runs; we just re-sign-in with the principal that was
        // stored alongside the original authorization code/refresh token.
        [HttpPost("~/connect/token")]
        [Consumes("application/x-www-form-urlencoded")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                if (result?.Principal is not ClaimsPrincipal principal)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The token is no longer valid.",
                        }));
                }

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new NotImplementedException("The specified grant type is not implemented.");
        }

        // GET/POST /connect/logout - ends the authenticated session and revokes the
        // tokens associated with it (OpenIddict automatically revokes the ad-hoc
        // authorizations/tokens tied to the signed-in session as part of SignOut).
        // Clients also have a dedicated POST /connect/revoke endpoint (handled
        // internally by OpenIddict, no controller action needed) to explicitly
        // revoke a specific access/refresh token, e.g. on manual "log out" clicks.
        [HttpGet("~/connect/logout")]
        [HttpPost("~/connect/logout")]
        [IgnoreAntiforgeryToken]
        public IActionResult Logout()
        {
            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    // Sends the user back to the SPA once the session has ended.
                    RedirectUri = "/",
                });
        }

        private const string TokenValidationParametersAuthenticationType = "WebAPICourse.Oidc";
    }
}
