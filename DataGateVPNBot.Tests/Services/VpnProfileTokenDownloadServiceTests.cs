using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class VpnProfileTokenDownloadServiceTests
{
    [Fact]
    public async Task DownloadByTokenAsync_ReturnsOpenVpnFile_WhenOpenVpnSucceeds()
    {
        var ovpn = new Mock<IOvpnFileService>(MockBehavior.Strict);
        ovpn.Setup(s => s.DownloadOvpnFileByTokenAsync("tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadFileResponse
            {
                Content = [1, 2],
                IssuedOvpn = new IssuedOvpnFileDto { FileName = "client.ovpn" }
            });
        var xray = new Mock<IXrayClientLinkBotService>(MockBehavior.Strict);
        var sut = new VpnProfileTokenDownloadService(ovpn.Object, xray.Object,
            Mock.Of<ILogger<VpnProfileTokenDownloadService>>());

        var result = await sut.DownloadByTokenAsync("tok", CancellationToken.None);

        Assert.Equal("client.ovpn", result.FileName);
        Assert.Equal(new byte[] { 1, 2 }, result.Content);
        xray.Verify(s => s.DownloadClientLinkByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadByTokenAsync_FallsBackToXray_WhenOpenVpnFileNotFound()
    {
        var ovpn = new Mock<IOvpnFileService>(MockBehavior.Strict);
        ovpn.Setup(s => s.DownloadOvpnFileByTokenAsync("tok", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing"));
        var xray = new Mock<IXrayClientLinkBotService>(MockBehavior.Strict);
        xray.Setup(s => s.DownloadClientLinkByTokenAsync("tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = [9],
                IssuedXrayClientLink = new IssuedXrayClientLinkDto { FileName = "vless.txt" }
            });
        var sut = new VpnProfileTokenDownloadService(ovpn.Object, xray.Object,
            Mock.Of<ILogger<VpnProfileTokenDownloadService>>());

        var result = await sut.DownloadByTokenAsync("tok", CancellationToken.None);

        Assert.Equal("vless.txt", result.FileName);
        Assert.Equal(new byte[] { 9 }, result.Content);
    }
}
