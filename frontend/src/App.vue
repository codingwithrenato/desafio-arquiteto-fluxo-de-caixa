<script setup lang="ts">
import AuthPanel from "./components/AuthPanel.vue";
import NovoLancamento from "./components/NovoLancamento.vue";
import ConsultarLancamento from "./components/ConsultarLancamento.vue";
import ConsolidadoPanel from "./components/ConsolidadoPanel.vue";
import SimularPico from "./components/SimularPico.vue";
import Extrato from "./components/Extrato.vue";
</script>

<template>
  <div class="app">
    <header class="topbar">
      <h1>Fluxo de Caixa <span class="accent">· Console de Testes</span></h1>
      <span class="sub">Lançamentos (8081) · Consolidado (8082) — comunicação assíncrona via RabbitMQ</span>
    </header>

    <div class="grid">
      <AuthPanel />
      <NovoLancamento />
      <ConsultarLancamento />
      <SimularPico />
      <ConsolidadoPanel />
      <Extrato />
    </div>
  </div>
</template>
