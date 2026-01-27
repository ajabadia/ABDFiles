namespace LetterConfig;

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
        try 
        {
            Application.Run(new Forms.MainForm());
        }
        catch (Exception ex)
        {
            File.WriteAllText("debug_crash.txt", ex.ToString());
            MessageBox.Show(ex.Message, "Error Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }    
}