using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Marketplace.API.OpenApi;

public sealed class EndpointDocumentationTransformer(EndpointDocRegistry registry) : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var description = context.Description;
        var method = description.HttpMethod ?? "GET";
        var path = description.RelativePath ?? "/";

        if (registry.TryGet(method, path, out var entry))
            EndpointDocumentationApplier.Apply(operation, entry);

        return Task.CompletedTask;
    }
}

public sealed class TagDescriptionsDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title ??= "Marketplace API";
        document.Info.Version ??= "v1";
        document.Info.Description = OpenApiTagDefinitions.BuildDocumentDescription();

        if (document.Tags is not null)
        {
            foreach (var tag in document.Tags)
            {
                if (tag.Name is null)
                    continue;

                if (OpenApiTagDefinitions.Tags.TryGetValue(tag.Name, out var description))
                    tag.Description = description;
            }
        }

        return Task.CompletedTask;
    }
}
