using DodosPhotoSaver.Sources;
using DodosPhotoSaver.Views;

namespace DodosPhotoSaver;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Una sola copia del programma alla volta: due istanze accederebbero insieme al telefono
        using var mutex = new Mutex(true, @"Local\DodosPhotoSaver.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance) return;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowFatal(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(new AutoDetectSource()));
    }

    private static void ShowFatal(Exception? ex) =>
        MessageBox.Show($"Errore imprevisto:\n\n{ex?.Message ?? "errore sconosciuto"}", "Dodo's Photo Saver",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
}