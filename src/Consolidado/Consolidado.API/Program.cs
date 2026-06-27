using System.Threading.RateLimiting;
using Consolidado.API.Endpoints;
using Consolidado.API.Extensions;
using Consolidado.Application;
using Consolidado.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedKernel.Security;
using SharedKernel.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Rate limiting (load shedding) no endpoint de leitura. Protege o serviço no pico de
// 50 req/s: requisições acima do limite recebem 429 imediatamente (descarte controlado),
// honrando a tolerância de até 5% de perda em vez de degradar todo o serviço.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("consolidado-read", limiter =>
    {
        limiter.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitPerSecond", 100);
        limiter.Window = TimeSpan.FromSeconds(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 50);
    });
});

// Health check inclui Postgres e Redis (dependências do caminho de LEITURA).
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ConsolidadoDb")!, name: "postgres")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Serviço de Consolidado Diário", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await app.MigrateConsolidadoDatabaseAsync();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDevAuthEndpoints();
app.MapConsolidadoEndpoints();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public partial class Program;
