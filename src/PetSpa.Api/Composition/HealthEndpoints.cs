namespace PetSpa.Api.Composition;

public static class HealthEndpoints
{
    public static void MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).AllowAnonymous();
    }
}
