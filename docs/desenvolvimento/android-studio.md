# Android Studio

## Status

O projeto Flutter agora possui plataforma Android gerada em `desktop/android`.

## Caminho do Projeto

O caminho original do projeto contem acentos:

`E:\Projetos\Projeto - CRM Autopeças - Serviços`

No Windows, o Gradle/Android pode falhar em builds quando o caminho contem caracteres nao ASCII. Por isso, para executar pelo Android Studio, use uma copia ou clone em caminho simples, por exemplo:

`E:\Projetos\Projeto_CRM_Android`

## Emulador

Emuladores encontrados:

- `Pixel_4_API_36`
- `Medium_Phone_API_36`

Para iniciar pelo terminal:

```powershell
flutter emulators --launch Pixel_4_API_36
```

## API Local

No Windows, a API roda em:

`http://localhost:5026`

No emulador Android, o app acessa essa mesma API por:

`http://10.0.2.2:5026`

Essa regra ja foi configurada no `ApiClient`.

## Como Rodar

1. Inicie a API:

```powershell
dotnet run --project backend\AutoPartsCrm.Api\AutoPartsCrm.Api.csproj --launch-profile http
```

2. Abra o app Flutter no Android Studio usando a pasta:

```text
E:\Projetos\Projeto_CRM_Android\desktop
```

3. Selecione o emulador Android.

4. Execute `lib/main.dart`.

## Login de Desenvolvimento

- Email: `admin@crm.local`
- Senha: `123456`
