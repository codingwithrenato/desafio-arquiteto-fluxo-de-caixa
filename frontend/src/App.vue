<script setup lang="ts">
// Layout raiz: cabeçalho com navegação + área de rota.
</script>

<template>
  <div class="app">
    <header class="topbar no-print">
      <h1>Fluxo de Caixa <span class="accent">· Console</span></h1>
      <nav class="nav">
        <RouterLink to="/" class="nav-link">Console</RouterLink>
        <RouterLink to="/extrato" class="nav-link">Extrato</RouterLink>
      </nav>
    </header>

    <RouterView />
  </div>
</template>
