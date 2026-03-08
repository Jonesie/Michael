using System.Text.RegularExpressions;

namespace Michael.Parsing;

public sealed partial class BuildToolDetector
{
    public IReadOnlyList<string> Detect(string logContent)
    {
        var content = logContent ?? string.Empty;
        var detected = new List<string>();

        var sdkVersion = DetectDotnetSdkVersion(content);
        if (!string.IsNullOrWhiteSpace(sdkVersion))
        {
            detected.Add($".NET SDK {sdkVersion}");
        }

        if (ContainsDotnet(content))
        {
            detected.Add(".NET");
        }

        if (ContainsCSharp(content))
        {
            detected.Add("C#");
        }

        if (ContainsAngular(content))
        {
            detected.Add("Angular");
        }

        if (ContainsReact(content))
        {
            detected.Add("React");
        }

        return detected;
    }

    private static bool ContainsDotnet(string content)
    {
        return content.Contains("dotnet", StringComparison.OrdinalIgnoreCase)
            || content.Contains("microsoft.net.sdk", StringComparison.OrdinalIgnoreCase)
            || content.Contains(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsCSharp(string content)
    {
        return CSharpDiagnosticRegex().IsMatch(content)
            || content.Contains("c#", StringComparison.OrdinalIgnoreCase)
            || content.Contains(".cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAngular(string content)
    {
        return content.Contains("@angular", StringComparison.OrdinalIgnoreCase)
            || content.Contains("angular cli", StringComparison.OrdinalIgnoreCase)
            || content.Contains("ng build", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsReact(string content)
    {
        return content.Contains("react", StringComparison.OrdinalIgnoreCase)
            || content.Contains("react-scripts", StringComparison.OrdinalIgnoreCase)
            || content.Contains("jsx", StringComparison.OrdinalIgnoreCase)
            || content.Contains("tsx", StringComparison.OrdinalIgnoreCase);
    }

    private static string? DetectDotnetSdkVersion(string content)
    {
        var sdkPathMatch = DotnetSdkPathRegex().Match(content);
        if (sdkPathMatch.Success)
        {
            return sdkPathMatch.Groups["version"].Value;
        }

        var sdkVersionLineMatch = DotnetSdkVersionLineRegex().Match(content);
        if (sdkVersionLineMatch.Success)
        {
            return sdkVersionLineMatch.Groups["version"].Value;
        }

        return null;
    }

    [GeneratedRegex(@"\bCS\d{4,5}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CSharpDiagnosticRegex();

    [GeneratedRegex(@"(?:^|[/\\])dotnet[/\\]sdk[/\\](?<version>\d+\.\d+\.\d+)(?:[/\\]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DotnetSdkPathRegex();

    [GeneratedRegex(@"\.NET\s+SDK\s*(?:Version)?\s*[:=]?\s*(?<version>\d+\.\d+\.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DotnetSdkVersionLineRegex();
}