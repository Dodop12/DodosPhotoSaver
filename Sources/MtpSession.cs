using System.Globalization;
using System.Text.RegularExpressions;
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
    private readonly bool _useModifiedDateOnly;

    private MtpSession(MediaDevice device, Func<string, int, bool>? skipDirectory, bool useModifiedDateOnly)
    {
        _device = device;
        _skipDirectory = skipDirectory;
        _useModifiedDateOnly = useModifiedDateOnly;
    }

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

    /// <param name="skipDirectory">Riceve (nome sottocartella, profondità della cartella genitore); true = salta.</param>
    /// <param name="useModifiedDateOnly">Android non espone la data di creazione: si usa solo quella di modifica.</param>
    internal static MtpSession Open(string deviceId, Func<string, int, bool>? skipDirectory = null,
            bool useModifiedDateOnly = false)
    {
        var all = MediaDevice.GetDevices().ToList();
        var dev = all.FirstOrDefault(d => d.DeviceId == deviceId);
        foreach (var other in all.Where(d => d != dev)) other.Dispose();

        if (dev == null) throw new InvalidOperationException("Telefono non trovato. Controlla il collegamento.");

        try
        {
            dev.Connect();
            return new MtpSession(dev, skipDirectory, useModifiedDateOnly);
        }
        catch
        {
            dev.Dispose();
            throw;
        }
    }

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
            string ext = Path.GetExtension(file.Name);
            bool isImage = AllowedExtensions.Contains(ext);
            DateTime? date = isImage ? ResolveDate(file) : null;

            if (isImage && date is DateTime d && period.Contains(d))
                result.Add(new PhotoItem(file.FullName, file.Name, file.Length));
        }

        foreach (var sub in dir.EnumerateDirectories())
        {
            if (_skipDirectory?.Invoke(sub.Name, depth) == true) continue;
            Walk(sub, period, result, depth + 1, ct);
        }
    }

    private DateTime? ResolveDate(MediaFileInfo file)
    {
        if (_useModifiedDateOnly) // Android: CreationTime è sempre null, si evita anche di leggerla
            return ValidDate(file.LastWriteTime) ?? DateFromFileName(file.Name);
 
        return ValidDate(file.CreationTime) ?? ValidDate(file.LastWriteTime) ?? DateFromFileName(file.Name);
    }

    private static DateTime? ValidDate(DateTime? date) =>
        date is DateTime d && d.Year >= 1995 && d <= DateTime.Now.AddDays(2) ? d : null;

    private static readonly Regex FileNameDate =
        new(@"(?<!\d)((?:19|20)\d{2})(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])(?!\d)", RegexOptions.Compiled);
 
    private static DateTime? DateFromFileName(string name)
    {
        var m = FileNameDate.Match(name);
        return m.Success &&
               DateTime.TryParseExact(m.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? ValidDate(d) : null;
    }

    public void CopyTo(PhotoItem photo, string destFilePath)
        => _device.DownloadFile(photo.DevicePath, destFilePath);

    public Task CopyToAsync(PhotoItem photo, string destFilePath, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // Controlla l'annullamento prima di avviare il trasferimento del singolo file
            ct.ThrowIfCancellationRequested();

            _device.DownloadFile(photo.DevicePath, destFilePath);
        }, ct);
    }

    public void Dispose() => _device.Dispose();
}