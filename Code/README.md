# FDR11.Logger

![FDR11.Logger](https://raw.githubusercontent.com/della11/FDR11.Logger/main/Code/assets/fdr11-logger-icon.png)

Pacchetto per applicare una configurazione Serilog uniforme alle applicazioni
.NET 10 e ASP.NET Core.

## Caratteristiche

- eventi strutturati in formato CLEF/JSON compatto;
- output console e file rolling giornaliero;
- proprietà standard `Application`, `Environment`, `MachineName`,
  `ProcessId`, `ThreadId`, `CorrelationId`, `TraceId` e `SpanId`;
- correlation ID propagato tramite l'header `X-Correlation-ID`;
- un singolo evento di completamento per ogni richiesta HTTP;
- simboli Portable PDB distribuiti anche come `.snupkg`.

## Installazione da GitHub Packages

Autenticare il feed del proprietario del repository:

```powershell
dotnet nuget add source `
  --username GITHUB_USER `
  --password GITHUB_PAT `
  --store-password-in-clear-text `
  --name github `
  "https://nuget.pkg.github.com/GITHUB_OWNER/index.json"
```

Installare il pacchetto:

```powershell
dotnet add package FDR11.Logger --version 1.0.0 --source github
```

Per leggere pacchetti privati, il PAT classic deve avere almeno lo scope
`read:packages`.

## ASP.NET Core

```csharp
using FDR11.Logger;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddFdr11Logging(options =>
{
    options.ApplicationName = "Orders.Api";
    options.MinimumLevel = LogEventLevel.Information;
    options.MicrosoftMinimumLevel = LogEventLevel.Warning;
    options.FilePath = "logs/orders-.json";
    options.RetainedFileCountLimit = 14;
});

var app = builder.Build();

// Inserire all'inizio della pipeline per misurare l'intera richiesta.
app.UseFdr11RequestLogging();

app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation(
        "Ordine {OrderId} elaborato per {CustomerId}",
        1234,
        "C-42");

    return Results.Ok();
});

app.Run();
```

Usare sempre message template e proprietà nominate; evitare interpolazione o
concatenazione:

```csharp
logger.LogInformation("Ordine {OrderId} creato", orderId);
```

## Worker service o applicazione console

```csharp
using FDR11.Logger;

var builder = Host.CreateApplicationBuilder(args);
builder.AddFdr11Logging(options =>
{
    options.ApplicationName = "Orders.Worker";
    options.WriteToFile = false;
});

using var host = builder.Build();
await host.RunAsync();
```

## Formato

Ogni riga è un documento JSON. Esempio abbreviato:

```json
{
  "@t": "2026-07-27T13:45:12.3456789Z",
  "@m": "Ordine 1234 elaborato per C-42",
  "OrderId": 1234,
  "Application": "Orders.Api",
  "Environment": "Production",
  "CorrelationId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "SpanId": "00f067aa0ba902b7"
}
```

Il timestamp `@t` è in UTC. Gli eventi Warning aggiungono `@l`; le eccezioni
aggiungono `@x`.

## Creazione locale dei pacchetti

```powershell
dotnet restore
dotnet pack --configuration Release --output artifacts
```

Il comando produce:

- `artifacts/FDR11.Logger.1.0.0.nupkg`
- `artifacts/FDR11.Logger.1.0.0.snupkg`

## Rilascio

1. Creare un repository GitHub con questa cartella come root.
2. Eseguire commit e push.
3. Creare e pubblicare un tag SemVer:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

Il workflow pubblica il pacchetto su GitHub Packages e crea una GitHub Release
con entrambi gli artefatti.

Un'applicazione ASP.NET Core eseguibile è disponibile in
`samples/FDR11.Logger.Sample`.
