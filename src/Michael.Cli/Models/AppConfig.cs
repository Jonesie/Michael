namespace Michael.Cli.Models;

public sealed record AppConfig(FixGenerationConfig Fixes)
{
    public const string DefaultAiCommandTemplate = "copilot -i \"agent --prompt {prompt}\"";

    public static AppConfig Default { get; } = new(new FixGenerationConfig(DefaultAiCommandTemplate));
}

public sealed record FixGenerationConfig(string AiCommandTemplate)
{
    public FixGenerationConfig() : this(AppConfig.DefaultAiCommandTemplate)
    {
    }
}
