using FluentValidation;
using Ghuri.Application.Behaviors;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Application;

/// <summary>
/// Registers everything the Application layer provides. Program.cs calls
/// this one method - it never needs to know Mediator or FluentValidation
/// exist (blueprint: "each layer registers itself via AddApplication /
/// AddInfrastructure").
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AddMediator is not in any library - Mediator's source generator
        // WRITES it at compile time, containing one registration per
        // handler it finds. No reflection scanning at startup.
        services.AddMediator((MediatorOptions options) =>
        {
            // Scoped = one instance per HTTP request. Handlers will use
            // repositories -> AppDbContext, which is scoped; a Singleton
            // handler holding a scoped DbContext would share one context
            // across every request at once (the library's default is
            // Singleton, so this line matters).
            options.ServiceLifetime = ServiceLifetime.Scoped;

            // ORDER MATTERS - first in the list is the OUTERMOST wrapper:
            //   Validation -> Logging -> Transaction -> Handler
            // Invalid input is rejected before anything is logged or any
            // transaction opens (blueprint section 7.2, pipeline order).
            options.PipelineBehaviors =
            [
                typeof(ValidationBehavior<,>),
                typeof(LoggingBehavior<,>),
                typeof(TransactionBehavior<,>)
            ];
        });

        // Finds every AbstractValidator<T> in this project and registers
        // it - adding a validator for a new command needs no extra wiring.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
