<script setup lang="ts">
import { ref } from "vue";
import { useSession } from "../useSession";
import { registrarLancamento, ApiError, type TipoLancamento } from "../api";

const { state, autenticado } = useSession();

const valor = ref<number | null>(100);
const tipo = ref<TipoLancamento>(1);
const descricao = ref("");
const enviando = ref(false);
const sucesso = ref("");
const erro = ref("");

async function enviar() {
  sucesso.value = "";
  erro.value = "";
  if (!valor.value || valor.value <= 0) {
    erro.value = "Informe um valor maior que zero.";
    return;
  }
  enviando.value = true;
  try {
    const r = await registrarLancamento(state.token, {
      comercianteId: state.comercianteId,
      valor: valor.value,
      tipo: tipo.value,
      descricao: descricao.value || null,
    });
    sucesso.value = r.id;
    descricao.value = "";
  } catch (e) {
    erro.value = e instanceof ApiError ? e.message : "Falha ao registrar lançamento.";
  } finally {
    enviando.value = false;
  }
}
</script>

<template>
  <section class="card">
    <h2>➕ Novo lançamento</h2>

    <div class="row">
      <div class="field">
        <label>Valor (R$)</label>
        <input v-model.number="valor" type="number" min="0.01" step="0.01" :disabled="!autenticado" />
      </div>
      <div class="field">
        <label>Tipo</label>
        <select v-model.number="tipo" :disabled="!autenticado">
          <option :value="1">Crédito</option>
          <option :value="2">Débito</option>
        </select>
      </div>
    </div>

    <div class="field">
      <label>Descrição (opcional)</label>
      <input v-model="descricao" placeholder="ex.: Venda balcão" :disabled="!autenticado" />
    </div>

    <button :disabled="!autenticado || enviando" @click="enviar">
      {{ enviando ? "Enviando..." : "Registrar lançamento" }}
    </button>

    <div v-if="!autenticado" class="muted" style="margin-top: 10px">Autentique-se para registrar.</div>
    <div v-if="sucesso" class="msg ok">
      Aceito (202). Id: <code>{{ sucesso }}</code>
    </div>
    <div v-if="erro" class="msg err">{{ erro }}</div>
  </section>
</template>
