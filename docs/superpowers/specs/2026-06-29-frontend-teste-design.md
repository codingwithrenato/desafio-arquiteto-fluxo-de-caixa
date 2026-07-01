# Spec — Frontend de teste (Vue 3) para o Fluxo de Caixa

**Data:** 2026-06-29
**Status:** Aprovado

## Objetivo
Fornecer uma interface web simples para **testar e demonstrar** os serviços de Lançamentos e
Consolidado sem depender de curl/Swagger — facilitando a apresentação do desafio.

## Stack
- **Vue 3 + Vite + TypeScript** (Composition API).
- Servido em produção/local por **Nginx** (container no `docker-compose`).
- CSS próprio enxuto (sem framework de UI pesado).

## Arquitetura de execução
- A SPA é buildada e servida por um container Nginx na porta **8080**.
- O Nginx faz **reverse-proxy** de `/api/*`, tornando tudo **same-origin** (sem CORS, sem
  alteração nos serviços de backend):
  - `/api/lanc/*` → `lancamentos-api:8080`
  - `/api/cons/*` → `consolidado-api:8080`
- Desenvolvimento local (`npm run dev`): o `vite.config.ts` replica o proxy apontando para
  `localhost:8081` e `localhost:8082`.

## Funcionalidades (painéis)
1. **Autenticação** — informa `comercianteId`; chama `POST /auth/token`; guarda o JWT em
   memória e exibe o status. Todos os painéis usam esse token e comerciante.
2. **Novo lançamento** — formulário (valor, tipo crédito/débito, descrição); `POST /lancamentos`;
   mostra `202 Accepted` e o id retornado.
3. **Consultar lançamento** — por id; `GET /lancamentos/{id}`.
4. **Saldo consolidado** — informa a data; `GET /consolidado/{comerciante}/{data}`; exibe
   créditos, débitos, saldo, quantidade; **auto-refresh** (toggle) para acompanhar a
   consolidação assíncrona.
5. **Simular pico** — dispara N lançamentos aleatórios e acompanha o saldo crescer em tempo
   real (demonstra o fluxo assíncrono e o caminho de leitura).

## Estrutura de código (`frontend/`)
```
frontend/
  index.html
  package.json
  tsconfig.json
  vite.config.ts          # proxy /api/lanc e /api/cons em dev
  nginx.conf              # serve SPA + proxy /api/* (container)
  Dockerfile              # multi-stage: node build -> nginx
  src/
    main.ts
    App.vue               # layout + composição dos painéis
    api.ts                # client tipado (auth, lançamentos, consolidado)
    useSession.ts         # composable: token + comercianteId (estado compartilhado)
    components/
      AuthPanel.vue
      NovoLancamento.vue
      ConsultarLancamento.vue
      ConsolidadoPanel.vue
      SimularPico.vue
```

## Contrato com as APIs (via proxy)
| Ação | Método | Caminho (frontend) | Destino |
|---|---|---|---|
| Token | POST | `/api/lanc/auth/token` | lancamentos-api `/auth/token` |
| Registrar lançamento | POST | `/api/lanc/lancamentos` | lancamentos-api `/lancamentos` |
| Consultar lançamento | GET | `/api/lanc/lancamentos/{id}` | lancamentos-api `/lancamentos/{id}` |
| Consolidado | GET | `/api/cons/consolidado/{c}/{data}` | consolidado-api `/consolidado/...` |

## Tratamento de erros
Mensagens amigáveis por status: `400` (validação — exibe campos), `401` (sem/expirado token),
`404` (sem consolidado/lançamento), `429` (rate limit atingido).

## Integração com a infra existente
- Novo serviço `frontend` no `docker-compose.yml` (porta 8080), `depends_on` das duas APIs.
- Opcional: adicionar o build do frontend ao CI.

## Fora de escopo (YAGNI)
- Testes automatizados de UI (é um harness de teste manual).
- Gestão de rotas/SPA router (uma página com painéis basta).
- Persistência de sessão (token em memória; recarregar a página reautentica).

## Critérios de sucesso
- `docker compose up` sobe o frontend em `http://localhost:8080`.
- É possível autenticar, lançar (crédito/débito), consultar o consolidado e ver o saldo
  atualizar via auto-refresh — tudo pela UI, sem curl.
