using Hangfire.Dashboard;
using System.Net.Http.Headers;
using System.Text;

namespace BackendAPI.Infrastructure
{
    /// <summary>
    /// US-11 — Protects the Hangfire dashboard with HTTP Basic Auth in production.
    ///
    /// Config keys (appsettings.json / Azure App Settings):
    ///   Hangfire:DashboardUser      — username  (default: "admin")
    ///   Hangfire:DashboardPassword  — password  (CHANGE THIS before deploying)
    ///
    /// In Development the filter always returns true (no credentials needed).
    /// </summary>
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public HangfireDashboardAuthFilter(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env    = env;
        }

        public bool Authorize(DashboardContext context)
        {
            // Dev: always allow — no credentials required
            if (_env.IsDevelopment()) return true;

            var httpContext = context.GetHttpContext();

            // Read expected credentials from config
            var expectedUser = _config["Hangfire:DashboardUser"]     ?? "admin";
            var expectedPass = _config["Hangfire:DashboardPassword"]  ?? "";

            // If no password configured in prod — block access entirely (fail safe)
            if (string.IsNullOrWhiteSpace(expectedPass))
            {
                httpContext.Response.StatusCode = 403;
                return false;
            }

            // Parse Authorization: Basic <base64> header
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var encoded    = authHeader["Basic ".Length..].Trim();
                    var decoded    = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    var colonIndex = decoded.IndexOf(':');

                    if (colonIndex > 0)
                    {
                        var user = decoded[..colonIndex];
                        var pass = decoded[(colonIndex + 1)..];

                        if (user == expectedUser && pass == expectedPass)
                            return true;
                    }
                }
                catch
                {
                    // Malformed header — fall through to challenge
                }
            }

            // No valid credentials — send WWW-Authenticate challenge so the browser
            // shows a login dialog instead of a blank 401 page
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }
    }
}
