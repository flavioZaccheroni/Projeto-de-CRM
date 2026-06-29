# Padroes de Desenvolvimento

## Organizacao Geral

- Separar interface, regras de negocio e acesso a dados.
- Evitar regra de negocio diretamente nas telas.
- Nomear arquivos, classes e funcoes de forma clara.
- Criar componentes reutilizaveis para telas semelhantes.
- Manter funcoes pequenas e objetivas.

## Comentarios

O projeto deve ser comentado com criterio. Comentarios sao obrigatorios quando:

- Uma regra de negocio nao for obvia.
- Houver uma decisao tecnica importante.
- Existir uma integracao externa.
- Uma validacao depender de exigencia legal, fiscal ou operacional.
- Um trecho tiver risco de manutencao futura.

Evitar comentarios que apenas repetem o codigo.

## Padrao de Commits

Sugestao:

- `feat:` nova funcionalidade.
- `fix:` correcao.
- `docs:` documentacao.
- `test:` testes.
- `refactor:` melhoria interna sem mudar comportamento.
- `chore:` tarefas de manutencao.

## Qualidade

- Validar dados de entrada no frontend e no backend.
- Criar testes para regras criticas.
- Revisar permissoes em todas as operacoes sensiveis.
- Registrar logs para erros e operacoes relevantes.
- Evitar dependencias desnecessarias.

## Auditoria

Operacoes importantes devem registrar:

- Usuario.
- Data e hora.
- Acao realizada.
- Registro afetado.
- Valores anteriores e novos quando aplicavel.

## Backup

Desde a primeira versao de producao, o sistema deve ter rotina clara de backup e restauracao testada.
