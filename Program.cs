namespace FileExplorer;
using Codex;
using static Codex.Incantation;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
		var mainForm = new FileExplorer_Form();
		register_icon(mainForm, "FileExplorer", "FileExplorer");
        Application.Run(mainForm);
    }    
}