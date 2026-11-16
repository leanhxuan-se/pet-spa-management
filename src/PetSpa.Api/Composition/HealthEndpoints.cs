// Đăng ký và cấu hình thành phần HealthEndpoints.
namespace PetSpa.Api.Composition;

public static class HealthEndpoints
{
    // Đăng ký endpoint kiểm tra tình trạng ứng dụng.
    public static void MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).AllowAnonymous();
    }
}
