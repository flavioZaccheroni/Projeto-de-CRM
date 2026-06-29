# Fase 1 - Status

## Objetivo

Criar o nucleo operacional do sistema:

- Login.
- Usuarios.
- Perfis e permissoes.
- Cadastros base.
- Banco de dados inicial.
- Auditoria basica.
- Layout principal do desktop.

## Implementado

### Backend

- Conexao com PostgreSQL.
- Endpoint de saude com verificacao de banco.
- Login de desenvolvimento.
- Consulta de dashboard.
- Consulta de perfis.
- Consulta de permissoes.
- Consulta e criacao de usuarios.
- Consulta e criacao de clientes.
- Consulta e criacao de veiculos.
- Consulta e criacao de produtos.
- Consulta e criacao de servicos.
- Consulta de auditoria.
- Registro de auditoria nas criacoes principais.

### Banco de Dados

- Migração `002_phase1_development_passwords.sql`.
- Senha temporaria de desenvolvimento aplicada aos usuarios seed.

### Desktop

- Tela de login.
- Shell principal com menu lateral.
- Painel com indicadores vindos da API.
- Tela de usuarios.
- Tela de clientes.
- Tela de veiculos.
- Tela de produtos.
- Tela de servicos.
- Tela de auditoria.
- Formularios basicos de criacao.

### Testes

- Testes de smoke da API.
- Teste de widget da tela de login.

## Credenciais Temporarias

Ambiente de desenvolvimento:

- Email: `admin@crm.local`
- Senha: `123456`

Essa senha e apenas para desenvolvimento. Antes de producao, a autenticacao deve usar hash seguro, troca obrigatoria de senha e politica de sessao.

## Pendencias Da Fase 1

Ainda faltam para considerar a Fase 1 totalmente fechada:

- Autorizacao real por permissao em cada endpoint.
- Edicao e inativacao dos cadastros.
- Validacoes mais completas de formulario.
- Padrao definitivo de hash de senha.
- Tela de parametros do sistema.
- Backup documentado em rotina operacional.

## Como Executar

1. Iniciar a API:

```powershell
dotnet run --project backend\AutoPartsCrm.Api\AutoPartsCrm.Api.csproj --launch-profile http
```

2. Em outro terminal, iniciar o desktop:

```powershell
cd desktop
flutter run -d windows
```
