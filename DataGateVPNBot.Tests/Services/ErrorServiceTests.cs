using DataGateVPNBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class ErrorServiceTests
{
    [Fact]
    public void LogErrorToDatabase_Does_Not_Throw_When_Exception_And_Null_Context()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Test");
        var logger = Mock.Of<ILogger<ErrorService>>();
        var sut = new ErrorService(serviceProvider, env.Object, logger);

        var ex = new InvalidOperationException("Test error");

        sut.LogErrorToDatabase(ex, null);
    }

    [Fact]
    public void LogErrorToDatabase_Truncates_Long_Message()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Test");
        var logger = Mock.Of<ILogger<ErrorService>>();
        var sut = new ErrorService(serviceProvider, env.Object, logger);

        var longMessage = new string('x', 5000);
        var ex = new InvalidOperationException(longMessage);

        sut.LogErrorToDatabase(ex, null);
    }
}
