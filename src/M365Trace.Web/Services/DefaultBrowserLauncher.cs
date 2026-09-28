using System.ComponentModel;
using System.Diagnostics;

namespace M365Trace.Web.Services;

public sealed class DefaultBrowserLauncher(
    ILogger<DefaultBrowserLauncher> logger)
{
    public const string DisableBrowserLaunchEnvironmentVariable =
        "M365_TRACE_DISABLE_BROWSER_LAUNCH";

    public void TryLaunch(int port)
    {
        if (IsBrowserLaunchDisabled() || !OperatingSystem.IsWindows())
        {
            return;
        }

        var browserUrl = GetBrowserUrl(port);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = browserUrl,
                UseShellExecute = true
            });
            logger.LogInformation(
                "Opened {BrowserUrl} in the default browser.",
                browserUrl);
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "The application started at {BrowserUrl}, but the default "
                + "browser could not be opened.",
                browserUrl);
        }
    }

    public static string GetBrowserUrl(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                "The port must be from 1 through 65535.");
        }

        return new UriBuilder(Uri.UriSchemeHttp, "localhost", port)
            .Uri
            .AbsoluteUri;
    }

    private static bool IsBrowserLaunchDisabled()
    {
        var value = Environment.GetEnvironmentVariable(
            DisableBrowserLaunchEnvironmentVariable);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
