using M365Trace.Web.Services;

namespace M365Trace.Web.Tests;

public sealed class LocalServerOptionsTests
{
    [Fact]
    public void Parse_UsesDefaultPort()
    {
        var result = LocalServerOptions.Parse([]);

        Assert.Equal(8080, result.Port);
        Assert.Empty(result.RemainingArguments);
    }

    [Theory]
    [InlineData("--port", "9090")]
    [InlineData("--PORT", "9090")]
    [InlineData("--port=9090", null)]
    public void Parse_AcceptsPortOption(string argument, string? value)
    {
        var args = value is null
            ? new[] { argument }
            : new[] { argument, value };

        var result = LocalServerOptions.Parse(args);

        Assert.Equal(9090, result.Port);
        Assert.Empty(result.RemainingArguments);
    }

    [Fact]
    public void Parse_PreservesUnrelatedArguments()
    {
        var result = LocalServerOptions.Parse(
            ["--environment", "Development", "--port", "9090"]);

        Assert.Equal(9090, result.Port);
        Assert.Equal(
            ["--environment", "Development"],
            result.RemainingArguments);
    }

    [Theory]
    [InlineData("--urls", "http://0.0.0.0:8080")]
    [InlineData("--urls=http://0.0.0.0:8080", null)]
    [InlineData("--URLS", "http://localhost:8080")]
    public void Parse_RejectsUrlsOption(string argument, string? value)
    {
        var args = value is null
            ? new[] { argument }
            : new[] { argument, value };

        var exception = Assert.Throws<ArgumentException>(
            () => LocalServerOptions.Parse(args));

        Assert.Contains("--urls", exception.Message);
        Assert.Contains("--port", exception.Message);
    }

    [Theory]
    [InlineData("--port")]
    [InlineData("--port=")]
    [InlineData("--port=0")]
    [InlineData("--port=65536")]
    [InlineData("--port=abc")]
    public void Parse_RejectsInvalidPort(string argument)
    {
        Assert.Throws<ArgumentException>(
            () => LocalServerOptions.Parse([argument]));
    }

    [Fact]
    public void Parse_RejectsDuplicatePort()
    {
        Assert.Throws<ArgumentException>(
            () => LocalServerOptions.Parse(
                ["--port", "8080", "--port=9090"]));
    }
}
