namespace DodosPhotoSaver.Views;

/// <summary>Schermata "Collega il tuo iPhone al computer".</summary>
public class ConnectView : UserControl
{
    public ConnectView()
    {
        Controls.Add(new Label
        {
            Text = "Collega il tuo iPhone al computer",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 14f)
        });
        Controls.Add(new Label
        {
            Text = "Sblocca l'iPhone e tocca \"Autorizza\" se richiesto.",
            Dock = DockStyle.Bottom,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray
        });
    }
}
