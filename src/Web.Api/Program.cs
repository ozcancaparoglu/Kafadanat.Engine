using Application;
using Infrastructure;
using Onspay.Infrastructure.Logging;
using Onspay.Infrastructure.Logging.Elasticsearch;
using Serilog;
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOnspayLogging((cfg, ctx) => cfg.WriteToElasticsearch(ctx));

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
