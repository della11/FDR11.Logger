using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace FDR11.Logger;

/// <summary>
/// Propaga o genera un correlation ID e lo aggiunge al contesto Serilog.
/// </summary>
public sealed class Fdr11CorrelationIdMiddleware
{
    private const int MaxCorrelationIdLength = 128;

    private readonly RequestDelegate _next;
    private readonly Fdr11LoggerOptions _options;

    /// <summary>Crea il middleware.</summary>
    public Fdr11CorrelationIdMiddleware(
        RequestDelegate next,
        Fdr11LoggerOptions options)
    {
        _next = next;
        _options = options;
    }

    /// <summary>Esegue il middleware per la richiesta corrente.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.Response.Headers[_options.CorrelationHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        var candidate = context.Request.Headers[_options.CorrelationHeaderName]
            .FirstOrDefault();

        if (IsValid(candidate))
        {
            return candidate!;
        }

        var traceId = Activity.Current?.TraceId.ToHexString();
        return string.IsNullOrEmpty(traceId)
            ? Guid.NewGuid().ToString("N")
            : traceId;
    }

    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        return value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_' or '.' or ':');
    }
}
