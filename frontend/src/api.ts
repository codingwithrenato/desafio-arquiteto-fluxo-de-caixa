// Client HTTP tipado para os serviços de Lançamentos e Consolidado.
// Os caminhos são relativos (/api/lanc, /api/cons) e resolvidos pelo proxy
// (Nginx em produção, Vite em dev) — mantendo tudo same-origin (sem CORS).

const LANC = "/api/lanc";
const CONS = "/api/cons";

export type TipoLancamento = 1 | 2; // 1 = Crédito, 2 = Débito

export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_at: string;
}

export interface RegistrarLancamentoRequest {
  comercianteId: string;
  valor: number;
  tipo: TipoLancamento;
  data?: string | null;
  descricao?: string | null;
}

export interface LancamentoDto {
  id: string;
  comercianteId: string;
  valor: number;
  tipo: TipoLancamento;
  data: string;
  criadoEmUtc: string;
  descricao?: string | null;
}

export interface ExtratoDto {
  comercianteId: string;
  data: string;
  itens: LancamentoDto[];
  totalCreditos: number;
  totalDebitos: number;
  saldo: number;
  quantidade: number;
}

export interface SaldoConsolidadoDto {
  comercianteId: string;
  data: string;
  totalCreditos: number;
  totalDebitos: number;
  saldo: number;
  quantidadeLancamentos: number;
  atualizadoEmUtc: string;
  fechado: boolean;
}

/** Erro de API com status HTTP e mensagem amigável já resolvida. */
export class ApiError extends Error {
  constructor(public status: number, message: string, public details?: unknown) {
    super(message);
  }
}

async function parseError(resp: Response): Promise<ApiError> {
  let body: any = null;
  try {
    body = await resp.json();
  } catch {
    /* sem corpo JSON */
  }

  const mensagemPorStatus: Record<number, string> = {
    400: body?.mensagem ?? "Dados inválidos. Verifique os campos.",
    401: "Não autenticado. Gere um token primeiro.",
    404: body?.mensagem ?? "Recurso não encontrado.",
    429: "Limite de requisições atingido (rate limiting). Tente novamente.",
  };

  const mensagem =
    mensagemPorStatus[resp.status] ??
    body?.mensagem ??
    body?.detail ??
    `Erro inesperado (HTTP ${resp.status}).`;

  return new ApiError(resp.status, mensagem, body);
}

function authHeader(token: string): HeadersInit {
  return { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
}

export async function obterToken(comercianteId: string): Promise<TokenResponse> {
  const resp = await fetch(`${LANC}/auth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ comercianteId }),
  });
  if (!resp.ok) throw await parseError(resp);
  return resp.json();
}

export async function registrarLancamento(
  token: string,
  req: RegistrarLancamentoRequest,
): Promise<{ id: string }> {
  const resp = await fetch(`${LANC}/lancamentos`, {
    method: "POST",
    headers: authHeader(token),
    body: JSON.stringify(req),
  });
  if (!resp.ok) throw await parseError(resp);
  return resp.json();
}

export async function obterLancamento(token: string, id: string): Promise<LancamentoDto> {
  const resp = await fetch(`${LANC}/lancamentos/${id}`, { headers: authHeader(token) });
  if (!resp.ok) throw await parseError(resp);
  return resp.json();
}

export async function obterExtrato(
  token: string,
  comercianteId: string,
  data: string,
): Promise<ExtratoDto> {
  const resp = await fetch(`${LANC}/lancamentos/extrato/${encodeURIComponent(comercianteId)}/${data}`, {
    headers: authHeader(token),
  });
  if (!resp.ok) throw await parseError(resp);
  return resp.json();
}

export async function obterConsolidado(
  token: string,
  comercianteId: string,
  data: string,
): Promise<SaldoConsolidadoDto> {
  const resp = await fetch(`${CONS}/consolidado/${encodeURIComponent(comercianteId)}/${data}`, {
    headers: authHeader(token),
  });
  if (!resp.ok) throw await parseError(resp);
  return resp.json();
}
