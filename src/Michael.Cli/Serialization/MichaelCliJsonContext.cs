using System.Text.Json.Serialization;
using Michael.Analysis.Models;
using Michael.Cli.Models;

namespace Michael.Cli.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(FixGenerationConfig))]
[JsonSerializable(typeof(IssuesReportPayload))]
[JsonSerializable(typeof(ReportMetadata))]
[JsonSerializable(typeof(RankedIssue))]
internal partial class MichaelCliJsonContext : JsonSerializerContext
{
}
