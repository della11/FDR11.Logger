using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace FDR11.Logger;

/// <summary>
/// Metodi di estensione per installare la configurazione Serilog standard FDR11.
/// </summary>
public static class Fdr11LoggingExtensions
{
    /// <summary>Registra Serilog come provider di logging e applica il formato standard.</summary>
    public static IHostApplicationBuilder AddFdr11Logging(
        this IHostApplicationBuilder builder,
        Action<Fdr11LoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new Fdr11LoggerOptions
        {
            ApplicationName = builder.Environment.ApplicationName,
            EnvironmentName = builder.Environment.EnvironmentName
        };

        configure?.Invoke(options);
        options.Validate();

        builder.Services.AddSingleton(options);
        builder.Services.AddSerilog(
            (_, loggerConfiguration) => ConfigureLogger(loggerConfiguration, options),
            preserveStaticLogger: false,
            writeToProviders: false);

        return builder;
    }

    /// <summary>
    /// Aggiunge correlation ID e request logging Serilog alla pipeline ASP.NET Core.
    /// Chiamare il metodo prima dei middleware che devono essere misurati.
    /// </summary>
    public static IApplicationBuilder UseFdr11RequestLogging(
        this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<Fdr11CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(requestLogging =>
        {
            requestLogging.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            requestLogging.GetLevel = GetRequestLevel;
            requestLogging.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set(
                    "ClientIp",
                    httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set(
                    "UserAgent",
                    httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set(
                    "EndpointName",
                    httpContext.GetEndpoint()?.DisplayName);
            };
        });

        return app;
    }

    private static void ConfigureLogger(
        LoggerConfiguration loggerConfiguration,
        Fdr11LoggerOptions options)
    {
        loggerConfiguration
            .MinimumLevel.Is(options.MinimumLevel)
            .MinimumLevel.Override("Microsoft", options.MicrosoftMinimumLevel)
            .MinimumLevel.Override("System", options.MicrosoftMinimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.With<ActivityEnricher>()
            .Enrich.WithProperty("Application", options.ApplicationName)
            .Enrich.WithProperty("Environment", options.EnvironmentName);

        if (options.WriteToConsole)
        {
            loggerConfiguration.WriteTo.Console(
                new RenderedCompactJsonFormatter());
        }

        if (options.WriteToFile)
        {
            loggerConfiguration.WriteTo.File(
                new RenderedCompactJsonFormatter(),
                options.FilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                fileSizeLimitBytes: options.FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true);
        }
    }

    private static LogEventLevel GetRequestLevel(
        HttpContext context,
        double _,
        Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        return context.Response.StatusCode >= 400
            ? LogEventLevel.Warning
            : LogEventLevel.Information;
    }
}
