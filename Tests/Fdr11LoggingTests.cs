using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FDR11.Logger.Tests;

public sealed class Fdr11LoggingTests
{
    [Fact]
    public void Structured_event_is_written_with_standard_properties()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "FDR11.Logger.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var filePath = Path.Combine(temporaryDirectory, "test-.json");
            var builder = Host.CreateApplicationBuilder();
            builder.AddFdr11Logging(options =>
            {
                options.ApplicationName = "Orders.Tests";
                options.EnvironmentName = "Test";
                options.WriteToConsole = false;
                options.FilePath = filePath;
            });

            using (var host = builder.Build())
            {
                var logger = host.Services.GetRequiredService<
                    ILogger<Fdr11LoggingTests>>();
                logger.LogInformation(
                    "Ordine {OrderId} creato per {CustomerId}",
                    42,
                    "C-42");
            }

            var logFile = Assert.Single(Directory.GetFiles(temporaryDirectory));
            var matchingEvent = File.ReadLines(logFile)
                .Select(line => JsonDocument.Parse(line))
                .First(document =>
                    document.RootElement.TryGetProperty("OrderId", out _));

            using (matchingEvent)
            {
                var root = matchingEvent.RootElement;
                Assert.Equal(42, root.GetProperty("OrderId").GetInt32());
                Assert.Equal("C-42", root.GetProperty("CustomerId").GetString());
                Assert.Equal(
                    "Orders.Tests",
                    root.GetProperty("Application").GetString());
                Assert.Equal(
                    "Test",
                    root.GetProperty("Environment").GetString());
                Assert.True(root.TryGetProperty("MachineName", out _));
                Assert.True(root.TryGetProperty("ProcessId", out _));
                Assert.True(root.TryGetProperty("ThreadId", out _));
            }
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Correlation_id_is_propagated_to_the_response()
    {
        const string correlationId = "client-order-42";
        var options = new Fdr11LoggerOptions();
        var context = new DefaultHttpContext();
        context.Request.Headers[options.CorrelationHeaderName] = correlationId;

        var middleware = new Fdr11CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            options);

        await middleware.InvokeAsync(context);

        Assert.Equal(
            correlationId,
            context.Response.Headers[options.CorrelationHeaderName].ToString());
    }

    [Fact]
    public void At_least_one_sink_must_be_enabled()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.AddFdr11Logging(options =>
            {
                options.WriteToConsole = false;
                options.WriteToFile = false;
            }));

        Assert.Contains("almeno una destinazione", exception.Message);
    }
}
