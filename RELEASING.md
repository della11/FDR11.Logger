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

## Configurazione GitHub una tantum

1. Aprire **Settings > Environments**.
2. Creare l'environment `nuget-release`.
3. Abilitare **Required reviewers** e scegliere gli approvatori.
4. Facoltativo: abilitare **Prevent self-review**.
5. In **Settings > Secrets and variables > Actions**, creare il repository
   secret `NUGET_USER` con il nome utente NuGet.org, non l'email.

Il job `prepare` esegue build, test e pack. Il job `publish` attende
l'approvazione dell'environment prima di accedere ai feed.

## Configurazione NuGet.org una tantum

In NuGet.org aprire **Trusted Publishing** e configurare:

- repository owner: `della11`;
- repository: `FDR11.Logger`;
- workflow file: `publish-nuget.yml`;
- environment: `nuget-release`.

L'environment deve coincidere con quello dichiarato nel workflow, altrimenti
l'autenticazione OIDC fallisce.

## Pubblicazione

1. Aprire **Actions > Publish NuGet package > Run workflow**.
2. Selezionare il branch `main`.
3. Attivare **Pubblicare una prerelease preview.N?** solo per una prerelease.
4. Avviare il workflow.
5. Dopo build e test, aprire il run e approvare `nuget-release`.

Il workflow calcola la versione dai tag esistenti:

- stable: `v1.0.1` diventa `v1.0.2`;
- prima prerelease: `v1.0.1` diventa `v1.0.2-preview.1`;
- prerelease successiva: `v1.0.2-preview.1` diventa
  `v1.0.2-preview.2`;
- stable dopo le preview: diventa `v1.0.2`.

Dopo l'approvazione pubblica `.nupkg` e `.snupkg`, quindi crea automaticamente
tag e GitHub Release. La concorrenza è serializzata per evitare versioni
duplicate.
