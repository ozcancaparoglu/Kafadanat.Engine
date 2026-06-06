using Domain;
using Microsoft.Extensions.DependencyInjection;
using Onspay.Cqrs.Behaviors;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMultiCriteriaDecisionEngine, MultiCriteriaDecisionEngine>();
        services.AddCqrs(typeof(AssemblyMarker).Assembly);
        return services;
    }
}
