using Application;
using Infrastructure;
using Onspay.Infrastructure.Logging;
using Onspay.Infrastructure.Logging.Elasticsearch;
using Serilog;
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Console as well as Elasticsearch, so `kubectl logs` shows errors without a port-forward to ES.
builder.Host.UseOnspayLogging((cfg, ctx) => cfg
    .WriteToElasticsearch(ctx)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health");

app.ApplyMigrations();

app.Run();
