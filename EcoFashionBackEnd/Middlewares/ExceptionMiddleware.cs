using System.Text.Json;
using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Exceptions;

namespace EcoFashionBackEnd.Middlewares;

public class ExceptionMiddleware : IMiddleware
{
    private readonly IDictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public ExceptionMiddleware()
    {
        _exceptionHandlers = new Dictionary<Type, Func<HttpContext, Exception, Task>>
        {
            { typeof(NotFoundException), HandleNotFoundExceptionAsync },
            { typeof(BadRequestException), HandleBadRequestExceptionAsync },
            { typeof(UnauthorizedException), HandleUnauthorizedExceptionAsync },
            { typeof(ForbiddenException), HandleForbiddenExceptionAsync }
        };
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var type = ex.GetType();
        if (_exceptionHandlers.TryGetValue(type, out var handler))
        {
            return handler(context, ex);
        }

        // fallback cho lỗi chưa handle
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        Console.WriteLine($"[Unhandled Exception] {ex}");
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Internal Server Error",
            Details = ex.Message
        }));
    }

    private static Task HandleNotFoundExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return WriteExceptionMessageAsync(context, ex);
    }

    private static Task HandleBadRequestExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return WriteExceptionMessageAsync(context, ex);
    }

    private static Task WriteExceptionMessageAsync(HttpContext context, Exception ex)
    {
        var error = ApiResult<string>.Fail(ex);
        return context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
    private static Task HandleUnauthorizedExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = "Unauthorized",
            message = "You are not authenticated to access this resource."
        }));
    }

    private static Task HandleForbiddenExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = "Forbidden",
            message = "You don't have permission to perform this action."
        }));
    }
}
