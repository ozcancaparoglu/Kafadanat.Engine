using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onspay.Infrastructure.Correlation;
using Onspay.Infrastructure.DomainEvents;
using Onspay.Infrastructure.Inbox;
using Onspay.Infrastructure.Messaging.RabbitMq;
using Onspay.Infrastructure.Outbox;
using Onspay.SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Missing connection string 'Database'.");

        services.AddCorrelation();
        services.AddDomainEvents();

        services.AddDbContext<EngineDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Message))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IInboxDbContext>(sp => sp.GetRequiredService<EngineDbContext>());
        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<EngineDbContext>());
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        services.AddOutboxProcessing(configuration);
        services.AddInboxProcessing(configuration);
        services.AddRabbitMqMessaging(configuration);

        return services;
    }
}
