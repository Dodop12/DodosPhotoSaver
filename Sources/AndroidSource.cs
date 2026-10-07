using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

/// <summary>
/// Telefoni Android in modalità "Trasferimento file" (MTP).
/// Windows li vede come dispositivi portatili.
///
/// La scansione privilegia le directory che possono realmente contenere
/// fotografie, evitando di attraversare inutilmente grandi quantità di
/// contenuti non fotografici.
/// </summary>
public class AndroidSource : IPhotoSource
{
    // Directory presenti nella memoria condivisa che contengono immagini.
    private static readonly HashSet<string> AllowedRootFolders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DCIM",
            "Pictures"
        };

    // Directory contenute in "Android" che vengono esplorate.
    private static readonly HashSet<string> AllowedAndroidFolders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "media"
        };

    private static readonly string[] NonImageKeywords =
    {
        "audio",
        "voice",
        "recording",
        "podcast",
        "cache",
        "temp",
        "tmp",
        "backup",
        "database",
        "log"
    };

    public DeviceInfo? FindDevice() =>
        MtpSession.FindFirst(
            d => !MtpSession.IsApple(d),
            DevicePlatform.Android,
            "Telefono Android");

    public IPhotoSession Open(
        DeviceInfo device,
        ScanOptions? options = null)
    {
        bool includeOtherMedia =
            options?.IncludeOtherMedia ?? false;

        return MtpSession.Open(
            device.Id,
            (parentName, name, parentDepth) =>
                SkipDirectory(
                    parentName,
                    name,
                    parentDepth,
                    includeOtherMedia));
    }

    /// <summary> Determina se una directory deve essere esclusa dalla scansione. </summary>
    private static bool SkipDirectory(
        string parentName,
        string name,
        int parentDepth,
        bool includeOtherMedia)
    {
        if (name.StartsWith('.')) // Directory nascoste
            return true;

        // Ricerca solo foto scattate
        if (!includeOtherMedia)
        {
            // Memoria interna: si considera esclusivamente DCIM
            if (parentDepth == 1)
            {
                return !name.Equals(
                    "DCIM",
                    StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        // Ricerca altre app: nella memoria interna si considerano 'DCIM', 'Pictures' e 'Android'
        if (parentDepth == 1)
        {
            if (name.Equals(
                    "Android",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return !AllowedRootFolders.Contains(name);
        }

        // All'interno di 'Android' si considera esclusivamente 'media'
        if (parentDepth == 2 &&
            parentName.Equals(
                "Android",
                StringComparison.OrdinalIgnoreCase))
        {
            return !AllowedAndroidFolders.Contains(name);
        }

        // All'interno di 'Android/media' e i suoi sottolivelli si scartano le cartelle non fotografiche
        if (parentDepth >= 3)
        {
            foreach (string keyword in NonImageKeywords)
            {
                if (name.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }
}