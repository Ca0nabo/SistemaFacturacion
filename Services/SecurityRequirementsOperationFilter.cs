using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SistemaFacturacion.Services;

public class SecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document, null), [] }
            }
        ];
    }
}
