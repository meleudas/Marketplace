using Marketplace.API.Extensions;
using Scalar.AspNetCore;

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

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();
