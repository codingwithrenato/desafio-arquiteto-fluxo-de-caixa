<script setup lang="ts">
import { ref, onUnmounted } from "vue";
import { useSession } from "../useSession";
import { obterConsolidado, ApiError, type SaldoConsolidadoDto } from "../api";

const { state, autenticado } = useSession();

function hoje() {
  return new Date().toISOString().slice(0, 10);
}

const data = ref(hoje());
const saldo = ref<SaldoConsolidadoDto | null>(null);
const carregando = ref(false);
const aviso = ref("");
const erro = ref("");
const autoRefresh = ref(false);
let timer: number | undefined;

async function consultar(silencioso = false) {
  if (!silencioso) {
    erro.value = "";
    aviso.value = "";
  }
  carregando.value = true;
  try {
    saldo.value = await obterConsolidado(state.token, state.comercianteId, data.value);
    aviso.value = "";
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) {
      saldo.value = null;
      aviso.value = "Ainda não há consolidado para esta data (aguarde a consolidação assíncrona).";
    } else {
      erro.value = e instanceof ApiError ? e.message : "Falha ao consultar.";
    }
  } finally {
    carregando.value = false;
  }
}

function toggleAuto() {
  autoRefresh.value = !autoRefresh.value;
  if (autoRefresh.value) {
    consultar(true);
    timer = window.setInterval(() => consultar(true), 2000);
  } else if (timer) {
    clearInterval(timer);
    timer = undefined;
  }
}

onUnmounted(() => timer && clearInterval(timer));
</script>

<template>
  <section class="card span-2">
    <h2>
      📊 Saldo consolidado diário
      <span v-if="autoRefresh" class="spin" style="margin-left: auto">↻</span>
    </h2>

    <div class="row" style="align-items: flex-end">
      <div class="field" style="margin-bottom: 0">
        <label>Data</label>
        <input v-model="data" type="date" :disabled="!autenticado" />
      </div>
      <button :disabled="!autenticado || carregando" @click="consultar(false)">Consultar</button>
      <button class="ghost" :disabled="!autenticado" @click="toggleAuto">
        {{ autoRefresh ? "Parar auto-refresh" : "Auto-refresh (2s)" }}
      </button>
    </div>

    <template v-if="saldo">
      <div
        class="saldo-grande"
        :class="saldo.saldo >= 0 ? 'pos' : 'neg'"
        style="margin-top: 20px"
      >
        R$ {{ saldo.saldo.toFixed(2) }}
      </div>
      <div class="metrics">
        <div class="metric">
          <div class="label">Créditos</div>
          <div class="value credito">{{ saldo.totalCreditos.toFixed(2) }}</div>
        </div>
        <div class="metric">
          <div class="label">Débitos</div>
          <div class="value debito">{{ saldo.totalDebitos.toFixed(2) }}</div>
        </div>
        <div class="metric">
          <div class="label">Lançamentos</div>
          <div class="value saldo">{{ saldo.quantidadeLancamentos }}</div>
        </div>
      </div>
      <p class="muted" style="margin-top: 12px">
        Atualizado em {{ new Date(saldo.atualizadoEmUtc).toLocaleString("pt-BR") }}
        <span v-if="saldo.fechado" class="pill" style="margin-left: 8px">🔒 dia fechado</span>
      </p>
    </template>

    <div v-if="!autenticado" class="muted" style="margin-top: 12px">Autentique-se para consultar.</div>
    <div v-if="aviso" class="msg" style="background: rgba(251, 191, 36, 0.1); color: var(--warn)">{{ aviso }}</div>
    <div v-if="erro" class="msg err">{{ erro }}</div>
  </section>
</template>
