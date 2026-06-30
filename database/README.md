# Banco de Dados

Banco principal planejado: PostgreSQL.

## Banco Local de Desenvolvimento

- Nome: `autoparts_crm_dev`
- Host: `localhost`
- Porta: `5432`
- Usuario administrativo local: `postgres`

Nao salvar senha real no repositorio. Use `.env.example` apenas como referencia.

## Estrutura

- `migrations/` - scripts de criacao e alteracao do schema.
- `seed/` - dados iniciais e massa de teste.

## Primeira Migracao

Arquivo: `migrations/001_initial_schema.sql`

Inclui:

- Usuarios, perfis e permissoes.
- Clientes e veiculos.
- Produtos e servicos.
- Saldos e movimentos de estoque.
- Orcamentos e itens.
- Ordens de servico e itens.
- Auditoria.

## Observacoes

O script usa UUIDs e a extensao `uuid-ossp`. Em ambientes onde essa extensao nao estiver disponivel, a alternativa sera trocar para `gen_random_uuid()` com a extensao `pgcrypto`.

## Scripts Aplicados na Fase 0

- `migrations/001_initial_schema.sql`
- `migrations/002_phase1_development_passwords.sql`
- `migrations/003_phase2_commercial.sql`
- `migrations/004_phase3_stock_purchases.sql`
- `seed/001_sample_data.sql`
- `seed/002_phase2_sample_data.sql`
- `seed/003_phase3_sample_data.sql`
