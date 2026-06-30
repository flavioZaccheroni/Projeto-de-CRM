# CRM Autopecas e Servicos

Sistema desktop integrado para empresas de autopecas e prestacao de servicos, com evolucao planejada para Web e mobile.

## Objetivo

Criar uma plataforma original, inspirada em boas praticas de gestao empresarial, mas sem copiar telas, nomes, fluxos proprietarios ou identidade visual de sistemas existentes.

O sistema deve organizar o fluxo de informacoes entre setores, reduzir retrabalho e permitir crescimento gradual. A primeira versao sera focada em uso desktop para aproximadamente 10 estacoes, com arquitetura preparada para ampliar usuarios e canais de acesso.

## Documentacao Inicial

- [Visao do produto](docs/produto/visao-produto.md)
- [Modulos do sistema](docs/produto/modulos.md)
- [MVP](docs/produto/mvp.md)
- [Fluxo principal do MVP](docs/produto/fluxo-principal-mvp.md)
- [Arquitetura recomendada](docs/arquitetura/arquitetura-recomendada.md)
- [Decisoes tecnicas](docs/arquitetura/decisoes-tecnicas.md)
- [Modelo de dados inicial](docs/arquitetura/modelo-dados-inicial.md)
- [Modelo de servidor](docs/arquitetura/modelo-servidor.md)
- [Ambiente de desenvolvimento](docs/desenvolvimento/ambiente-desenvolvimento.md)
- [Android Studio](docs/desenvolvimento/android-studio.md)
- [Padroes de desenvolvimento](docs/desenvolvimento/padroes.md)
- [Fases do projeto](docs/planejamento/fases.md)
- [Status da Fase 1](docs/planejamento/fase-1-status.md)
- [Status da Fase 2](docs/planejamento/fase-2-status.md)
- [Proximos passos](docs/planejamento/proximos-passos.md)
- [Massa de testes do MVP](docs/planejamento/massa-testes-mvp.md)

## Estrutura Tecnica

- `backend/` - API ASP.NET Core.
- `desktop/` - aplicacao Flutter Desktop para Windows.
- `database/` - migracoes e massa de teste PostgreSQL.
- `tests/` - area reservada para testes complementares.

## Principios do Projeto

- Integracao real entre setores.
- Evolucao em fases, sem rupturas operacionais.
- Codigo organizado, comentado quando necessario e facil de manter.
- Banco de dados centralizado e preparado para auditoria.
- Interface propria, clara e adequada ao setor de autopecas e servicos.
- Arquitetura preparada para desktop primeiro, Web e mobile depois.

## Stack Recomendada

- Frontend desktop inicial: Flutter Desktop para Windows.
- Backend: API em ASP.NET Core ou Node.js/NestJS.
- Banco de dados: PostgreSQL.
- Autenticacao: usuarios, perfis e permissoes por modulo.
- Comunicacao: API REST inicialmente, com possibilidade de eventos/filas no futuro.

Essa combinacao permite criar uma aplicacao desktop moderna, com reaproveitamento de parte da interface para Web/mobile e um backend robusto para crescimento.
