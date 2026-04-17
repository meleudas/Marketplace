using Hangfire;
using Marketplace.API.Extensions;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.Jobs;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await Marketplace.Infrastructure.DependencyInjection.InitializeDatabaseAsync(app.Services);
}

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.Title = "Marketplace API";
    options.Theme = ScalarTheme.Mars;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Marketplace API v1");
    options.RoutePrefix = "swagger";
});

app.UseMarketplaceMiddleware();
app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<InventoryJobs>(
    "inventory-expire-reservations",
    job => job.ExpireReservationsAsync(default),
    Cron.Minutely);
RecurringJob.AddOrUpdate<SearchIndexJobs>(
    "search-full-reindex-products",
    job => job.FullReindexAsync(default),
    Cron.Daily(2));

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.MapGet("/health/sendgrid", async (IEmailHealthProbe probe, CancellationToken ct) =>
{
    var status = await probe.CheckAsync(ct);
    return status.IsHealthy
        ? Results.Ok(status)
        : Results.Problem(
            title: "SendGrid health check failed",
            detail: status.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
    .WithName("SendGridHealthCheck")
    .WithTags("Health");

app.MapGet("/health/sendgrid/key-trace", (IConfiguration cfg, IOptions<SendGridOptions> options) =>
{
    var envApiKey = Environment.GetEnvironmentVariable("SENDGRID__APIKEY") ?? string.Empty;
    var configApiKey = cfg["SendGrid:ApiKey"] ?? string.Empty;
    var optionsApiKey = options.Value.ApiKey ?? string.Empty;

    var response = new
    {
        env = BuildKeySnapshot(envApiKey),
        config = BuildKeySnapshot(configApiKey),
        options = BuildKeySnapshot(optionsApiKey),
        sameFingerprint = !string.IsNullOrEmpty(envApiKey) &&
                          Fingerprint(envApiKey) == Fingerprint(configApiKey) &&
                          Fingerprint(configApiKey) == Fingerprint(optionsApiKey)
    };

    return Results.Ok(response);
})
    .WithName("SendGridKeyTrace")
    .WithTags("Health");

app.Run();

static object BuildKeySnapshot(string value) => new
{
    present = !string.IsNullOrWhiteSpace(value),
    length = value.Length,
    startsWithSG = value.StartsWith("SG.", StringComparison.Ordinal),
    masked = Mask(value),
    fingerprint = Fingerprint(value)
};

static string Mask(string value)
{
    if (string.IsNullOrEmpty(value))
        return string.Empty;
    if (value.Length <= 8)
        return new string('*', value.Length);
    return $"{value[..4]}...{value[^4..]}";
}

static string Fingerprint(string value)
{
    if (string.IsNullOrEmpty(value))
        return string.Empty;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes)[..12];
}
