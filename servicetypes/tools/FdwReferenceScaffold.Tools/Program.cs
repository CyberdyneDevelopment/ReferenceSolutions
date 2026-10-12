using System;
using System.Linq;
using FdwReferenceScaffold.Tools;
using Spectre.Console;

if (args.Any(a => a is "-h" or "--help" or "help"))
{
    HelpText.Show();
    return 0;
}

try
{
    return await MainMenu.Run();
}
catch (NotSupportedException)
{
    Console.Error.WriteLine("fdw-ref-scaffold needs an interactive terminal (arrow-key menus) — run it directly in a console, not piped or redirected.");
    return 1;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}
