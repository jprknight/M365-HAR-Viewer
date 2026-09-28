using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SharpZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;
using SharpZipOutputStream = ICSharpCode.SharpZipLib.Zip.ZipOutputStream;
using Microsoft.Playwright;

namespace M365Trace.Web.E2E.Tests;

public sealed class StandaloneApplicationTests
{
    private const string DisableBrowserLaunchEnvironmentVariable =
        "M365_TRACE_DISABLE_BROWSER_LAUNCH";
    private const string EncryptedSazPassword = "E2E P@ssword";
    private const string TestApplicationInsightsConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;"
        + "IngestionEndpoint=https://dc.services.visualstudio.com/";

    [Fact]
    public async Task StandalonePackage_EnforcesLoopbackOnlyListener()
    {
        var executablePath = GetPublishedExecutablePath();
        var publishDirectory = Path.GetDirectoryName(executablePath)!;
        var port = GetAvailablePort();
        var url = $"http://localhost:{port}";
        using var process = StartApplication(
            executablePath,
            publishDirectory,
            $"--port {port}",
            new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = $"http://0.0.0.0:{port}"
            });

        try
        {
            await WaitForApplicationAsync(url, process);

            var listeners = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Where(endpoint => endpoint.Port == port)
                .ToArray();

            Assert.NotEmpty(listeners);
            Assert.All(
                listeners,
                endpoint => Assert.True(
                    IPAddress.IsLoopback(endpoint.Address),
                    $"Port {port} unexpectedly listened on "
                    + $"'{endpoint.Address}'."));
        }
        finally
        {
            await StopProcessAsync(process);
        }
    }

    [Fact]
    public async Task StandalonePackage_RejectsUrlsOptionWithoutListening()
    {
        var executablePath = GetPublishedExecutablePath();
        var publishDirectory = Path.GetDirectoryName(executablePath)!;
        var port = GetAvailablePort();
        using var process = StartApplication(
            executablePath,
            publishDirectory,
            $"--urls http://0.0.0.0:{port}");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(timeout.Token);
        var standardError = await process.StandardError.ReadToEndAsync(
            timeout.Token);

        Assert.Equal(2, process.ExitCode);
        Assert.Contains("--urls option is not supported", standardError);
        Assert.Contains("Use --port", standardError);
        Assert.DoesNotContain(
            IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners(),
            endpoint => endpoint.Port == port);
    }

    [Fact]
    public async Task StandalonePackage_SupportsCriticalInvestigationWorkflow()
    {
        var executablePath = GetPublishedExecutablePath();
        var publishDirectory = Path.GetDirectoryName(executablePath)!;

        var port = GetAvailablePort();
        var url = $"http://localhost:{port}";
        var outputPath = Path.GetTempFileName();
        var errorPath = Path.GetTempFileName();
        var harPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-e2e-{Guid.NewGuid():N}.har");
        var warningHarPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-warning-e2e-{Guid.NewGuid():N}.har");
        var largeHarPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-large-e2e-{Guid.NewGuid():N}.har");
        var sazPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-e2e-{Guid.NewGuid():N}.saz");
        var encryptedSazPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-encrypted-e2e-{Guid.NewGuid():N}.saz");
        var telemetrySettingsPath = Path.Combine(
            Path.GetTempPath(),
            $"m365-trace-telemetry-e2e-{Guid.NewGuid():N}.json");
        Process? process = null;

        try
        {
            await File.WriteAllTextAsync(harPath, CreateHar());
            await File.WriteAllTextAsync(
                warningHarPath,
                CreateWarningHar());
            await File.WriteAllTextAsync(largeHarPath, CreateLargeHar());
            CreateSaz(sazPath);
            CreateEncryptedSaz(encryptedSazPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"--port {port}",
                WorkingDirectory = publishDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.Environment[
                DisableBrowserLaunchEnvironmentVariable] = "1";
            startInfo.Environment["ASPNETCORE_URLS"] =
                $"http://0.0.0.0:{port}";
            startInfo.Environment[
                "Telemetry__ApplicationInsightsConnectionString"] =
                TestApplicationInsightsConnectionString;
            startInfo.Environment[
                "Telemetry__SettingsPath"] = telemetrySettingsPath;
            process = Process.Start(startInfo);
            Assert.NotNull(process);

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await WaitForApplicationAsync(url, process);

            using (var hostValidationClient = new HttpClient())
            {
                using var invalidHostRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    url);
                invalidHostRequest.Headers.Host = "untrusted.example";
                using var invalidHostResponse =
                    await hostValidationClient.SendAsync(invalidHostRequest);
                Assert.Equal(
                    HttpStatusCode.BadRequest,
                    invalidHostResponse.StatusCode);
            }

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true });
            await using var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync(
                url,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForFunctionAsync(
                "() => typeof window.Blazor !== 'undefined'");
            await page.WaitForTimeoutAsync(500);

            var telemetryDialog = page.Locator(
                "dialog.telemetry-consent-dialog");
            await telemetryDialog.WaitForAsync();
            Assert.True(await telemetryDialog.EvaluateAsync<bool>(
                "element => element.open && element.matches(':modal')"));
            Assert.Contains(
                "Yes, share anonymous usage",
                await telemetryDialog.InnerTextAsync());
            Assert.Contains(
                "No, do not share",
                await telemetryDialog.InnerTextAsync());
            await telemetryDialog
                .Locator("button")
                .Filter(new LocatorFilterOptions
                {
                    HasText = "No, do not share"
                })
                .ClickAsync();
            await telemetryDialog.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Detached
                });

            await page.Locator("input[type=file]").First.SetInputFilesAsync(harPath);
            await WaitForRowCountWithDiagnosticsAsync(page, 2);

            Assert.Equal(
                0,
                await page
                    .Locator("details.import-quality")
                    .CountAsync());

            var diagnosticHeader = page
                .Locator(".diagnostic-header-row")
                .Filter(new LocatorFilterOptions { HasText = "request-id" });
            await diagnosticHeader.WaitForAsync();
            Assert.Contains("e2e-request-1", await diagnosticHeader.InnerTextAsync());

            var severityMenu = page.Locator(
                "details[data-filter-menu=severity]");
            var statusMenu = page.Locator(
                "details[data-filter-menu=status]");
            var durationMenu = page.Locator(
                "details[data-filter-menu=duration]");
            var findingsMenu = page.Locator(
                "details[data-filter-menu=findings]");
            var ruleMenu = page.Locator(
                "details[data-filter-menu=finding-rule]");
            await severityMenu.Locator("summary").ClickAsync();
            Assert.NotNull(await severityMenu.GetAttributeAsync("open"));

            await statusMenu.Locator("summary").ClickAsync();
            Assert.Null(await severityMenu.GetAttributeAsync("open"));
            Assert.NotNull(await statusMenu.GetAttributeAsync("open"));

            await page.Locator(".panel-heading h2").ClickAsync();
            Assert.Null(await statusMenu.GetAttributeAsync("open"));

            var menuBounds = await severityMenu
                .Locator("summary")
                .BoundingBoxAsync();
            var durationBounds = await durationMenu
                .Locator("summary")
                .BoundingBoxAsync();
            var findingsBounds = await findingsMenu
                .Locator("summary")
                .BoundingBoxAsync();
            var ruleBounds = await ruleMenu
                .Locator("summary")
                .BoundingBoxAsync();
            Assert.NotNull(menuBounds);
            Assert.NotNull(durationBounds);
            Assert.NotNull(findingsBounds);
            Assert.NotNull(ruleBounds);
            Assert.InRange(
                Math.Abs(menuBounds.Y - durationBounds.Y),
                0,
                0.5);
            Assert.InRange(
                Math.Abs(menuBounds.Height - durationBounds.Height),
                0,
                0.5);
            Assert.InRange(
                Math.Abs(ruleBounds.Y - findingsBounds.Y),
                0,
                0.5);
            Assert.InRange(
                Math.Abs(menuBounds.Height - findingsBounds.Height),
                0,
                0.5);

            await page
                .Locator("input.search-box")
                .FillAsync("SERVICE UNAVAILABLE");
            await WaitForRowCountWithDiagnosticsAsync(page, 1);
            await page
                .Locator("tbody tr[data-session-id='2']")
                .WaitForAsync();

            await page.Locator("button.search-clear-button").ClickAsync();
            await WaitForRowCountWithDiagnosticsAsync(page, 2);

            await page.Locator("input.search-box").FillAsync("missing");
            await WaitForRowCountWithDiagnosticsAsync(page, 0);
            Assert.True(await page.Locator("button.search-clear-button").IsVisibleAsync());

            await page.Locator("button.search-clear-button").ClickAsync();
            await WaitForRowCountWithDiagnosticsAsync(page, 2);

            var firstSession = page.Locator(
                "tbody tr[data-session-id='1']");
            var secondSession = page.Locator(
                "tbody tr[data-session-id='2']");
            await firstSession.FocusAsync();
            await firstSession.PressAsync("ArrowDown");
            await secondSession.WaitForAsync();
            await page
                .Locator("tbody tr.selected[data-session-id='2']")
                .WaitForAsync();
            await WaitForActiveSessionAsync(page, 2);
            Assert.Equal(
                "true",
                await secondSession.GetAttributeAsync("aria-selected"));

            await firstSession.ClickAsync();
            await page
                .Locator("tbody tr.selected[data-session-id='1']")
                .WaitForAsync();
            await WaitForActiveSessionAsync(page, 1);
            Assert.Equal(
                1,
                await page.Locator("tbody tr.selected").CountAsync());
            Assert.Equal(
                "solid",
                await firstSession.EvaluateAsync<string>(
                    "element => getComputedStyle(element).outlineStyle"));

            await firstSession.PressAsync("ArrowDown");
            await page
                .Locator("tbody tr.selected[data-session-id='2']")
                .WaitForAsync();
            await WaitForActiveSessionAsync(page, 2);
            Assert.Equal(
                1,
                await page.Locator("tbody tr.selected").CountAsync());
            Assert.Equal(
                "none",
                await firstSession.EvaluateAsync<string>(
                    "element => getComputedStyle(element).outlineStyle"));

            await secondSession.PressAsync("Home");
            await page
                .Locator("tbody tr.selected[data-session-id='1']")
                .WaitForAsync();
            await firstSession.PressAsync("End");
            await page
                .Locator("tbody tr.selected[data-session-id='2']")
                .WaitForAsync();
            await secondSession.PressAsync("ArrowRight");
            Assert.True(await page
                .Locator("[data-session-detail]")
                .EvaluateAsync<bool>("element => element === document.activeElement"));

            await page.Locator("[data-session-detail]").PressAsync("ArrowLeft");
            await WaitForActiveSessionAsync(page, 2);

            await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions { Name = "Request", Exact = true })
                .ClickAsync();
            var requestSummaries = page.Locator("summary").Filter(
                new LocatorFilterOptions { HasText = "Request headers" });
            var responseSummaries = page.Locator("summary").Filter(
                new LocatorFilterOptions { HasText = "Response headers" });

            await requestSummaries.First.WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await responseSummaries.First.WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            Assert.Equal(1, await requestSummaries.CountAsync());
            Assert.Equal(
                0,
                await responseSummaries.CountAsync());

            await page
                .Locator("input[type=file]")
                .First
                .SetInputFilesAsync(sazPath);
            await WaitForLoadedFileAsync(page, sazPath);
            await WaitForRowCountWithDiagnosticsAsync(page, 2);
            Assert.Contains(
                Path.GetFileName(sazPath),
                await page.Locator(".panel-heading h2").InnerTextAsync());
            await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions
                    {
                        Name = "Response",
                        Exact = true
                    })
                .ClickAsync();
            await WaitForBodyTextAsync(page, "saz response body");

            await page
                .Locator("input[type=file]")
                .First
                .SetInputFilesAsync(encryptedSazPath);
            var passwordPrompt = page.Locator(".password-prompt");
            await passwordPrompt.WaitForAsync();
            Assert.Contains(
                Path.GetFileName(encryptedSazPath),
                await passwordPrompt.InnerTextAsync());

            var passwordInput = page.Locator("input.password-input");
            await passwordInput.FillAsync("wrong password");
            await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions
                    {
                        Name = "Open archive",
                        Exact = true
                    })
                .ClickAsync();
            await page
                .Locator("[role=alert]")
                .Filter(new LocatorFilterOptions
                {
                    HasText = "The password is incorrect"
                })
                .WaitForAsync();
            Assert.Equal(string.Empty, await passwordInput.InputValueAsync());

            await passwordInput.FillAsync(EncryptedSazPassword);
            await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions
                    {
                        Name = "Open archive",
                        Exact = true
                    })
                .ClickAsync();
            await passwordPrompt.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Detached
                });
            await WaitForLoadedFileAsync(page, encryptedSazPath);
            await WaitForRowCountWithDiagnosticsAsync(page, 1);
            await WaitForBodyTextAsync(page, "decrypted browser body");

            await page
                .Locator("input[type=file]")
                .First
                .SetInputFilesAsync(largeHarPath);
            await page.WaitForFunctionAsync(
                """
                () => {
                    const grid = document.querySelector(
                        "table[data-session-grid]");
                    const rows = document.querySelectorAll(
                        "tbody tr[data-session-row]");
                    return grid?.dataset.sessionCount === "250"
                        && rows.length > 0
                        && rows.length < 250;
                }
                """);

            var firstLargeSession = page.Locator(
                "tbody tr[data-session-id='1']");
            await firstLargeSession.FocusAsync();
            await firstLargeSession.PressAsync("End");
            await WaitForSelectedSessionWithDiagnosticsAsync(page, 250);
            await WaitForActiveSessionAsync(page, 250);

            await page.SetViewportSizeAsync(1060, 768);
            await page
                .Locator("input[type=file]")
                .First
                .SetInputFilesAsync(warningHarPath);
            await WaitForRowCountWithDiagnosticsAsync(page, 1);

            var importQuality = page.Locator("details.import-quality");
            await importQuality.WaitForAsync();
            Assert.NotNull(await importQuality.GetAttributeAsync("open"));
            Assert.Contains(
                "1 of 2 sessions imported",
                await importQuality.InnerTextAsync());
            var importQualityBounds = await importQuality.BoundingBoxAsync();
            var finalWarningBounds = await importQuality
                .Locator(".import-quality-issues li")
                .Last
                .BoundingBoxAsync();
            Assert.NotNull(importQualityBounds);
            Assert.NotNull(finalWarningBounds);
            Assert.True(
                finalWarningBounds.Y + finalWarningBounds.Height
                <= importQualityBounds.Y + importQualityBounds.Height);
            var importQualityDimensions = await importQuality
                .EvaluateAsync<int[]>(
                    "element => [element.clientHeight, element.scrollHeight]");
            Assert.InRange(
                importQualityDimensions[1] - importQualityDimensions[0],
                0,
                1);

            if (process.HasExited)
            {
                await File.WriteAllTextAsync(outputPath, await outputTask);
                await File.WriteAllTextAsync(errorPath, await errorTask);
                Assert.Fail(
                    $"Application exited unexpectedly. Output: {await File.ReadAllTextAsync(outputPath)} "
                    + $"Error: {await File.ReadAllTextAsync(errorPath)}");
            }
        }
        finally
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            process?.Dispose();
            File.Delete(harPath);
            File.Delete(warningHarPath);
            File.Delete(largeHarPath);
            File.Delete(sazPath);
            File.Delete(encryptedSazPath);
            File.Delete(telemetrySettingsPath);
            File.Delete(outputPath);
            File.Delete(errorPath);
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string GetPublishedExecutablePath()
    {
        var publishDirectory = Environment.GetEnvironmentVariable(
            "M365_TRACE_PUBLISH_DIR");
        Assert.False(
            string.IsNullOrWhiteSpace(publishDirectory),
            "M365_TRACE_PUBLISH_DIR must identify the tested publish directory.");

        var executablePath = Path.Combine(
            publishDirectory!,
            "M365Trace.Web.exe");
        Assert.True(
            File.Exists(executablePath),
            $"Published executable was not found at '{executablePath}'.");
        return executablePath;
    }

    private static Process StartApplication(
        string executablePath,
        string workingDirectory,
        string arguments,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment[
            DisableBrowserLaunchEnvironmentVariable] = "1";
        if (environment is not null)
        {
            foreach (var item in environment)
            {
                startInfo.Environment[item.Key] = item.Value;
            }
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "The packaged application could not be started.");
    }

    private static async Task StopProcessAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    private static async Task WaitForApplicationAsync(
        string url,
        Process process)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    "The packaged application exited before becoming available.");
            }

            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"The packaged application did not respond at '{url}'.");
    }

    private static async Task WaitForRowCountWithDiagnosticsAsync(
        IPage page,
        int expectedCount)
    {
        try
        {
            await page.WaitForFunctionAsync(
                "expected => document.querySelectorAll('tbody tr[data-session-id]').length === expected",
                expectedCount);
        }
        catch (TimeoutException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected {expectedCount} session rows. "
                + $"Page content: {await page.ContentAsync()}",
                exception);
        }
    }

    private static async Task WaitForLoadedFileAsync(
        IPage page,
        string path)
    {
        var fileName = Path.GetFileName(path);

        try
        {
            await page
                .Locator(".panel-heading h2")
                .Filter(new LocatorFilterOptions { HasText = fileName })
                .WaitForAsync();
        }
        catch (TimeoutException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected '{fileName}' to become the loaded trace. "
                + $"Page content: {await page.ContentAsync()}",
                exception);
        }
    }

    private static async Task WaitForBodyTextAsync(
        IPage page,
        string expectedText)
    {
        try
        {
            await page
                .Locator(".body-content")
                .Filter(new LocatorFilterOptions { HasText = expectedText })
                .WaitForAsync();
        }
        catch (TimeoutException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected body text '{expectedText}'. "
                + $"Detail content: "
                + $"{await page.Locator("[data-session-detail]").InnerTextAsync()}",
                exception);
        }
    }

    private static async Task WaitForSelectedSessionWithDiagnosticsAsync(
        IPage page,
        int sessionId)
    {
        try
        {
            await page
                .Locator($"tbody tr.selected[data-session-id='{sessionId}']")
                .WaitForAsync();
        }
        catch (TimeoutException exception)
        {
            var state = await page.EvaluateAsync<string>(
                """
                () => {
                    const container = document.querySelector(".table-container");
                    const rows = Array.from(document.querySelectorAll(
                        "tbody tr[data-session-row]"));
                    return JSON.stringify({
                        scrollTop: container?.scrollTop,
                        scrollHeight: container?.scrollHeight,
                        clientHeight: container?.clientHeight,
                        firstIndex: rows[0]?.dataset.sessionIndex,
                        lastIndex: rows.at(-1)?.dataset.sessionIndex,
                        selectedId: document.querySelector(
                            "tbody tr.selected")?.dataset.sessionId
                    });
                }
                """);
            throw new Xunit.Sdk.XunitException(
                $"Expected session {sessionId} to be selected. Grid state: {state}",
                exception);
        }
    }

    private static async Task WaitForActiveSessionAsync(
        IPage page,
        int sessionId)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
                sessionId =>
                    document.activeElement?.dataset.sessionId
                    === String(sessionId)
                """,
                sessionId);
        }
        catch (TimeoutException exception)
        {
            var activeElement = await page.EvaluateAsync<string>(
                """
                () => JSON.stringify({
                    tagName: document.activeElement?.tagName,
                    sessionId: document.activeElement?.dataset.sessionId,
                    className: document.activeElement?.className
                })
                """);
            throw new Xunit.Sdk.XunitException(
                $"Expected session {sessionId} to have focus. "
                + $"Active element: {activeElement}",
                exception);
        }
    }

    private static string CreateHar() =>
        """
        {
          "log": {
            "version": "1.2",
            "creator": {
              "name": "M365 Trace Analyzer E2E",
              "version": "1.0"
            },
            "entries": [
              {
                "startedDateTime": "2026-09-23T10:00:00-04:00",
                "time": 100,
                "request": {
                  "method": "GET",
                  "url": "https://outlook.office.com/owa/",
                  "httpVersion": "HTTP/1.1",
                  "headers": [
                    { "name": "Accept", "value": "application/json" },
                    { "name": "request-id", "value": "e2e-request-1" }
                  ]
                },
                "response": {
                  "status": 200,
                  "statusText": "OK",
                  "httpVersion": "HTTP/1.1",
                  "headers": [{ "name": "Content-Type", "value": "application/json" }],
                  "content": {
                    "mimeType": "application/json",
                    "text": "{\"status\":\"ok\"}"
                  }
                }
              },
              {
                "startedDateTime": "2026-09-23T10:00:01-04:00",
                "time": 250,
                "request": {
                  "method": "GET",
                  "url": "https://outlook.office.com/owa/service.svc",
                  "httpVersion": "HTTP/1.1",
                  "headers": []
                },
                "response": {
                  "status": 503,
                  "statusText": "Service Unavailable",
                  "httpVersion": "HTTP/1.1",
                  "headers": [{ "name": "Content-Type", "value": "text/plain" }],
                  "content": {
                    "mimeType": "text/plain",
                    "text": "Service unavailable"
                  }
                }
              }
            ]
          }
        }
        """;

    private static string CreateLargeHar()
    {
        var entries = Enumerable.Range(1, 250)
            .Select(id => new
            {
                startedDateTime =
                    $"2026-09-23T10:{id / 60:00}:{id % 60:00}-04:00",
                time = 50,
                request = new
                {
                    method = "GET",
                    url = $"https://example.test/session/{id}",
                    httpVersion = "HTTP/1.1",
                    headers = Array.Empty<object>()
                },
                response = new
                {
                    status = 200,
                    statusText = "OK",
                    httpVersion = "HTTP/1.1",
                    headers = Array.Empty<object>(),
                    content = new
                    {
                        mimeType = "application/json",
                        text = """{"status":"ok"}"""
                    }
                }
            });

        return JsonSerializer.Serialize(new
        {
            log = new
            {
                version = "1.2",
                creator = new
                {
                    name = "M365 Trace Analyzer E2E",
                    version = "1.0"
                },
                entries
            }
        });
    }

    private static void CreateSaz(string path)
    {
        using var stream = File.Create(path);
        using var archive = new ZipArchive(
            stream,
            ZipArchiveMode.Create);

        AddSazEntry(
            archive,
            "raw/1_c.txt",
            "GET /unencrypted HTTP/1.1\r\n"
            + "Host: example.test\r\n"
            + "\r\n");
        AddSazEntry(
            archive,
            "raw/1_s.txt",
            "HTTP/1.1 200 OK\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n"
            + "\r\n"
            + "saz response body");
        AddSazEntry(
            archive,
            "raw/2_c.txt",
            "GET /failing HTTP/1.1\r\n"
            + "Host: outlook.office.com\r\n"
            + "\r\n");
        AddSazEntry(
            archive,
            "raw/2_s.txt",
            "HTTP/1.1 503 Service Unavailable\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n"
            + "\r\n"
            + "temporary failure");
    }

    private static void CreateEncryptedSaz(string path)
    {
        using var stream = File.Create(path);
        using var archive = new SharpZipOutputStream(stream)
        {
            Password = EncryptedSazPassword
        };

        AddEncryptedSazEntry(
            archive,
            "raw/1_c.txt",
            "GET /encrypted HTTP/1.1\r\n"
            + "Host: example.test\r\n"
            + "\r\n");
        AddEncryptedSazEntry(
            archive,
            "raw/1_s.txt",
            "HTTP/1.1 200 OK\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n"
            + "\r\n"
            + "decrypted browser body");
        archive.Finish();
    }

    private static void AddSazEntry(
        ZipArchive archive,
        string path,
        string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(
            entry.Open(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void AddEncryptedSazEntry(
        SharpZipOutputStream archive,
        string path,
        string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var entry = new SharpZipEntry(path)
        {
            AESKeySize = 256,
            DateTime = new DateTime(2026, 9, 27, 1, 0, 0),
            Size = bytes.Length
        };

        archive.PutNextEntry(entry);
        archive.Write(bytes);
        archive.CloseEntry();
    }

    private static string CreateWarningHar() =>
        """
        {
          "log": {
            "version": "1.2",
            "creator": {
              "name": "M365 Trace Analyzer E2E",
              "version": "1.0"
            },
            "entries": [
              {
                "startedDateTime": "2026-09-25T16:30:00-04:00",
                "time": 125,
                "request": {
                  "method": "GET",
                  "url": "https://outlook.office.com/owa/",
                  "httpVersion": "HTTP/1.1",
                  "headers": []
                },
                "response": {
                  "status": 200,
                  "statusText": "OK",
                  "httpVersion": "HTTP/1.1",
                  "headers": [],
                  "content": {
                    "mimeType": "application/json",
                    "text": "{\"status\":\"ok\"}"
                  }
                }
              },
              {
                "startedDateTime": "2026-09-25T16:30:01-04:00",
                "time": 50,
                "request": {
                  "method": "GET",
                  "url": "https://outlook.office.com/owa/malformed-demo",
                  "httpVersion": "HTTP/1.1",
                  "headers": []
                }
              }
            ]
          }
        }
        """;
}
