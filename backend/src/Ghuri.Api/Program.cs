using Ghuri.Api.ErrorHandling;
using Ghuri.Application;
using Ghuri.Infrastructure;
using Serilog;

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
// instead of ASP.NET Core's several noisy lines.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
