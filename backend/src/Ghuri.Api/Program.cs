using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Api.RateLimiting;
using Ghuri.Application;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Infrastructure;
using Ghuri.Infrastructure.Storage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default logger. Settings (levels, where logs go)
// come from the "Serilog" section of appsettings.json, so changing log
// verbosity never needs a code change or redeploy of new code.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Each layer registers itself - Program.cs knows nothing about Mediator,
// FluentValidation or EF Core (blueprint: composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// "Who is the current user" is answered by the Api (it's the only layer
// that knows about HTTP). The audit interceptor in Infrastructure depends
// on this.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
// Same idea: "where is this request from?" (IP, browser) for new sessions.
builder.Services.AddScoped<IClientInfo, HttpClientInfo>();

// ProblemDetails (RFC 9457) as the JSON shape for EVERY error response,
// plus our handler for exceptions nobody else caught.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Security (see the files in Api/Authentication and Api/RateLimiting):
// check the JWT on every request, role-based policies for [Authorize],
// and per-IP limits on the sensitive auth endpoints.
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddRateLimitPolicies();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// "dotnet run --project src/Ghuri.Api -- seed": create the first Super
// Admin, then exit without starting the web server. A deliberate command,
// never automatic on startup - the same rule the blueprint sets for
// migrations (section 14.1). It also keeps the integration tests, which
// boot this Program with no database, from ever touching SQL Server.
if (args.Contains("seed"))
{
    await app.Services.SeedDatabaseAsync();
    return;
}

// First in the pipeline on purpose: it has to wrap everything after it to
// be able to catch their exceptions.
app.UseExceptionHandler();

// One concise log line per HTTP request (method, path, status, duration)
// instead of ASP.NET Core's several noisy lines. Successful /health calls
// are dropped to Verbose (hidden at our Information level): an uptime
// monitor polls every few seconds, and thousands of "health OK" lines a
// day would bury the logs that matter. A FAILING health check still logs.
// A request the BROWSER abandoned (tab closed, connection lost) is not our
// bug: its cancellation surfaces as an exception, but logging it as an
// Error would bury the real errors - so it's Information.
app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) =>
    httpContext.RequestAborted.IsCancellationRequested
        ? LogEventLevel.Information
    : exception is null
    && httpContext.Response.StatusCode < 400
    && httpContext.Request.Path.StartsWithSegments("/health")
        ? LogEventLevel.Verbose
        : httpContext.Response.StatusCode >= 500 || exception is not null
            ? LogEventLevel.Error
            : LogEventLevel.Information);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTP -> HTTPS redirect, but NOT in Development. Locally, the Vite dev
// proxy talks to the API over plain http://localhost:5176; if Visual
// Studio starts the "https" profile, this redirect would bounce every
// proxied call to https://localhost:7273 - a DIFFERENT origin, so the
// browser blocks it (CORS) and the page shows "API unreachable".
// In production HTTPS is enforced anyway (Nginx, Day 7/14).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Uploaded images at /files/... (see LocalDiskFileStorage). On the real
// server Nginx serves this folder itself and the request never reaches us;
// this makes local development - and a server without Nginx - work the same.
// Before the rate limiter and auth on purpose: catalogue images are public.
var storage = app.Services.GetRequiredService<IOptions<LocalDiskStorageOptions>>().Value;
Directory.CreateDirectory(storage.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    RequestPath = storage.PublicBaseUrl,
    OnPrepareResponse = context =>
    {
        // Every upload gets a brand-new name, so a file's content never
        // changes - browsers may keep it for a year without asking again.
        context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        // "Trust the Content-Type, don't guess" - a browser never treats an image as a page.
        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
    }
});

// ORDER MATTERS in this block:
// 1. Rate limiter first - the cheapest check. A flood is refused before
//    any work (JWT signature check, database) is spent on it.
// 2. Authentication - reads the "Authorization: Bearer ..." header, checks
//    the JWT, and fills HttpContext.User. It never refuses anything itself.
// 3. Authorization - NOW decides: [Authorize] endpoint and no valid user?
//    401. Wrong role for the policy? 403. It needs step 2's result, so it
//    must come after it.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoints (blueprint: "GET health/live · GET health/ready" and
// "uptime monitoring on /health"). Deliberately outside /api/v1 - they
// describe the running server, not a versioned business API.
//
// /health/live  - "is the process alive?" Runs NO checks (predicate false),
//                 so it answers even if the database is down. Used to
//                 decide "restart this app?" - restarting won't fix a
//                 database outage, so the DB must not affect this one.
// /health/ready - "can it serve real requests?" Runs the database check.
//                 Used to decide "send traffic here?".
// /health       - everything, for the uptime monitor (blueprint Day 14).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthCheckTags.Ready) });
app.MapHealthChecks("/health");

app.Run();
