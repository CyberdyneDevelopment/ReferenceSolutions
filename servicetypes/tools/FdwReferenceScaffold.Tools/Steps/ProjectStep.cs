using Spectre.Console;

namespace FdwReferenceScaffold.Tools.Steps;

/// <summary>The project name (<c>-n</c>) and output directory (<c>-o</c>) dotnet new itself needs.</summary>
internal static class ProjectStep
{
    public static void Run(WizardAnswers answers)
    {
        AnsiConsole.Write(ReferenceTheme.Rule("Project"));

        string name;
        do
        {
            name = Prompts.Text("Project name (--name)", answers.Name);
        }
        while (string.IsNullOrWhiteSpace(name));

        answers.Name = name;
        answers.OutputDirectory = Prompts.Text(
            "Output directory (blank = a folder named after the project, in the current directory)",
            answers.OutputDirectory);
    }
}
