namespace DodosPhotoSaver.Views;

/// <summary>Schermata "Collega il tuo telefono al computer".</summary>
public class ConnectView : UserControl
{
    public ConnectView()
    {
        Controls.Add(new Label
        {
            Text = "Collega il tuo telefono al computer",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 14f)
        });
        Controls.Add(new Label
        {
            Text = "iPhone: sblocca e tocca \"Autorizza\".\nAndroid: sblocca e scegli \"Trasferimento file\" (MTP).",
            Dock = DockStyle.Bottom,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        });
    }
}