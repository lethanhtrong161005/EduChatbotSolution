using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Presentation.Middleware;

namespace UnitTests;

[TestFixture]
public class CustomExceptionMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_EntityValidationException_SetsBadRequestStatus()
    {
        await AssertStatusCodeAsync(
            new EntityValidationException("The supplied entity is invalid."),
            StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task InvokeAsync_EntityConflictException_SetsConflictStatus()
    {
        await AssertStatusCodeAsync(
            new EntityConflictException("The entity state conflicts with the operation."),
            StatusCodes.Status409Conflict);
    }

    private static async Task AssertStatusCodeAsync(Exception exception, int expectedStatusCode)
    {
        var middleware = new CustomExceptionMiddleware();
        var context = new DefaultHttpContext();
        var invocationCount = 0;

        RequestDelegate next = _ =>
        {
            invocationCount++;

            return invocationCount == 1
                ? Task.FromException(exception)
                : Task.CompletedTask;
        };

        await middleware.InvokeAsync(context, next);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(expectedStatusCode));
            Assert.That(invocationCount, Is.EqualTo(2));
        }
    }
}
