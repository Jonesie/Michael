# Michael Analysis Summary

## Metadata

- Generated (UTC): 2026-03-04T09:14:15.9441228+00:00
- Version: 1.0.0+9942b3c8edf4c501267f5cbffaa976cb2f0a9d79
- Input: /home/jonesie/dev/Michael/data/build.log
- Output: /home/jonesie/dev/Michael/out
- Analyse only: True
- Limit: 5
- Git branch: (not set)
- AI tool: (not set)
- AI model: (not set)
- Parsed issues: 107
- Summaries: 107
- Ranked issues: 5

## Ranked Issues

| Rank | Severity | Frequency | Confidence | Score | Key |
|---:|---|---:|---:|---:|---|
| 1 | warning | 28 | 0.98 | 438.00 | /home/jonesie/dev/BARXUI/src/barXui.Api/Controllers/BOSSController.cs::CA2017: Number of parameters supplied in the logging message template do not match the number of named placeholders (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2017) |
  - CA2017 appears 28 time(s). This does not block compilation but should be addressed.
| 2 | warning | 28 | 0.98 | 438.00 | /home/jonesie/dev/BARXUI/src/barXui.Test.Unit/Tables/TableServiceTests.cs::xUnit1048: Support for 'async void' unit tests is being removed from xUnit.net v3. To simplify upgrading, convert the test to 'async Task' instead. (https://xunit.net/xunit.analyzers/rules/xUnit1048) |
  - xUnit1048 appears 28 time(s). This does not block compilation but should be addressed.
| 3 | warning | 22 | 0.98 | 378.00 | /home/jonesie/dev/BARXUI/src/barXui.Test.Unit/Files/DBFileIndexStoreTests.cs::xUnit1048: Support for 'async void' unit tests is being removed from xUnit.net v3. To simplify upgrading, convert the test to 'async Task' instead. (https://xunit.net/xunit.analyzers/rules/xUnit1048) |
  - xUnit1048 appears 22 time(s). This does not block compilation but should be addressed.
| 4 | warning | 16 | 0.98 | 318.00 | /home/jonesie/dev/BARXUI/src/barXui.Test.Integration.Data/SerialNumberStoreTests.cs::xUnit1048: Support for 'async void' unit tests is being removed from xUnit.net v3. To simplify upgrading, convert the test to 'async Task' instead. (https://xunit.net/xunit.analyzers/rules/xUnit1048) |
  - xUnit1048 appears 16 time(s). This does not block compilation but should be addressed.
| 5 | warning | 16 | 0.98 | 318.00 | /home/jonesie/dev/BARXUI/src/barXui.Test.Unit.Sharding/ShardServiceTests.cs::CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread. |
  - CS1998 appears 16 time(s). This does not block compilation but should be addressed.
