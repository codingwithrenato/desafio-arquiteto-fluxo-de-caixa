<script setup lang="ts">
import { ref, computed } from "vue";
import { useSession } from "../useSession";
import { obterExtrato, ApiError, type ExtratoDto } from "../api";

const { state, autenticar } = useSession();

function hojeIso() {
  return new Date().toISOString().slice(0, 10);
}

const comerciante = ref(state.comercianteId);
const data = ref(hojeIso());
const extrato = ref<ExtratoDto | null>(null);
const emitidoEm = ref<Date | null>(null);
const carregando = ref(false);
const erro = ref("");

// Saldo acumulado (running balance) calculado no cliente a partir dos itens ordenados.
const linhas = computed(() => {
  if (!extrato.value) return [];
  let saldo = 0;
  return extrato.value.itens.map((item) => {
    saldo += item.tipo === 1 ? item.valor : -item.valor;
    return { ...item, saldoAcumulado: saldo };
  });
});

async function gerar() {
  erro.value = "";
  carregando.value = true;
  try {
    // Garante um token para o comerciante informado (torna a tela autossuficiente).
    await autenticar(comerciante.value.trim());
    extrato.value = await obterExtrato(state.token, comerciante.value.trim(), data.value);
    emitidoEm.value = new Date();
  } catch (e) {
    extrato.value = null;
    erro.value = e instanceof ApiError ? e.message : "Falha ao gerar o extrato.";
  } finally {
    carregando.value = false;
  }
}

function imprimir() {
  window.print();
}

function dataFmt(iso: string) {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}
function hora(iso: string) {
  return new Date(iso).toLocaleTimeString("pt-BR");
}
</script>

<template>
  <div class="extrato-page">
    <section class="card no-print">
      <h2>🧾 Gerar extrato</h2>
      <div class="row" style="align-items: flex-end">
        <div class="field" style="margin-bottom: 0">
          <label>Comerciante</label>
          <input v-model="comerciante" placeholder="ex.: loja-001" />
        </div>
        <div class="field" style="margin-bottom: 0">
          <label>Data</label>
          <input v-model="data" type="date" />
        </div>
        <button :disabled="carregando || !comerciante.trim()" @click="gerar">
          {{ carregando ? "Gerando..." : "Gerar extrato" }}
        </button>
        <button class="ghost" :disabled="!extrato || !extrato.itens.length" @click="imprimir">
          Imprimir
        </button>
      </div>
      <div v-if="erro" class="msg err">{{ erro }}</div>
    </section>

    <!-- Documento do extrato (área impressa) -->
    <section v-if="extrato" class="statement">
      <header class="statement-head">
        <div>
          <h1>Extrato de Fluxo de Caixa</h1>
          <p class="muted">Console de Testes · Fluxo de Caixa</p>
        </div>
        <div class="statement-meta">
          <div><span class="muted">Comerciante:</span> <strong>{{ extrato.comercianteId }}</strong></div>
          <div><span class="muted">Período:</span> <strong>{{ dataFmt(extrato.data) }}</strong></div>
          <div v-if="emitidoEm"><span class="muted">Emitido em:</span> {{ emitidoEm.toLocaleString("pt-BR") }}</div>
        </div>
      </header>

      <table v-if="linhas.length" class="statement-table">
        <thead>
          <tr>
            <th>Hora</th>
            <th>Descrição</th>
            <th>Tipo</th>
            <th class="num">Valor</th>
            <th class="num">Saldo</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="l in linhas" :key="l.id">
            <td class="muted">{{ hora(l.criadoEmUtc) }}</td>
            <td>{{ l.descricao || "—" }}</td>
            <td>
              <span :class="l.tipo === 1 ? 'tag-credito' : 'tag-debito'">
                {{ l.tipo === 1 ? "Crédito" : "Débito" }}
              </span>
            </td>
            <td class="num" :class="l.tipo === 1 ? 'credito' : 'debito'">
              {{ l.tipo === 1 ? "+" : "−" }} {{ l.valor.toFixed(2) }}
            </td>
            <td class="num">{{ l.saldoAcumulado.toFixed(2) }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="3" class="muted">Totais do dia</td>
            <td class="num">
              <span class="credito">+{{ extrato.totalCreditos.toFixed(2) }}</span>
              /
              <span class="debito">−{{ extrato.totalDebitos.toFixed(2) }}</span>
            </td>
            <td class="num" :class="extrato.saldo >= 0 ? 'credito' : 'debito'">
              <strong>{{ extrato.saldo.toFixed(2) }}</strong>
            </td>
          </tr>
        </tfoot>
      </table>

      <p v-else class="muted" style="margin-top: 20px">Nenhum lançamento neste dia.</p>

      <footer class="statement-foot muted">
        Saldo final do dia: <strong :class="extrato.saldo >= 0 ? 'credito' : 'debito'">R$ {{ extrato.saldo.toFixed(2) }}</strong>
        · {{ extrato.quantidade }} lançamento(s)
      </footer>
    </section>

    <p v-else class="muted no-print" style="margin-top: 16px">
      Informe o comerciante e a data e clique em <strong>Gerar extrato</strong>.
    </p>
  </div>
</template>
