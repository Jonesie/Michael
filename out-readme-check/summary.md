# Michael Analysis Summary

## Metadata

- Generated (UTC): 2026-03-04T09:22:06.6058320+00:00
- Version: 1.0.0+f5eefc909373a97df1dcc3d3cd941bf3b5d4634c
- Input: /home/jonesie/dev/Michael/data/sample-dotnet-small.log
- Output: /home/jonesie/dev/Michael/out-readme-check
- Analyse only: True
- Limit: 3
- Git branch: (not set)
- AI tool: (not set)
- AI model: (not set)
- Parsed issues: 3
- Summaries: 3
- Ranked issues: 3

## Ranked Issues

| Rank | Severity | Frequency | Confidence | Score | Key |
|---:|---|---:|---:|---:|---|
| 1 | error | 1 | 0.98 | 208.00 | /usr/lib/dotnet/sdk/9.0.114/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets::MSB4019: The imported project was not found. |
  - MSB4019 appears 1 time(s). This blocks a successful build.
| 2 | warning | 1 | 0.88 | 158.00 | /tmp/sample/App.cs::CS0168: The variable 'ex' is declared but never used |
  - CS0168 appears 1 time(s). This does not block compilation but should be addressed.
| 3 | warning | 1 | 0.88 | 158.00 | /tmp/sample/App.csproj::NU1902: Package 'RestSharp' 110.2.0 has a known moderate severity vulnerability |
  - NU1902 appears 1 time(s). This does not block compilation but should be addressed.
