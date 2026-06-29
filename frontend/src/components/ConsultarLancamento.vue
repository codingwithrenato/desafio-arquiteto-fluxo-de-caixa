<script setup lang="ts">
import { ref } from "vue";
import { useSession } from "../useSession";
import { obterLancamento, ApiError, type LancamentoDto } from "../api";

const { state, autenticado } = useSession();
const id = ref("");
const buscando = ref(false);
const resultado = ref<LancamentoDto | null>(null);
const erro = ref("");

async function buscar() {
  erro.value = "";
  resultado.value = null;
  buscando.value = true;
  try {
    resultado.value = await obterLancamento(state.token, id.value.trim());
  } catch (e) {
    erro.value = e instanceof ApiError ? e.message : "Falha ao consultar.";
  } finally {
    buscando.value = false;
  }
}
</script>

<template>
  <section class="card">
    <h2>🔎 Consultar lançamento</h2>

    <div class="field">
      <label>Id do lançamento</label>
      <input v-model="id" placeholder="cole o id retornado" :disabled="!autenticado" @keyup.enter="buscar" />
    </div>

    <button :disabled="!autenticado || buscando || !id.trim()" @click="buscar">
      {{ buscando ? "Buscando..." : "Buscar" }}
    </button>

    <div v-if="resultado" class="msg ok">
      <div><strong>{{ resultado.tipo === 1 ? "Crédito" : "Débito" }}</strong> de R$ {{ resultado.valor.toFixed(2) }}</div>
      <div class="muted">Data: {{ resultado.data }} · {{ resultado.descricao || "sem descrição" }}</div>
    </div>
    <div v-if="erro" class="msg err">{{ erro }}</div>
  </section>
</template>
