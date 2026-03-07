namespace Michael.Cli.Models;

public sealed record AppConfig(FixGenerationConfig Fixes)
{
    public const string DefaultFixScriptTemplateFile = "templates/fix-script.ps1.template";

    public static AppConfig Default { get; } = new(new FixGenerationConfig(DefaultFixScriptTemplateFile));
}

public sealed record FixGenerationConfig(string ScriptTemplateFile)
{
    public FixGenerationConfig() : this(AppConfig.DefaultFixScriptTemplateFile)
    {
    }
}
