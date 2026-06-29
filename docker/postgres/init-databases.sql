-- Cria os bancos dos dois serviços no mesmo servidor PostgreSQL.
-- Mantém o "database-per-service" lógico: bancos distintos, sem acesso cruzado.
-- O banco "lancamentos" já é criado pelo entrypoint (POSTGRES_DB); aqui criamos o restante.
CREATE DATABASE consolidado;
