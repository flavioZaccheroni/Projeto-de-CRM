# MVP - Primeira Versao Operacional

## Objetivo

Entregar uma primeira versao funcional que valide o fluxo central da empresa sem tentar implementar todos os modulos de uma vez.

## Escopo Incluido

### Acesso e Seguranca

- Login.
- Usuarios.
- Perfis.
- Permissoes por modulo e acao.

### Cadastros

- Clientes.
- Veiculos.
- Produtos.
- Servicos.

### Comercial

- Orcamento simples.
- Itens de produtos no orcamento.
- Itens de servicos no orcamento.
- Status do orcamento.

### Ordem de Servico

- Criacao de ordem a partir de cliente e veiculo.
- Vinculo opcional com orcamento.
- Produtos utilizados.
- Servicos executados.
- Status da ordem.

### Estoque

- Saldo inicial de produto.
- Movimento de entrada.
- Movimento de saida.
- Baixa de estoque pela ordem de servico.

## Fora do MVP

- Emissao fiscal.
- Integracao bancaria.
- Compras completas.
- Contabilidade.
- Aplicativo mobile.
- Portal Web.
- BI avancado.

## Perfis Iniciais

- Administrador.
- Gerencia.
- Vendedor.
- Estoquista.
- Tecnico.
- Financeiro.

## Criterios de Aceite

- Usuario consegue fazer login.
- Permissoes controlam acesso as telas principais.
- Cliente, veiculo, produto e servico podem ser cadastrados.
- Orcamento pode conter produtos e servicos.
- Ordem de servico pode consumir produtos do estoque.
- Movimentacoes de estoque ficam registradas.
- Operacoes relevantes registram usuario e data.
