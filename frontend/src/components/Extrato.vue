<script setup lang="ts">
import { ref } from "vue";
import { useSession } from "../useSession";
import { obterExtrato, ApiError, type ExtratoDto } from "../api";

const { state, autenticado } = useSession();

function hoje() {
  return new Date().toISOString().slice(0, 10);
}

const data = ref(hoje());
const extrato = ref<ExtratoDto | null>(null);
const carregando = ref(false);
const erro = ref("");

async function consultar() {
  erro.value = "";
  carregando.value = true;
  try {
    extrato.value = await obterExtrato(state.token, state.comercianteId, data.value);
  } catch (e) {
    erro.value = e instanceof ApiError ? e.message : "Falha ao obter o extrato.";
  } finally {
    carregando.value = false;
  }
}

function hora(iso: string) {
  return new Date(iso).toLocaleTimeString("pt-BR");
}
</script>

<template>
  <section class="card span-2">
    <h2>🧾 Extrato do dia</h2>

    <div class="row" style="align-items: flex-end">
      <div class="field" style="margin-bottom: 0">
        <label>Data</label>
        <input v-model="data" type="date" :disabled="!autenticado" />
      </div>
      <button :disabled="!autenticado || carregando" @click="consultar">
        {{ carregando ? "Carregando..." : "Ver extrato" }}
      </button>
    </div>

    <template v-if="extrato">
      <table v-if="extrato.itens.length" class="extrato">
        <thead>
          <tr>
            <th>Hora</th>
            <th>Tipo</th>
            <th>Descrição</th>
            <th class="num">Valor</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in extrato.itens" :key="item.id">
            <td class="muted">{{ hora(item.criadoEmUtc) }}</td>
            <td>
              <span :class="item.tipo === 1 ? 'tag-credito' : 'tag-debito'">
                {{ item.tipo === 1 ? "Crédito" : "Débito" }}
              </span>
            </td>
            <td>{{ item.descricao || "—" }}</td>
            <td class="num" :class="item.tipo === 1 ? 'credito' : 'debito'">
              {{ item.tipo === 1 ? "+" : "−" }} {{ item.valor.toFixed(2) }}
            </td>
          </tr>
        </tbody>
      </table>

      <div v-else class="msg" style="background: rgba(251, 191, 36, 0.1); color: var(--warn)">
        Nenhum lançamento neste dia.
      </div>

      <div v-if="extrato.itens.length" class="extrato-resumo">
        <span>Créditos: <strong class="credito">{{ extrato.totalCreditos.toFixed(2) }}</strong></span>
        <span>Débitos: <strong class="debito">{{ extrato.totalDebitos.toFixed(2) }}</strong></span>
        <span>Lançamentos: <strong>{{ extrato.quantidade }}</strong></span>
        <span>Saldo: <strong :class="extrato.saldo >= 0 ? 'credito' : 'debito'">R$ {{ extrato.saldo.toFixed(2) }}</strong></span>
      </div>
    </template>

    <div v-if="!autenticado" class="muted" style="margin-top: 12px">Autentique-se para ver o extrato.</div>
    <div v-if="erro" class="msg err">{{ erro }}</div>
  </section>
</template>
