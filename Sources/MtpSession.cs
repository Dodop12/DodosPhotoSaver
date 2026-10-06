using DodosPhotoSaver.Models;
using MediaDevices;

namespace DodosPhotoSaver.Sources;

/// <summary>
/// Sessione su un telefono che Windows vede come dispositivo MTP/PTP (sia iPhone sia Android).
/// La logica di ricerca e copia è condivisa: cambia solo come si riconosce il telefono
/// e quali cartelle saltare.
/// </summary>
public sealed class MtpSession : IPhotoSession
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".heic" };

    private const int MaxDepth = 12; // protezione da strutture di cartelle anomale

    private readonly MediaDevice _device;
    private readonly Func<string, int, bool>? _skipDirectory;

    private MtpSession(MediaDevice device, Func<string, int, bool>? skipDirectory)
    {
        _device = device;
        _skipDirectory = skipDirectory;
    }

    // ---------- Rilevamento ----------

    internal static bool IsApple(MediaDevice d) =>
        (d.Manufacturer ?? "").Contains("Apple", StringComparison.OrdinalIgnoreCase) ||
        (d.Description ?? "").Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
        (d.Description ?? "").Contains("iPad", StringComparison.OrdinalIgnoreCase);

    internal static DeviceInfo? FindFirst(Func<MediaDevice, bool> match, DevicePlatform platform, string fallbackName)
    {
        var devices = new List<MediaDevice>();
        try
        {
            devices.AddRange(MediaDevice.GetDevices());
            foreach (var d in devices)
            {
                if (!match(d)) continue;

                string name =
                    !string.IsNullOrWhiteSpace(d.FriendlyName) ? d.FriendlyName :
                    !string.IsNullOrWhiteSpace(d.Description) ? d.Description : fallbackName;
                return new DeviceInfo(d.DeviceId, name, platform);
            }
            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            foreach (var d in devices) d.Dispose();
        }
    }

    // ---------- Apertura ----------

    /// <param name="skipDirectory">Riceve (nome sottocartella, profondità della cartella genitore); true = salta.</param>
    internal static MtpSession Open(string deviceId, Func<string, int, bool>? skipDirectory = null)
    {
        var all = MediaDevice.GetDevices().ToList();
        var dev = all.FirstOrDefault(d => d.DeviceId == deviceId);
        foreach (var other in all.Where(d => d != dev)) other.Dispose();

        if (dev == null) throw new InvalidOperationException("Telefono non trovato. Controlla il collegamento.");

        try
        {
            dev.Connect();
            return new MtpSession(dev, skipDirectory);
        }
        catch
        {
            dev.Dispose();
            throw;
        }
    }

    // ---------- IPhotoSession ----------

    public List<PhotoItem> Scan(Period period, CancellationToken ct)
    {
        var result = new List<PhotoItem>();
        Walk(_device.GetRootDirectory(), period, result, 0, ct);
        return result;
    }

    private void Walk(MediaDirectoryInfo dir, Period period, List<PhotoItem> result, int depth, CancellationToken ct)
    {
        if (depth > MaxDepth) return;
        ct.ThrowIfCancellationRequested();

        foreach (var file in dir.EnumerateFiles())
        {
            if (!AllowedExtensions.Contains(Path.GetExtension(file.Name))) continue;

            DateTime? date = file.CreationTime ?? file.LastWriteTime;
            if (date is DateTime d && period.Contains(d))
                result.Add(new PhotoItem(file.FullName, file.Name, file.Length));
        }

        foreach (var sub in dir.EnumerateDirectories())
        {
            if (_skipDirectory?.Invoke(sub.Name, depth) == true) continue;
            Walk(sub, period, result, depth + 1, ct);
        }
    }

    public void CopyTo(PhotoItem photo, string destFilePath)
        => _device.DownloadFile(photo.DevicePath, destFilePath);

    public void Dispose() => _device.Dispose();
}