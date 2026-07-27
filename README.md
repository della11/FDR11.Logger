# FDR11.Logger

![FDR11.Logger](https://raw.githubusercontent.com/della11/FDR11.Logger/main/Code/assets/fdr11-logger-icon.png)

`FDR11.Logger` applica una configurazione Serilog uniforme alle applicazioni
.NET 10 e ASP.NET Core. Produce eventi strutturati in formato CLEF/JSON,
aggiunge automaticamente informazioni applicative e di tracing e standardizza
il logging delle richieste HTTP.

## Funzionalità

- output CLEF/JSON compatto su console e file;
- file rolling giornaliero con limite di dimensione e retention configurabili;
- proprietà standard `Application`, `Environment`, `MachineName`, `ProcessId`
  e `ThreadId`;
- proprietà distribuite `CorrelationId`, `TraceId` e `SpanId`;
- propagazione del correlation ID tramite `X-Correlation-ID`;
- un singolo evento strutturato di completamento per ogni richiesta HTTP;
- livelli separati per applicazione e log Microsoft/System;
- Portable PDB pubblicati nel pacchetto simboli `.snupkg`.

## Requisiti

- .NET 10;
- ASP.NET Core 10 oppure un'applicazione basata su Generic Host.

## Installazione

Da NuGet.org:

```powershell
dotnet add package FDR11.Logger --version 1.0.1
```

Oppure aggiungere direttamente il riferimento al progetto:

```xml
<PackageReference Include="FDR11.Logger" Version="1.0.1" />
```

Le dipendenze Serilog necessarie vengono installate automaticamente dal
pacchetto.

## Integrazione ASP.NET Core

Registrare il logging sul `WebApplicationBuilder`, costruire l'applicazione e
aggiungere il middleware di request logging prima degli endpoint:

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

app.UseFdr11RequestLogging();

app.MapGet("/orders/{orderId:int}", (
    int orderId,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "Ordine {OrderId} elaborato per {CustomerId}",
        orderId,
        "C-42");

    return Results.Ok(new { orderId });
});

app.Run();
```

Se l'applicazione usa `UseForwardedHeaders`, chiamarlo prima di
`UseFdr11RequestLogging`, in modo che indirizzo IP, schema e host descrivano la
richiesta originale. Il middleware FDR11 deve precedere i componenti dei quali
si vuole misurare il tempo di esecuzione.

## Worker service o applicazione console

Per un Worker o un'applicazione basata su Generic Host non serve il middleware
HTTP:

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

## Opzioni

| Opzione | Valore predefinito | Descrizione |
| --- | --- | --- |
| `ApplicationName` | Nome dell'applicazione host | Proprietà `Application` aggiunta a ogni evento. |
| `EnvironmentName` | Ambiente dell'host | Proprietà `Environment`, ad esempio `Development` o `Production`. |
| `MinimumLevel` | `Information` | Livello minimo generale. |
| `MicrosoftMinimumLevel` | `Warning` | Livello minimo per le categorie Microsoft e System. |
| `WriteToConsole` | `true` | Scrive eventi CLEF/JSON sulla console. |
| `WriteToFile` | `true` | Scrive eventi CLEF/JSON su file. |
| `FilePath` | `logs/log-.json` | Percorso del file rolling giornaliero. |
| `RetainedFileCountLimit` | `31` | Numero massimo di file conservati. |
| `FileSizeLimitBytes` | `104857600` | Dimensione massima di ogni file, pari a 100 MiB. |
| `CorrelationHeaderName` | `X-Correlation-ID` | Header HTTP letto e restituito dal middleware. |

Almeno una tra `WriteToConsole` e `WriteToFile` deve essere abilitata. Se si
usa il file sink, il processo deve avere i permessi di scrittura sulla
directory configurata.

## Logging strutturato

Usare `ILogger<T>` tramite dependency injection e conservare i valori come
proprietà nominate:

```csharp
logger.LogInformation(
    "Ordine {OrderId} creato per {CustomerId}",
    orderId,
    customerId);
```

Evitare interpolazione e concatenazione, che trasformano i dati in semplice
testo:

```csharp
// Da evitare
logger.LogInformation($"Ordine {orderId} creato");
```

## Correlation ID e request logging

Per ogni richiesta HTTP il middleware:

1. usa il valore dell'header configurato, se valido;
2. in assenza dell'header usa il `TraceId` dell'attività corrente;
3. se non esiste un'attività genera un identificatore;
4. restituisce il correlation ID nella risposta;
5. aggiunge `CorrelationId` agli eventi emessi durante la richiesta.

L'evento conclusivo HTTP contiene inoltre metodo, percorso, status code,
durata, host, protocollo, schema, IP client, user agent ed endpoint.

## Formato di output

Ogni riga è un documento JSON. Esempio abbreviato:

```json
{
  "@t": "2026-07-27T13:45:12.3456789Z",
  "@m": "Ordine 1234 elaborato per C-42",
  "OrderId": 1234,
  "CustomerId": "C-42",
  "Application": "Orders.Api",
  "Environment": "Production",
  "CorrelationId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "SpanId": "00f067aa0ba902b7"
}
```

Il timestamp `@t` è espresso in UTC. Gli eventi Warning aggiungono `@l`; le
eccezioni aggiungono `@x`.

## Collegamenti

- [Codice sorgente](https://github.com/della11/FDR11.Logger)
- [Segnalazione problemi](https://github.com/della11/FDR11.Logger/issues)
