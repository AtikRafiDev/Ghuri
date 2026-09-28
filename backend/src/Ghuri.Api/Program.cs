using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

// ProblemDetails (RFC 9457) as the JSON shape for EVERY error response,
// plus our handler for exceptions nobody else caught.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// First in the pipeline on purpose: it has to wrap everything after it to
// be able to catch their exceptions.
app.UseExceptionHandler();

// One concise log line per HTTP request (method, path, status, duration)
// instead of ASP.NET Core's several noisy lines. Successful /health calls
// are dropped to Verbose (hidden at our Information level): an uptime
// monitor polls every few seconds, and thousands of "health OK" lines a
// day would bury the logs that matter. A FAILING health check still logs.
app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) =>
    exception is null
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
