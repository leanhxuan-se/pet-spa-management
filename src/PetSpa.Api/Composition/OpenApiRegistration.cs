// Đăng ký và cấu hình thành phần OpenApiRegistration.
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Api.Composition;

public static class OpenApiRegistration
{
    private const string BearerScheme = "bearerAuth";

    // Đăng ký tài liệu OpenAPI và cấu hình bảo mật.
    public static IServiceCollection AddPetSpaOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security = [];
                    return Task.CompletedTask;
                }

                // All other endpoints are covered by an explicit policy or the fallback policy.
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
                    }
                ];
                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Authentication required." });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Access denied." });
                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(LoginResponse) &&
                    schema.Properties?["role"] is OpenApiSchema role)
                {
                    schema.Required = new HashSet<string> { "accessToken", "tokenType", "expiresAtUtc", "role", "user" };
                    role.Enum =
                    [
                        JsonValue.Create(AccountRoles.Customer),
                        JsonValue.Create(AccountRoles.Staff),
                        JsonValue.Create(AccountRoles.Receptionist),
                        JsonValue.Create(AccountRoles.Manager),
                        JsonValue.Create(AccountRoles.Admin)
                    ];
                    if (schema.Properties["tokenType"] is OpenApiSchema tokenType)
                        tokenType.Enum = [JsonValue.Create("Bearer")];
                }
                else if (context.JsonTypeInfo.Type == typeof(LoginUser))
                {
                    schema.Required = new HashSet<string> { "id", "fullName", "phone" };
                }
                return Task.CompletedTask;
            });
        });
        return services;
    }
}
