import { reactive, computed } from "vue";
import { obterToken } from "./api";

// Estado de sessão compartilhado entre os painéis (singleton reativo).
// Token em memória — recarregar a página reautentica (decisão de escopo).
const state = reactive({
  comercianteId: "loja-001",
  token: "" as string,
  expiraEm: "" as string,
});

export function useSession() {
  const autenticado = computed(() => state.token.length > 0);

  async function autenticar(comercianteId: string) {
    const resp = await obterToken(comercianteId);
    state.comercianteId = comercianteId;
    state.token = resp.access_token;
    state.expiraEm = resp.expires_at;
  }

  function sair() {
    state.token = "";
    state.expiraEm = "";
  }

  return { state, autenticado, autenticar, sair };
}
