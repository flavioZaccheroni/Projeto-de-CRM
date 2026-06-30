# Status da Fase 4 - Ordem de Servico

## Objetivo

Integrar clientes, veiculos, tecnicos, pecas, servicos, estoque e faturamento dentro do fluxo de ordem de servico.

## Implementado

- Listagem de ordens de servico.
- Abertura de OS com cliente, veiculo, tecnico, problema relatado e notas tecnicas.
- Inclusao inicial de peca ou servico na abertura.
- Atualizacao de status para execucao e encerramento.
- Baixa automatica de estoque ao encerrar OS com pecas.
- Bloqueio de encerramento quando nao ha saldo suficiente.
- Faturamento da OS com geracao de pedido de venda vinculado.
- Indicacao visual de OS ja faturada.
- Tela desktop de ordens de servico.
- Permissao de consulta `work_orders.read`.

## Banco de Dados

- `database/migrations/005_phase4_work_orders.sql`
- `database/seed/004_phase4_sample_data.sql`

## Validacoes

- Migration e seed aplicados no PostgreSQL local.
- Testes smoke atualizados com `/api/work-orders`.

## Pendencias para evolucao

- Edicao de itens da OS apos abertura.
- Inclusao de multiplas pecas e servicos no mesmo formulario.
- Historico detalhado por etapa de execucao.
- Impressao/geracao de PDF da OS.
- Regras fiscais/financeiras completas no faturamento.
