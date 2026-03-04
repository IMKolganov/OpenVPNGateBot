using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class HttpClientFactoryServiceTests
{
    [Fact]
    public void CreateDashboardClient_Calls_Factory_With_DashboardClient_Name()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var client = new HttpClient();
        mockFactory.Setup(f => f.CreateClient("DashboardClient")).Returns(client);
        var logger = Mock.Of<ILogger<HttpClientFactoryService>>();
        var sut = new HttpClientFactoryService(mockFactory.Object, logger);

        var result = sut.CreateDashboardClient();

        Assert.Same(client, result);
        mockFactory.Verify(f => f.CreateClient("DashboardClient"), Times.Once);
    }
}
