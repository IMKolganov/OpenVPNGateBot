using DataGateVPNBot.Middlewares;
using Xunit;

namespace DataGateVPNBot.Tests;

public class TmaAuthMiddlewareTests
{
    [Fact]
    public void Parse_Valid_Query_Returns_TmaInitData()
    {
        var query = "query_id=abc&auth_date=1234567890&hash=xyz&user=%7B%22id%22%3A42%7D";
        var result = TmaAuthMiddleware.Parse(query, out var error);

        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal("abc", result!.QueryId);
        Assert.Equal(1234567890L, result.AuthDate);
        Assert.Equal("xyz", result.Hash);
        Assert.Equal(42L, result.UserId);
    }

    [Fact]
    public void Parse_Empty_Query_Returns_Data_With_Defaults()
    {
        var result = TmaAuthMiddleware.Parse("", out var error);

        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal(0L, result!.AuthDate);
        Assert.Null(result.QueryId);
        Assert.Null(result.Hash);
    }

    [Fact]
    public void Parse_With_AuthDate_And_CanSendAfter_Parses_Numbers()
    {
        var query = "auth_date=999&can_send_after=888&hash=h";
        var result = TmaAuthMiddleware.Parse(query, out var error);

        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal(999L, result!.AuthDate);
        Assert.Equal(888L, result.CanSendAfter);
    }
}
