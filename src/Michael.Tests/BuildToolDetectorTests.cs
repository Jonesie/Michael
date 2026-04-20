using Michael.Parsing;

namespace Michael.Tests;

public class BuildToolDetectorTests
{
    [Fact]
    public void Detect_FindsDotnetSdkDotnetAndCSharp_FromDotnetBuildLog()
    {
        var log = """
            /usr/lib/dotnet/sdk/10.0.100/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(1479,3): error MSB4019: Missing target [/tmp/sample/App.csproj]
            /tmp/sample/App.cs(15,10): warning CS0168: The variable 'ex' is declared but never used [/tmp/sample/App.csproj]
            """;

        var detector = new BuildToolDetector();

        var detected = detector.Detect(log);

        Assert.Contains(".NET SDK 10.0.100", detected, StringComparer.Ordinal);
        Assert.Contains(".NET", detected, StringComparer.Ordinal);
        Assert.Contains("C#", detected, StringComparer.Ordinal);
    }

    [Fact]
    public void Detect_FindsFrontendFrameworks_WhenPresentInLog()
    {
        var log = """
            Running ng build for @angular/core app
            npm run react-scripts build
            """;

        var detector = new BuildToolDetector();

        var detected = detector.Detect(log);

        Assert.Contains("Angular", detected, StringComparer.Ordinal);
        Assert.Contains("React", detected, StringComparer.Ordinal);
    }

    [Fact]
    public void Detect_ReturnsEmpty_WhenNoKnownSignals()
    {
        var detector = new BuildToolDetector();

        var detected = detector.Detect("Build started... no framework identifiers here.");

        Assert.Empty(detected);
    }
}