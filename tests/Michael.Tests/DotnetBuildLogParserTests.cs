using Michael.Parsing;

namespace Michael.Tests;

public class DotnetBuildLogParserTests
{
    [Fact]
    public void Parse_ExtractsWarningsAndErrors_WithCountAndNormalizedFields()
    {
        var log = """
            /tmp/app/Foo.csproj : warning NU1902: Package 'RestSharp' 110.2.0 has a known vulnerability [/tmp/app/App.sln]
            /tmp/app/Foo.csproj : warning NU1902: Package 'RestSharp' 110.2.0 has a known vulnerability [/tmp/app/App.sln]
            /tmp/app/Bar.cs(42,13): warning CS8602: Dereference of a possibly null reference. [/tmp/app/Bar.csproj]
            /usr/lib/dotnet/sdk/9.0.114/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(1479,3): error MSB4019: The imported project was not found. [/tmp/app/Bar.csproj]
            """;

        var parser = new DotnetBuildLogParser();

        var issues = parser.Parse(log);

        Assert.Equal(3, issues.Count);

        var nuIssue = Assert.Single(issues, issue => issue.Message.StartsWith("NU1902:", StringComparison.Ordinal));
        Assert.Equal("dotnet", nuIssue.Source);
        Assert.Equal("warning", nuIssue.Severity);
        Assert.Equal("/tmp/app/Foo.csproj", nuIssue.FilePath);
        Assert.Equal(2, nuIssue.Count);

        var csIssue = Assert.Single(issues, issue => issue.Message.StartsWith("CS8602:", StringComparison.Ordinal));
        Assert.Equal("/tmp/app/Bar.cs", csIssue.FilePath);
        Assert.Equal(1, csIssue.Count);

        var msbIssue = Assert.Single(issues, issue => issue.Message.StartsWith("MSB4019:", StringComparison.Ordinal));
        Assert.Equal("error", msbIssue.Severity);
        Assert.Equal(1, msbIssue.Count);
    }

    [Fact]
    public void Parse_WithTextReader_SupportsStreamingInput()
    {
        var parser = new DotnetBuildLogParser();
        using var reader = new StringReader("/tmp/app/Foo.csproj : warning NU1903: High severity vulnerability");

        var issues = parser.Parse(reader);

        var issue = Assert.Single(issues);
        Assert.Equal("NU1903: High severity vulnerability", issue.Message);
        Assert.Equal("warning", issue.Severity);
        Assert.Equal("/tmp/app/Foo.csproj", issue.FilePath);
    }

    [Fact]
    public void Parse_IgnoresNonDiagnosticLines()
    {
        var log = """
              Determining projects to restore...
              Restored /tmp/app/Foo.csproj (in 200 ms).
              Build succeeded.
            """;

        var parser = new DotnetBuildLogParser();

        var issues = parser.Parse(log);

        Assert.Empty(issues);
    }
}
