using DodosPhotoSaver.Views;

namespace DodosPhotoSaver;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
