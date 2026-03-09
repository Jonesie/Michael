# Sample Warning App

This is a tiny .NET console app used to generate predictable compiler warnings for testing the Michael GitHub Action.

## Build and produce a log

```bash
dotnet build samples/sample-warning-app/SampleWarningApp.csproj > samples/sample-warning-app/build.log 2>&1
```

Then pass `samples/sample-warning-app/build.log` to the action as the `input` value.
