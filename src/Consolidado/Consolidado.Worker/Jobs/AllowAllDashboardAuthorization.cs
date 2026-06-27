using Hangfire.Dashboard;

namespace Consolidado.Worker.Jobs;

/// <summary>
/// Filtro de autorização do dashboard do Hangfire para ambiente LOCAL: libera o acesso.
/// Em produção, trocar por verificação de identidade (ex.: JWT/role admin ou rede interna).
/// </summary>
public sealed class AllowAllDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
