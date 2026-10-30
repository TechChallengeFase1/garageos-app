-- Role somente leitura da Lambda de login por CPF (garageos-lambda-auth).
--
-- Idempotente: pode rodar a cada deploy. Executado pelo Job
-- k8s/jobs/db-bootstrap-job.yaml, DEPOIS das migrations (a tabela "Clientes"
-- precisa existir), com o usuario master e a senha sorteada pelo Terraform
-- (infra/auth-reader.tf) na variavel do psql :senha.
--
--   psql -v senha="$AUTH_READER_PASSWORD" -f criar-auth-reader.sql
--
-- O privilegio e por COLUNA, e so nas colunas que a Lambda consulta:
--   SELECT "Id", "Nome", "Email", "Ativo" FROM "Clientes" WHERE "DocumentoValor" = $1
-- Endereco, telefone e demais tabelas ficam fora do alcance dela.

\set ON_ERROR_STOP on

SELECT format('CREATE ROLE auth_reader LOGIN PASSWORD %L', :'senha')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'auth_reader')
\gexec

-- Garante a senha atual mesmo se a role ja existia (o Terraform pode ter
-- sorteado outra ao recriar o segredo).
SELECT format('ALTER ROLE auth_reader WITH LOGIN PASSWORD %L', :'senha')
\gexec

SELECT format('GRANT CONNECT ON DATABASE %I TO auth_reader', current_database())
\gexec

GRANT USAGE ON SCHEMA public TO auth_reader;

-- Zera e concede de novo: se uma migration criar tabela nova, ela nao fica
-- visivel para a Lambda por acidente.
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM auth_reader;
GRANT SELECT ("Id", "Nome", "Email", "Ativo", "DocumentoValor") ON "Clientes" TO auth_reader;
