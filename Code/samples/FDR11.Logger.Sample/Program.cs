using FDR11.Logger;

var builder = WebApplication.CreateBuilder(args);

builder.AddFdr11Logging(options =>
{
    options.ApplicationName = "FDR11.Logger.Sample";
    options.FilePath = "logs/sample-.json";
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
