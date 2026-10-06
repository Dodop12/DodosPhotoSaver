using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

/// <summary>
/// Telefoni Android in modalità "Trasferimento file" (MTP). Windows li vede come dispositivi portatili:
/// qui si considera Android ogni dispositivo portatile che non sia Apple.
/// </summary>
public class AndroidSource : IPhotoSource
{
    public DeviceInfo? FindDevice() =>
        MtpSession.FindFirst(d => !MtpSession.IsApple(d), DevicePlatform.Android, "Telefono Android");

    public IPhotoSession Open(DeviceInfo device) => MtpSession.Open(device.Id, SkipDirectory);

    /// <summary>
    /// Salta le cartelle nascoste (es. DCIM/.thumbnails, che contiene miniature e non foto vere)
    /// e la cartella "Android" di primo livello (dati e cache delle app).
    /// parentDepth: 0 = radice, 1 = memoria interna/scheda SD.
    /// </summary>
    private static bool SkipDirectory(string name, int parentDepth) =>
        name.StartsWith('.') ||
        (parentDepth == 1 && name.Equals("Android", StringComparison.OrdinalIgnoreCase));
}