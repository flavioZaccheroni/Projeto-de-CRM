# Modelo de Servidor

## Decisao

Para a primeira implantacao, o sistema usara o modelo de servidor local na empresa, preparado para migracao futura para nuvem.

## Modelo Inicial

- Um computador ou servidor dedicado na rede local.
- PostgreSQL instalado no servidor.
- API ASP.NET Core executando no servidor.
- Desktops Windows acessando a API pela rede interna.
- Backups locais e copia externa periodica.

## Motivos

- A operacao inicial sera de aproximadamente 10 desktops.
- O acesso local tende a ser rapido e simples para a primeira versao.
- Reduz dependencia de internet para a operacao diaria.
- Permite validar o sistema antes de assumir custos de nuvem.

## Preparacao Para Futuro

A arquitetura com API separada permite migrar para:

- Servidor em nuvem.
- Modelo hibrido.
- Acesso Web.
- Aplicativo mobile.

## Requisitos Minimos do Servidor Local

- Windows 10/11 Pro ou Windows Server.
- Processador moderno com pelo menos 4 nucleos.
- 16 GB de memoria RAM recomendados.
- SSD.
- Rede cabeada estavel.
- Rotina de backup configurada.

## Cuidados Obrigatorios

- Usuario e senha fortes para banco e sistema.
- Firewall configurado apenas para portas necessarias.
- Backup diario.
- Teste periodico de restauracao.
- No-break recomendado para evitar corrupcao por queda de energia.
