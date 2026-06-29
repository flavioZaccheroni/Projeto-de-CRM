# Proximos Passos

## Decisoes Antes da Programacao

1. Confirmar a stack do backend:
   - Recomendado: ASP.NET Core.
   - Alternativa: Node.js com NestJS.

2. Confirmar o modelo de instalacao:
   - Servidor local na empresa.
   - Servidor em nuvem.
   - Modelo hibrido.

3. Confirmar quais modulos entram no MVP:
   - Login e permissoes.
   - Clientes.
   - Produtos.
   - Estoque basico.
   - Orcamentos.
   - Ordem de servico.

4. Definir usuarios iniciais e perfis:
   - Administrador.
   - Vendedor.
   - Estoquista.
   - Tecnico.
   - Financeiro.
   - Gerencia.

5. Definir campos obrigatorios dos cadastros principais.

## Primeira Entrega Recomendada

Criar um MVP desktop com:

- Tela de login.
- Menu principal por permissao.
- Cadastro de clientes.
- Cadastro de produtos.
- Cadastro de servicos.
- Cadastro de veiculos.
- Orcamento simples.
- Ordem de servico simples.
- Estoque basico.

Essa primeira entrega ja permite validar o fluxo central da empresa sem tentar resolver todo o ERP de uma vez.

## Ordem Tecnica Recomendada

1. Inicializar repositorio Git.
2. Criar projeto backend.
3. Criar projeto desktop.
4. Configurar PostgreSQL.
5. Criar banco inicial com migracoes.
6. Implementar autenticacao.
7. Implementar cadastros base.
8. Implementar primeiro fluxo completo: cliente, produto, orcamento e ordem de servico.

## Riscos Principais

- Comecar por telas sem definir regras de negocio.
- Misturar regras de negocio dentro da interface.
- Adiar controle de permissoes.
- Adiar backup.
- Tentar copiar fluxo de outro sistema em vez de criar um fluxo proprio.
- Incluir modulo fiscal cedo demais sem especificacao adequada.

## Recomendacao

Comecar pelo MVP operacional e deixar fiscal, integracoes externas e mobile para fases posteriores. Isso reduz risco, acelera validacao e evita retrabalho.
