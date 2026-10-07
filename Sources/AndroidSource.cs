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

    public IPhotoSession Open(DeviceInfo device, ScanOptions? options = null)
    {
        bool includeOtherMedia = options?.IncludeOtherMedia ?? false;
        return MtpSession.Open(device.Id,
            (parentName, name, parentDepth) => SkipDirectory(parentName, name, parentDepth, includeOtherMedia));
    }

    /// <summary>
    /// parentDepth: 0 = radice, 1 = memoria interna/scheda SD, 2 = cartelle di primo livello (DCIM, Android, ...).
    /// - Sempre: salta le cartelle nascoste (es. DCIM/.thumbnails, che contiene miniature e non foto vere).
    /// - Senza "altri media": scansiona solo DCIM (foto della fotocamera).
    /// - Con "altri media": scansiona tutto (WhatsApp, Telegram, Download...), compresa Android/media,
    ///   ma salta Android/data e Android/obb (dati e cache delle app).
    /// </summary>
    private static bool SkipDirectory(string parentName, string name, int parentDepth, bool includeOtherMedia)
    {
        if (name.StartsWith('.')) return true;

        if (!includeOtherMedia)
            return parentDepth == 1 && !name.Equals("DCIM", StringComparison.OrdinalIgnoreCase);

        return parentDepth == 2 &&
               parentName.Equals("Android", StringComparison.OrdinalIgnoreCase) &&
               (name.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obb", StringComparison.OrdinalIgnoreCase));
    }
}