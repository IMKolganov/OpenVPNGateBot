using System.Net;
using System.Text;
using DataGateVPNBot.Middlewares;
using Microsoft.AspNetCore.Http;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_When_Next_Throws_Sets_500_And_Writes_Json_Response()
    {
        var services = new ServiceCollection();
        var errorService = new Mock<IErrorService>();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = serviceProvider;

        RequestDelegate next = _ => throw new InvalidOperationException("Test error");
        var sut = new GlobalExceptionMiddleware(next, serviceProvider, Mock.Of<ILogger<GlobalExceptionMiddleware>>());

        await sut.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("unexpected error", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Test error", body);

        errorService.Verify(e => e.LogErrorToDatabase(It.IsAny<Exception>(), context), Times.Once);
        errorService.Verify(e => e.NotifyAdminsAboutExceptionAsync(It.IsAny<Exception>(), context, default), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_When_Next_Succeeds_Does_Not_Change_Response()
    {
        var services = new ServiceCollection();
        services.AddScoped<IErrorService>(_ => Mock.Of<IErrorService>());
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = serviceProvider;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var sut = new GlobalExceptionMiddleware(next, serviceProvider, Mock.Of<ILogger<GlobalExceptionMiddleware>>());

        await sut.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }
}
