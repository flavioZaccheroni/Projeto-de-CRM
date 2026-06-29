# Backend

API central do CRM Autopecas e Servicos.

## Tecnologia

- ASP.NET Core.
- .NET 9.
- PostgreSQL previsto como banco principal.

## Endpoints iniciais

- `GET /` - informacoes basicas da API.
- `GET /health` - verificacao simples de saude.

## Responsabilidades

- Autenticacao e autorizacao.
- Regras de negocio.
- Validacoes centrais.
- Integracao com banco de dados.
- Auditoria de operacoes relevantes.

As telas desktop devem consumir a API em vez de acessar diretamente o banco.
