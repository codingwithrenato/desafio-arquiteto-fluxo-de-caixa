<script setup lang="ts">
import AuthPanel from "../components/AuthPanel.vue";
import NovoLancamento from "../components/NovoLancamento.vue";
import ConsultarLancamento from "../components/ConsultarLancamento.vue";
import ConsolidadoPanel from "../components/ConsolidadoPanel.vue";
import SimularPico from "../components/SimularPico.vue";
import Extrato from "../components/Extrato.vue";
</script>

<template>
  <div class="grid">
    <AuthPanel />
    <NovoLancamento />
    <ConsultarLancamento />
    <SimularPico />
    <ConsolidadoPanel />
    <Extrato />
  </div>
</template>
