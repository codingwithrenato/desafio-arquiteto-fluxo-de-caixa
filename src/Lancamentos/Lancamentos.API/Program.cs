using Lancamentos.API.Endpoints;
using Lancamentos.API.Extensions;
using Lancamentos.Application;
using Lancamentos.Infrastructure;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedKernel.Observability;
using SharedKernel.Security;
using SharedKernel.Web;

var builder = WebApplication.CreateBuilder(args);

// Observabilidade estruturada.
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .Enrich.FromLogContext());

// Camadas (Clean Architecture).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Segurança (JWT) + autorização.
builder.Services.AddJwtAuthentication(builder.Configuration);

// Observabilidade: tracing distribuído (OpenTelemetry → OTLP/Jaeger).
builder.Services.AddObservability(builder.Configuration, "lancamentos-api");

// Tratamento global de exceções (ProblemDetails).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health check do Lançamentos depende APENAS do seu próprio banco.
// Intencionalmente NÃO inclui o RabbitMQ: a disponibilidade do Lançamentos não
// pode depender do broker (requisito não-funcional central do desafio). Se o broker
// cair, os eventos acumulam no outbox e o serviço continua aceitando lançamentos.
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("LancamentosDb")!, name: "postgres");

// OpenAPI / Swagger com suporte a Bearer.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Serviço de Lançamentos", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token JWT (sem o prefixo 'Bearer')."
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

// Aplica migrations automaticamente (conveniência para rodar localmente).
await app.MigrateLancamentosDatabaseAsync();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDevAuthEndpoints();
app.MapLancamentosEndpoints();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Necessário para os testes de integração (WebApplicationFactory).
public partial class Program;
