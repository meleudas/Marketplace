using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Marketplace.API.OpenApi;

internal static class EndpointDocumentationApplier
{
    public static void Apply(OpenApiOperation operation, EndpointDocEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Summary))
            operation.Summary = entry.Summary;

        operation.Description = entry.BuildDescriptionMarkdown();
        ApplyExtensions(operation, entry);
    }

    private static void ApplyExtensions(OpenApiOperation operation, EndpointDocEntry entry)
    {
        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();

        SetExtension(operation, "x-required-global-roles", entry.GlobalRoles);
        SetExtension(operation, "x-required-company-roles", entry.CompanyRoles);
        SetExtension(operation, "x-frontend-status", entry.FrontendStatus);
        SetExtension(operation, "x-frontend-module", entry.FrontendModule);
        SetExtension(operation, "x-notification-templates", entry.NotificationTemplates);
        SetExtension(operation, "x-idempotency-required", entry.IdempotencyRequired);
    }

    private static void SetExtension(OpenApiOperation operation, string name, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
            return;

        var array = new JsonArray();
        foreach (var value in values)
            array.Add(value);

        operation.Extensions![name] = new JsonNodeExtension(array);
    }

    private static void SetExtension(OpenApiOperation operation, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        operation.Extensions![name] = new JsonNodeExtension(JsonValue.Create(value));
    }

    private static void SetExtension(OpenApiOperation operation, string name, bool value)
    {
        operation.Extensions![name] = new JsonNodeExtension(JsonValue.Create(value));
    }
}
