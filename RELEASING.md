# Rilascio di FDR11.Logger

## Build locale

```powershell
dotnet restore FDR11.Logger.slnx
dotnet build FDR11.Logger.slnx --configuration Release
dotnet test FDR11.Logger.slnx --configuration Release --no-build
dotnet pack Code/FDR11.Logger.csproj `
  --configuration Release `
  --no-build `
  --output artifacts
```

Il pack genera:

- `FDR11.Logger.<versione>.nupkg`;
- `FDR11.Logger.<versione>.snupkg`.

## Configurazione NuGet.org una tantum

1. Accedere a NuGet.org e aprire **Trusted Publishing** dal menu dell'account.
2. Creare una policy con:
   - repository owner: `della11`;
   - repository: `FDR11.Logger`;
   - workflow file: `publish-nuget.yml`;
   - environment: vuoto.
3. Nel repository GitHub aprire **Settings > Secrets and variables > Actions**
   e creare il repository secret `NUGET_USER`, contenente il nome utente
   NuGet.org e non l'indirizzo email.

## Pubblicazione

Il pacchetto `1.0.0` pubblicato su GitHub Packages non contiene il README della
root. La prima versione pubblicata anche su NuGet.org deve quindi essere
`1.0.1`.

Dopo aver eseguito commit e push delle modifiche, creare e pubblicare il nuovo
tag SemVer:

```powershell
git tag v1.0.1
git push origin v1.0.1
```

Il workflow:

1. esegue restore, build e test;
2. genera `.nupkg` e `.snupkg`;
3. pubblica entrambi su NuGet.org;
4. pubblica il pacchetto principale su GitHub Packages;
5. crea o aggiorna la GitHub Release.
