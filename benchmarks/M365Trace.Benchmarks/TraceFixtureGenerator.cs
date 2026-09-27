using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip;

namespace M365Trace.Benchmarks;

internal static class TraceFixtureGenerator
{
    private const string EncryptedSazPassword = "benchmark-password";

    public static string GetPassword(BenchmarkFormat format) =>
        format == BenchmarkFormat.EncryptedSaz
            ? EncryptedSazPassword
            : string.Empty;

    public static async Task<string> CreateAsync(
        string directory,
        BenchmarkFormat format,
        int sessionCount,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var extension = format == BenchmarkFormat.Har ? ".har" : ".saz";
        var path = Path.Combine(
            directory,
            $"{GetFormatName(format)}-{sessionCount}{extension}");

        try
        {
            switch (format)
            {
                case BenchmarkFormat.Har:
                    await CreateHarAsync(path, sessionCount, cancellationToken);
                    break;
                case BenchmarkFormat.Saz:
                    CreateSaz(path, sessionCount, cancellationToken);
                    break;
                case BenchmarkFormat.EncryptedSaz:
                    CreateEncryptedSaz(path, sessionCount, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return path;
    }

    public static string GetFormatName(BenchmarkFormat format) =>
        format switch
        {
            BenchmarkFormat.Har => "har",
            BenchmarkFormat.Saz => "saz",
            BenchmarkFormat.EncryptedSaz => "saz-encrypted",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private static async Task CreateHarAsync(
        string path,
        int sessionCount,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();
        writer.WritePropertyName("log");
        writer.WriteStartObject();
        writer.WriteString("version", "1.2");
        writer.WritePropertyName("creator");
        writer.WriteStartObject();
        writer.WriteString("name", "M365Trace.Benchmarks");
        writer.WriteString("version", "1");
        writer.WriteEndObject();
        writer.WritePropertyName("entries");
        writer.WriteStartArray();

        for (var id = 1; id <= sessionCount; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteHarEntry(writer, id);
            if (id % 10_000 == 0)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }

    private static void WriteHarEntry(Utf8JsonWriter writer, int id)
    {
        var statusCode = GetStatusCode(id);
        var host = GetHost(id);

        writer.WriteStartObject();
        writer.WriteString(
            "startedDateTime",
            DateTimeOffset.UnixEpoch.AddMilliseconds(id)
                .ToString("O", CultureInfo.InvariantCulture));
        writer.WriteNumber("time", id % 5_000);

        writer.WritePropertyName("request");
        writer.WriteStartObject();
        writer.WriteString("method", id % 5 == 0 ? "POST" : "GET");
        writer.WriteString(
            "url",
            $"https://{host}/benchmark/{id}?marker=benchmark-marker-{id % 100}");
        writer.WriteString("httpVersion", "HTTP/1.1");
        WriteHeaders(writer, id, request: true);
        writer.WriteEndObject();

        writer.WritePropertyName("response");
        writer.WriteStartObject();
        writer.WriteNumber("status", statusCode);
        writer.WriteString("statusText", GetStatusText(statusCode));
        writer.WriteString("httpVersion", "HTTP/1.1");
        WriteHeaders(writer, id, request: false);
        writer.WritePropertyName("content");
        writer.WriteStartObject();
        writer.WriteNumber("size", 48);
        writer.WriteString("mimeType", "application/json");
        writer.WriteString(
            "text",
            $"{{\"id\":{id},\"marker\":\"benchmark-marker-{id % 100}\"}}");
        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.WritePropertyName("timings");
        writer.WriteStartObject();
        writer.WriteNumber("blocked", 0);
        writer.WriteNumber("dns", 1);
        writer.WriteNumber("connect", 2);
        writer.WriteNumber("ssl", 1);
        writer.WriteNumber("send", 1);
        writer.WriteNumber("wait", id % 4_990);
        writer.WriteNumber("receive", 5);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteHeaders(
        Utf8JsonWriter writer,
        int id,
        bool request)
    {
        writer.WritePropertyName("headers");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WriteString(
            "name",
            request ? "X-Benchmark-Request" : "X-Benchmark-Response");
        writer.WriteString("value", $"benchmark-marker-{id % 100}");
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void CreateSaz(
        string path,
        int sessionCount,
        CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            FileOptions.SequentialScan);
        using var archive = new ZipArchive(
            stream,
            ZipArchiveMode.Create,
            leaveOpen: false);

        for (var id = 1; id <= sessionCount; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddSazSession(archive, id);
        }
    }

    private static void AddSazSession(ZipArchive archive, int id)
    {
        var host = GetHost(id);
        var statusCode = GetStatusCode(id);
        AddEntry(
            archive,
            $"raw/{id}_c.txt",
            $"{(id % 5 == 0 ? "POST" : "GET")} /benchmark/{id}"
            + $"?marker=benchmark-marker-{id % 100} HTTP/1.1\r\n"
            + $"Host: {host}\r\n"
            + $"X-Benchmark-Request: benchmark-marker-{id % 100}\r\n\r\n");
        AddEntry(
            archive,
            $"raw/{id}_s.txt",
            $"HTTP/1.1 {statusCode} {GetStatusText(statusCode)}\r\n"
            + "Content-Type: application/json\r\n"
            + $"X-Benchmark-Response: benchmark-marker-{id % 100}\r\n\r\n"
            + $"{{\"id\":{id},\"marker\":\"benchmark-marker-{id % 100}\"}}");
    }

    private static void AddEntry(
        ZipArchive archive,
        string name,
        string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(
            entry.Open(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void CreateEncryptedSaz(
        string path,
        int sessionCount,
        CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            FileOptions.SequentialScan);
        using var archive = new ZipOutputStream(stream)
        {
            IsStreamOwner = false,
            Password = EncryptedSazPassword
        };

        for (var id = 1; id <= sessionCount; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var host = GetHost(id);
            var statusCode = GetStatusCode(id);
            AddEncryptedEntry(
                archive,
                $"raw/{id}_c.txt",
                $"GET /benchmark/{id} HTTP/1.1\r\n"
                + $"Host: {host}\r\n\r\n");
            AddEncryptedEntry(
                archive,
                $"raw/{id}_s.txt",
                $"HTTP/1.1 {statusCode} {GetStatusText(statusCode)}\r\n"
                + "Content-Type: text/plain\r\n\r\n"
                + $"benchmark-marker-{id % 100}");
        }

        archive.Finish();
    }

    private static void AddEncryptedEntry(
        ZipOutputStream archive,
        string name,
        string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var entry = new ZipEntry(name)
        {
            AESKeySize = 256,
            DateTime = new DateTime(2026, 9, 27, 12, 0, 0),
            Size = bytes.Length
        };

        archive.PutNextEntry(entry);
        archive.Write(bytes);
        archive.CloseEntry();
    }

    private static string GetHost(int id) =>
        (id % 4) switch
        {
            0 => "outlook.office.com",
            1 => "login.microsoftonline.com",
            2 => "graph.microsoft.com",
            _ => "autodiscover-s.outlook.com"
        };

    private static int GetStatusCode(int id) =>
        (id % 20) switch
        {
            0 => 503,
            1 => 401,
            2 => 302,
            _ => 200
        };

    private static string GetStatusText(int statusCode) =>
        statusCode switch
        {
            200 => "OK",
            302 => "Found",
            401 => "Unauthorized",
            503 => "Service Unavailable",
            _ => "Status"
        };
}
