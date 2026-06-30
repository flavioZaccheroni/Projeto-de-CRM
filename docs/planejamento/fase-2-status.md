# Fase 2 - Status

## Objetivo

Implementar o modulo comercial e de atendimento.

## Escopo

- Historico de atendimento ao cliente.
- Orcamentos com produtos e servicos.
- Pedidos de venda.
- Consulta de disponibilidade de produtos.
- Status de negociacao.

## Implementado

### Backend

- Endpoints de historico de atendimento.
- Endpoints de orcamentos.
- Endpoints de pedidos de venda.
- Atualizacao de status de orcamentos e pedidos.
- Dashboard com contadores comerciais.
- Auditoria nas criacoes e alteracoes de status.

### Banco de Dados

- Tabela `customer_interactions`.
- Tabela `sales_orders`.
- Tabela `sales_order_items`.
- Permissoes `interactions.write` e `sales_orders.write`.
- Massa de teste comercial.

### Desktop

- Tela de atendimento.
- Tela de orcamentos.
- Tela de pedidos de venda.
- Formularios basicos para atendimento, orcamento e pedido.
- Painel com indicadores comerciais.

## Pendencias

- Edicao de atendimentos, orcamentos e pedidos.
- Conversao automatica de orcamento aprovado em pedido.
- Busca/filtros comerciais.
- Reserva de estoque por pedido.
- Impressao/exportacao de orcamento.
