using System.Net;
using System.Text.Json;
using VTCStockManagementCase.Application;
using VTCStockManagementCase.Domain.Exceptions;

namespace VTCStockManagementCase.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business exception {Code}", ex.Code);
            var status = MapStatus(ex.Code);
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            var body = new { code = ex.Code, message = ex.Message, details = ex.Details };
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOpts));
        }
    }

    private static HttpStatusCode MapStatus(string code) => code switch
    {
        ErrorCodes.OrderNotFound or ErrorCodes.ProductNotFound => HttpStatusCode.NotFound,
        ErrorCodes.InsufficientStock or ErrorCodes.DuplicateSku => HttpStatusCode.Conflict,
        ErrorCodes.InvalidOrderState => HttpStatusCode.UnprocessableEntity,
        _ => HttpStatusCode.BadRequest
    };
}
