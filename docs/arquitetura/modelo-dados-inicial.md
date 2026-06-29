# Modelo de Dados Inicial

## Objetivo

Dar suporte ao MVP com controle de usuarios, clientes, veiculos, produtos, servicos, orcamentos, ordens de servico, estoque e auditoria.

## Entidades Principais

- `roles`: perfis de acesso.
- `permissions`: permissoes disponiveis.
- `role_permissions`: permissoes vinculadas a cada perfil.
- `users`: usuarios do sistema.
- `customers`: clientes.
- `vehicles`: veiculos dos clientes.
- `products`: pecas e produtos.
- `services`: servicos prestados.
- `stock_balances`: saldo atual por produto.
- `stock_movements`: historico de entradas, saidas e ajustes.
- `quotations`: orcamentos.
- `quotation_items`: produtos e servicos do orcamento.
- `work_orders`: ordens de servico.
- `work_order_items`: produtos e servicos da ordem.
- `audit_logs`: historico de operacoes relevantes.

## Regras Iniciais

- Produto e servico nao podem ter preco negativo.
- Itens de orcamento e ordem devem ter quantidade positiva.
- Item do tipo produto deve apontar apenas para produto.
- Item do tipo servico deve apontar apenas para servico.
- Saldo de estoque nao pode ficar negativo no modelo inicial.
- Ordem de servico e orcamento possuem status controlados.

## Evolucoes Futuras

- Empresas/filiais.
- Multiplos locais de estoque.
- Compras completas.
- Financeiro completo.
- Fiscal.
- Integracoes externas.
