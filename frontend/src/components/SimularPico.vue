<script setup lang="ts">
import { ref } from "vue";
import { useSession } from "../useSession";
import { registrarLancamento, ApiError, type TipoLancamento } from "../api";

const { state, autenticado } = useSession();

const quantidade = ref(20);
const rodando = ref(false);
const enviados = ref(0);
const falhas = ref(0);
const log = ref<{ tipo: TipoLancamento; valor: number }[]>([]);

function aleatorio() {
  const tipo: TipoLancamento = Math.random() < 0.6 ? 1 : 2; // ~60% crédito
  const valor = Math.round((Math.random() * 490 + 10) * 100) / 100; // 10..500
  return { tipo, valor };
}

async function disparar() {
  rodando.value = true;
  enviados.value = 0;
  falhas.value = 0;
  log.value = [];

  const tarefas = Array.from({ length: quantidade.value }, async () => {
    const { tipo, valor } = aleatorio();
    try {
      await registrarLancamento(state.token, { comercianteId: state.comercianteId, valor, tipo });
      enviados.value++;
      log.value.unshift({ tipo, valor });
      if (log.value.length > 30) log.value.pop();
    } catch (e) {
      falhas.value++;
      if (!(e instanceof ApiError)) console.error(e);
    }
  });

  await Promise.all(tarefas);
  rodando.value = false;
}
</script>

<template>
  <section class="card">
    <h2>⚡ Simular pico</h2>
    <p class="muted" style="margin-top: -8px; margin-bottom: 14px">
      Dispara vários lançamentos de uma vez. Use o auto-refresh do painel de consolidado para
      ver o saldo subir em tempo real (consolidação assíncrona).
    </p>

    <div class="row" style="align-items: flex-end">
      <div class="field" style="margin-bottom: 0">
        <label>Quantidade</label>
        <input v-model.number="quantidade" type="number" min="1" max="200" :disabled="!autenticado" />
      </div>
      <button :disabled="!autenticado || rodando" @click="disparar">
        {{ rodando ? "Disparando..." : "Disparar" }}
      </button>
    </div>

    <div v-if="enviados || falhas" class="msg ok">
      Enviados: <strong>{{ enviados }}</strong> · Falhas: <strong>{{ falhas }}</strong>
    </div>

    <div v-if="log.length" class="log">
      <div v-for="(l, i) in log" :key="i">
        <span :class="l.tipo === 1 ? 'credito' : 'debito'">
          {{ l.tipo === 1 ? "＋ crédito" : "－ débito " }}
        </span>
        R$ {{ l.valor.toFixed(2) }}
      </div>
    </div>
  </section>
</template>
