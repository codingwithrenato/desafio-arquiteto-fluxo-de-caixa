# Spec — Tela dedicada de extrato (estilo extrato bancário)

**Data:** 2026-06-29
**Status:** Aprovado

## Objetivo
Criar uma **tela dedicada** para apresentar o extrato (estilo extrato bancário), em rota
própria, pronta para mostrar/imprimir — complementando o painel embutido do console.

## Roteamento
- Adicionar **Vue Router** ao frontend.
- Rotas:
  - `/` → `views/ConsoleView.vue` (o dashboard atual, com os painéis).
  - `/extrato` → `views/ExtratoView.vue` (a tela de apresentação).
- `App.vue` passa a ter um **cabeçalho com navegação** (Console | Extrato) + `<router-view>`.
- O dashboard atual (grid de painéis) é movido para `ConsoleView.vue` sem mudança funcional.
- A sessão (`useSession`) é um singleton reativo → **persiste ao navegar** entre rotas.

## Tela do Extrato (`ExtratoView.vue`)
Layout formal, limpo, para apresentação/impressão:
- **Cabeçalho**: título "Extrato de Fluxo de Caixa", comerciante, período (data), data de emissão.
- **Controles**: campos comerciante + data + botão "Gerar extrato"; botão "Imprimir".
  - "Gerar extrato" garante o token (autentica com o comerciante informado, reusando
    `useSession`) e busca via `obterExtrato`. Torna a tela autossuficiente para apresentar.
- **Tabela**: Hora · Descrição · Tipo · Valor · **Saldo acumulado** (running balance calculado
  no cliente a partir dos itens ordenados).
- **Rodapé**: total de créditos, total de débitos, saldo final.
- **Estado vazio** amigável quando não há lançamentos no dia.
- **Impressão**: `window.print()` + CSS `@media print` (esconde nav/botões/console, formata o
  extrato para papel/PDF).

## Reuso
- Mesmo endpoint `GET /lancamentos/extrato/{comerciante}/{data}` e funções de `api.ts` — **sem
  mudança no backend**.
- O painel `Extrato.vue` embutido no console permanece como atalho rápido.

## Estrutura (frontend)
```
src/
  main.ts            # registra o router
  router.ts          # rotas / e /extrato
  App.vue            # nav + <router-view>
  views/
    ConsoleView.vue  # dashboard atual (grid de painéis)
    ExtratoView.vue  # tela de apresentação do extrato
```

## Fora de escopo (YAGNI)
- Exportação programática de PDF (o "Imprimir → Salvar como PDF" do navegador cobre).
- Paginação / intervalo de datas (segue por dia).
- Persistência de sessão entre reloads.

## Critérios de sucesso
- Navegar para `/extrato` mostra a tela dedicada; "Gerar extrato" lista os lançamentos do dia
  com saldo acumulado e totais.
- "Imprimir" gera uma versão limpa (sem navegação/controles) do extrato.
- O console (`/`) continua funcionando como antes.
