# ADR 0003 — Padrão Transactional Outbox

**Status:** Aceito

## Contexto
Ao registrar um lançamento, precisamos fazer **duas escritas**: gravar no banco e publicar o
evento no broker. Fazer as duas separadamente cria o **dual-write problem**: se o processo
cair (ou o broker estiver fora) entre a gravação e a publicação, o lançamento existe mas o
evento se perde — o Consolidado nunca o veria, gerando saldo incorreto e silencioso.

## Decisão
Usar o **padrão Outbox**: na MESMA transação do lançamento, gravamos o evento em uma tabela
`outbox_messages`. Um **dispatcher** (BackgroundService) lê as mensagens pendentes e as publica
no RabbitMQ, marcando-as como processadas.

Detalhes de implementação:
- A leitura usa `FOR UPDATE SKIP LOCKED` → várias réplicas do dispatcher coexistem sem
  processar a mesma mensagem nem se bloquearem.
- Publicação com **mensagens persistentes + publisher confirms** e **Polly** (retry + circuit
  breaker).
- Falha na publicação **não perde** a mensagem: incrementa tentativas e retenta no próximo ciclo.

## Alternativas consideradas
- **Publicar direto no broker dentro do handler:** sujeito ao dual-write. Rejeitada.
- **Transação distribuída (2PC) banco+broker:** complexa, frágil e mal suportada. Rejeitada.

## Consequências
- ✅ Garantia de entrega **at-least-once**: nenhum evento se perde.
- ✅ Lançamentos responde rápido (202) sem depender do broker no caminho da requisição.
- ⚠️ Exige consumidor **idempotente** no Consolidado (reentrega pode duplicar a mensagem) —
  resolvido com a tabela `eventos_processados`.
- ⚠️ Pequena latência adicional (intervalo de polling do dispatcher).
