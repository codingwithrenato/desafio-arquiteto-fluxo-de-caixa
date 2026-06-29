<script setup lang="ts">
import { ref } from "vue";
import { useSession } from "../useSession";
import { ApiError } from "../api";

const { state, autenticado, autenticar, sair } = useSession();
const comerciante = ref(state.comercianteId);
const carregando = ref(false);
const erro = ref("");

async function entrar() {
  erro.value = "";
  carregando.value = true;
  try {
    await autenticar(comerciante.value.trim());
  } catch (e) {
    erro.value = e instanceof ApiError ? e.message : "Falha ao autenticar.";
  } finally {
    carregando.value = false;
  }
}
</script>

<template>
  <section class="card">
    <h2>🔑 Autenticação</h2>

    <div class="field">
      <label>Comerciante (identificador)</label>
      <input v-model="comerciante" placeholder="ex.: loja-001" @keyup.enter="entrar" />
    </div>

    <div class="row">
      <button :disabled="carregando || !comerciante.trim()" @click="entrar">
        {{ carregando ? "Gerando..." : "Gerar token (JWT)" }}
      </button>
      <button v-if="autenticado" class="ghost" @click="sair">Sair</button>
    </div>

    <p style="margin-top: 14px">
      <span class="pill" :class="{ ok: autenticado }">
        <span class="dot"></span>
        {{ autenticado ? `Autenticado como ${state.comercianteId}` : "Não autenticado" }}
      </span>
    </p>

    <div v-if="erro" class="msg err">{{ erro }}</div>
  </section>
</template>
