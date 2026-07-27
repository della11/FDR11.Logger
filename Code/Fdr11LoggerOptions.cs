using Serilog.Events;

namespace FDR11.Logger;

/// <summary>
/// Configura il formato e le destinazioni di log standard FDR11.
/// </summary>
public sealed class Fdr11LoggerOptions
{
    /// <summary>Nome logico dell'applicazione, aggiunto a ogni evento.</summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>Nome dell'ambiente, ad esempio Development, Staging o Production.</summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>Livello minimo generale.</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>Livello minimo per i log prodotti da Microsoft e dal framework.</summary>
    public LogEventLevel MicrosoftMinimumLevel { get; set; } = LogEventLevel.Warning;

    /// <summary>Abilita l'output CLEF/JSON sulla console.</summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>Abilita l'output CLEF/JSON su file.</summary>
    public bool WriteToFile { get; set; } = true;

    /// <summary>Percorso del file rolling. Il trattino finale viene sostituito dalla data.</summary>
    public string FilePath { get; set; } = "logs/log-.json";

    /// <summary>Numero massimo di file conservati.</summary>
    public int RetainedFileCountLimit { get; set; } = 31;

    /// <summary>Dimensione massima di ogni file, in byte.</summary>
    public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>Nome dell'header HTTP usato per il correlation ID.</summary>
    public string CorrelationHeaderName { get; set; } = "X-Correlation-ID";

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApplicationName))
        {
            throw new InvalidOperationException(
                $"{nameof(ApplicationName)} non può essere vuoto.");
        }

        if (string.IsNullOrWhiteSpace(EnvironmentName))
        {
            throw new InvalidOperationException(
                $"{nameof(EnvironmentName)} non può essere vuoto.");
        }

        if (WriteToFile && string.IsNullOrWhiteSpace(FilePath))
        {
            throw new InvalidOperationException(
                $"{nameof(FilePath)} non può essere vuoto quando il file sink è abilitato.");
        }

        if (RetainedFileCountLimit <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(RetainedFileCountLimit)} deve essere maggiore di zero.");
        }

        if (FileSizeLimitBytes <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(FileSizeLimitBytes)} deve essere maggiore di zero.");
        }

        if (string.IsNullOrWhiteSpace(CorrelationHeaderName))
        {
            throw new InvalidOperationException(
                $"{nameof(CorrelationHeaderName)} non può essere vuoto.");
        }

        if (!WriteToConsole && !WriteToFile)
        {
            throw new InvalidOperationException(
                "Abilitare almeno una destinazione di log.");
        }
    }
}
