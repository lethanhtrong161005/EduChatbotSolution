using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Presentation.Settings;
using System.Runtime.ExceptionServices;

namespace Presentation.Middleware;

public class CustomExceptionMiddleware : IMiddleware
{
    private const int DefaultErrorStatusCode = StatusCodes.Status500InternalServerError;

    private static readonly Dictionary<Type, int> ErrorStatusCodes = new()
    {
        { typeof(BadRequestException), StatusCodes.Status400BadRequest },
        { typeof(UserClaimException), StatusCodes.Status401Unauthorized },
        { typeof(EntityNotFoundException), StatusCodes.Status404NotFound },
        { typeof(EntityConstraintException), StatusCodes.Status422UnprocessableEntity },
    };

    private static readonly HashSet<string> AllowedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.AccessControlAllowCredentials,
        HeaderNames.AccessControlAllowHeaders,
        HeaderNames.AccessControlAllowMethods,
        HeaderNames.AccessControlAllowOrigin,
        HeaderNames.AccessControlExposeHeaders,
        HeaderNames.AccessControlMaxAge,

        HeaderNames.WWWAuthenticate,

        HeaderNames.StrictTransportSecurity,
    };

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ExceptionDispatchInfo? edi = null;

        try
        {
            await next.Invoke(context);

            if (IsProblem(context))
            {
                await HandleProblem(context, next);
            }
        }
        catch (Exception ex)
        {
            // Get the Exception, but don't continue processing in the catch block as its bad for stack usage.
            // cf. Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl
            edi = ExceptionDispatchInfo.Capture(ex);
        }

        if (edi != null)
        {
            await HandleException(context, edi, next);
        }
    }

    private async Task HandleProblem(HttpContext context, RequestDelegate next)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Status = statusCode,
        };

        ClearResponse(context);

        context.Response.StatusCode = statusCode;

        await WriteError(context, problemDetails, next);
    }

    private async Task HandleException(HttpContext context, ExceptionDispatchInfo edi, RequestDelegate next)
    {
        if (context.Response.HasStarted)
        {
            edi.Throw();
        }

        var originalPath = context.Request.Path;

        try
        {
            var exception = edi.SourceException;

            var feature = new ExceptionHandlerFeature
            {
                Error = exception,
                Path = context.Request.Path,
                Endpoint = context.GetEndpoint(),
                RouteValues = context.Features.Get<IRouteValuesFeature>()?.RouteValues,
            };

            ClearResponse(context);

            context.Features.Set<IExceptionHandlerFeature>(feature);
            context.Features.Set<IExceptionHandlerPathFeature>(feature);
            context.Response.StatusCode = ErrorStatusCodes.TryGetValue(exception.GetType(), out var statusCode) ? statusCode : DefaultErrorStatusCode;

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.io/{statusCode}",
                Title = exception.GetType().Name,
                Detail = exception.GetAllExceptionMessages(),
                Status = context.Response.StatusCode,
            };

            await WriteError(context, problemDetails, next);
            return;
        }
        catch (Exception)
        {
            // Suppress secondary exceptions; re-throw the original below
        }
        finally
        {
            context.Request.Path = originalPath;
        }

        edi.Throw();
    }

    private async Task WriteError(HttpContext context, ProblemDetails problemDetails, RequestDelegate next)
    {
        context.Items[ErrorHandlingDefaults.ProblemDetailsHttpContextItemName] = problemDetails;

        // Re-execute the pipeline, this time to the error page
        context.Request.Method = HttpMethod.Get.Method;
        context.Request.Path = ErrorHandlingDefaults.ErrorPagePath;
        context.SetEndpoint(null);
        await next.Invoke(context);
    }

    private static bool IsProblem(HttpContext context)
    {
        return (context.Response.StatusCode is >= 400 and < 600)
               && !context.Response.ContentLength.HasValue
               && string.IsNullOrEmpty(context.Response.ContentType);
    }

    // cf. https://github.com/khellang/Middleware/blob/master/src/ProblemDetails/ProblemDetailsMiddleware.cs
    private static void ClearResponse(HttpContext context)
    {
        var headers = new HeaderDictionary();

        foreach (var header in context.Response.Headers)
        {
            if (AllowedHeaderNames.Contains(header.Key))
            {
                headers.Add(header);
            }
        }

        context.Response.Clear();

        foreach (var header in headers)
        {
            // > Because the CORS middleware adds all the headers early in the pipeline,
            // we want to copy over the existing Access-Control-* headers after resetting the response.
            //
            // > Well... this middleware currently only functions when placed _before_ the routing middleware (cf. _::WriteError),
            // which itself is before CORS. Ergo, we're leaving this here for posterity.
            context.Response.Headers.Add(header);
        }

        SetCacheClearHeaders(context.Response);
    }

    private static void SetCacheClearHeaders(HttpResponse response)
    {
        var headers = response.Headers;
        headers.CacheControl = "no-cache, no-store, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
        headers.ETag = default;
    }
}

internal static class ExceptionExtensions
{
    public static string GetAllExceptionMessages(this Exception? ex)
    {
        return ex == null
            ? string.Empty
            : $"{ex.Message}{Environment.NewLine}{ex.InnerException.GetAllExceptionMessages()}";
    }
}
