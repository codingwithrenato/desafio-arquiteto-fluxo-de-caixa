using Consolidado.Application;
using Consolidado.Infrastructure;
using Consolidado.Worker.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .Enrich.FromLogContext());

// Camadas + consumidor da fila (este host É o job-worker, separado da read-API).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddConsumidorDeLancamentos();

// Hangfire com storage no PostgreSQL.
var hangfireConnection = builder.Configuration.GetConnectionString("HangfireDb");
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<ConsolidadoRecurringJobs>();

// Health check do worker: depende do seu banco.
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ConsolidadoDb")!, name: "postgres");

var app = builder.Build();

app.UseSerilogRequestLogging();

// Dashboard do Hangfire (observabilidade dos jobs).
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorization()]
});

// Agenda os jobs recorrentes.
var cronFechamento = app.Configuration.GetValue<string>("Hangfire:CronFechamentoDiario") ?? "5 0 * * *";
var cronReconciliacao = app.Configuration.GetValue<string>("Hangfire:CronReconciliacao") ?? "*/15 * * * *";

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<ConsolidadoRecurringJobs>(
    ConsolidadoRecurringJobs.FechamentoDiarioId,
    job => job.FecharDiaAnteriorAsync(),
    cronFechamento);
recurringJobs.AddOrUpdate<ConsolidadoRecurringJobs>(
    ConsolidadoRecurringJobs.ReconciliacaoId,
    job => job.ReconciliarAsync(),
    cronReconciliacao);

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/hangfire")).ExcludeFromDescription();

app.Run();
