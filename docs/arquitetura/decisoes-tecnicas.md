# Decisoes Tecnicas

## Decisao 001 - Stack Oficial Inicial

Data: 2026-06-29

Status: aprovada para inicio do projeto

## Contexto

O sistema sera iniciado como aplicacao desktop para Windows, com aproximadamente 10 estacoes. Futuramente devera evoluir para Web e mobile, sem reescrever as regras principais do negocio.

## Decisao

A stack oficial inicial sera:

- Aplicacao desktop: Flutter Desktop para Windows.
- Backend/API: ASP.NET Core.
- Banco de dados: PostgreSQL.
- Comunicacao entre desktop e backend: API REST.
- Controle de versao: Git.

## Motivos

- Flutter permite construir a versao desktop e reaproveitar conhecimento para Web/mobile.
- ASP.NET Core e robusto, performatico e adequado para APIs empresariais.
- PostgreSQL e gratuito, confiavel e preparado para operacao multiusuario.
- API separada evita que regras de negocio fiquem presas na interface desktop.

## Observacoes do Ambiente Atual

- .NET SDK encontrado: 9.0.313.
- PostgreSQL/psql ainda nao encontrado no PATH.
- Flutter precisa ser confirmado novamente antes da criacao do projeto desktop.

## Consequencias

- O projeto sera organizado em pastas separadas para desktop, backend e banco.
- As regras de negocio ficarao prioritariamente no backend.
- O banco sera modelado pensando em auditoria, permissoes e crescimento.
