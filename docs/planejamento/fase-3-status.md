# Fase 3 - Status

## Objetivo

Implementar o controle inicial de estoque e compras.

## Implementado

### Backend

- Consulta de saldos de estoque.
- Consulta de movimentacoes de estoque.
- Registro de entrada, saida e ajuste de estoque.
- Criacao de pedidos de compra.
- Recebimento de pedidos de compra com entrada automatica no estoque.
- Indicadores de compras e estoque baixo no dashboard.

### Banco de Dados

- Tabela `purchase_orders`.
- Tabela `purchase_order_items`.
- Tabela `purchase_receipts`.
- Tabela `purchase_receipt_items`.
- Permissoes `purchase_orders.write` e `stock.read`.
- Massa de teste para pedido de compra.

### Desktop

- Tela de estoque.
- Tela de compras.
- Formulario de movimentacao de estoque.
- Formulario basico de pedido de compra.

## Pendencias

- Tela de recebimento parcial/total do pedido de compra.
- Edicao e cancelamento de pedidos de compra.
- Inventario completo.
- Multiplos locais de estoque.
- Reserva de estoque vinculada a pedidos e ordens de servico.
