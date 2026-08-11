using System.Reflection;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Common.Behaviors;

namespace StudentCouncil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR handlers (Commands/Queries) discovered from this assembly, wrapped in the
        // validation + logging pipeline. Order matters: validate before logging the handler run.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        // FluentValidation validators for the commands/queries above.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Mapster mapping configuration discovered from this assembly.
        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        return services;
    }
}
