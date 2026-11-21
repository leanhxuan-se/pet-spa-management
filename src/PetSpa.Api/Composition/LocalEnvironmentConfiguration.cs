using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;

namespace PetSpa.Api.Composition;

internal static class LocalEnvironmentConfiguration
{
    public static void AddLocalEnvironmentFile(this ConfigurationManager configuration, string contentRoot)
    {
        const string fileName = ".env.local";
        var path = Path.Combine(contentRoot, fileName);
        if (!File.Exists(path))
            path = Path.Combine(contentRoot, "src", "PetSpa.Api", fileName);
        if (!File.Exists(path))
            return;

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator < 0 || string.IsNullOrWhiteSpace(line[..separator]))
                throw new FormatException($"Expected NAME=value in {fileName} at line {lineNumber}.");

            var key = line[..separator].Trim().Replace("__", ConfigurationPath.KeyDelimiter);
            values[key] = line[(separator + 1)..];
        }

        // Keep process environment variables and command-line arguments above the local file.
        var environmentIndex = configuration.Sources.ToList().FindIndex(source =>
            source is EnvironmentVariablesConfigurationSource { Prefix: null or "" });
        configuration.Sources.Insert(environmentIndex,
            new MemoryConfigurationSource { InitialData = values });
    }
}
