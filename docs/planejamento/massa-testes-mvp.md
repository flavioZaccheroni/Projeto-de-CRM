# Massa de Testes do MVP

## Objetivo

Permitir testes manuais e automatizados do primeiro fluxo operacional:

Cliente -> Veiculo -> Orcamento -> Ordem de Servico -> Baixa de Estoque

## Arquivo

`database/seed/001_sample_data.sql`

## Dados Criados

- Perfis iniciais.
- Permissoes iniciais.
- Usuarios ficticios.
- Cliente ficticio.
- Veiculo ficticio.
- Produtos ficticios.
- Servico ficticio.
- Orcamento aprovado.
- Ordem de servico em andamento.
- Movimentos de saida de estoque.

## Usuarios de Teste

As senhas ainda nao sao reais. O campo `password_hash` usa o valor temporario `trocar-por-hash-real` ate a autenticacao ser implementada.

- `admin@crm.local`
- `vendedor@crm.local`
- `tecnico@crm.local`

## Conferencias Esperadas

- O cliente deve ter um veiculo.
- O orcamento deve estar aprovado.
- A ordem de servico deve estar em andamento.
- O estoque do filtro de oleo deve sair de 20 para 19.
- O estoque do oleo 5W30 deve sair de 48 para 46.
