using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Marketplace.API.OpenApi;

public sealed class EndpointDocumentationOperationFilter(EndpointDocRegistry registry) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "GET";
        var path = context.ApiDescription.RelativePath ?? "/";
        if (!registry.TryGet(method, path, out var entry))
            return;

        EndpointDocumentationApplier.Apply(operation, entry);
    }
}

public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info ??= new OpenApiInfo();
        swaggerDoc.Info.Description = OpenApiTagDefinitions.BuildDocumentDescription();

        if (swaggerDoc.Tags is null)
            return;

        foreach (var tag in swaggerDoc.Tags)
        {
            if (tag.Name is not null && OpenApiTagDefinitions.Tags.TryGetValue(tag.Name, out var description))
                tag.Description = description;
        }
    }
}
