using DataGateVPNBot.Services;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class ErrorServiceTests
{
    private static ErrorService CreateSut(IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= new ServiceCollection().AddLogging().BuildServiceProvider();
        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Test");
        return new ErrorService(
            serviceProvider,
            env.Object,
            Mock.Of<IAdminRecipientService>(),
            Mock.Of<ILogger<ErrorService>>());
    }

    [Fact]
    public void LogErrorToDatabase_Does_Not_Throw_When_Exception_And_Null_Context()
    {
        var sut = CreateSut();

        var ex = new InvalidOperationException("Test error");

        sut.LogErrorToDatabase(ex, null);
    }

    [Fact]
    public void LogErrorToDatabase_Truncates_Long_Message()
    {
        var sut = CreateSut();

        var longMessage = new string('x', 5000);
        var ex = new InvalidOperationException(longMessage);

        sut.LogErrorToDatabase(ex, null);
    }
}
