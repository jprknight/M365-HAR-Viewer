using System.Globalization;

namespace M365Trace.Web.Services;

public sealed record LocalServerOptions(
    int Port,
    string[] RemainingArguments)
{
    public const int DefaultPort = 8080;

    public static LocalServerOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var port = DefaultPort;
        var portSpecified = false;
        var remainingArguments = new List<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (string.Equals(
                    argument,
                    "--urls",
                    StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith(
                    "--urls=",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "The --urls option is not supported. Use --port to "
                    + "select a port; M365 Trace Analyzer always listens "
                    + "on the local computer only.");
            }

            string? portValue = null;
            if (string.Equals(
                    argument,
                    "--port",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= args.Length)
                {
                    throw new ArgumentException(
                        "The --port option requires a port number.");
                }

                portValue = args[index];
            }
            else if (argument.StartsWith(
                         "--port=",
                         StringComparison.OrdinalIgnoreCase))
            {
                portValue = argument["--port=".Length..];
            }
            else
            {
                remainingArguments.Add(argument);
            }

            if (portValue is null)
            {
                continue;
            }

            if (portSpecified)
            {
                throw new ArgumentException(
                    "The --port option may be specified only once.");
            }

            if (!int.TryParse(
                    portValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out port)
                || port is < 1 or > 65535)
            {
                throw new ArgumentException(
                    "The --port value must be an integer from 1 through 65535.");
            }

            portSpecified = true;
        }

        return new LocalServerOptions(
            port,
            remainingArguments.ToArray());
    }
}
