using M365Trace.Web.Services;

namespace M365Trace.Web.Tests;

public sealed class DefaultBrowserLauncherTests
{
    [Theory]
    [InlineData(8080, "http://localhost:8080/")]
    [InlineData(8443, "http://localhost:8443/")]
    public void GetBrowserUrl_ReturnsLoopbackAddress(
        int port,
        string expected)
    {
        var result = DefaultBrowserLauncher.GetBrowserUrl(port);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void GetBrowserUrl_RejectsInvalidPort(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DefaultBrowserLauncher.GetBrowserUrl(port));
    }
}
