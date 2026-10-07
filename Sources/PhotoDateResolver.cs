using System.Globalization;
using DodosPhotoSaver.Models;
using MediaDevices;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace DodosPhotoSaver.Sources;

/// <summary>
/// Stabilisce se una foto appartiene a un periodo con la stessa logica per iPhone e Android.
/// Aprire un file via MTP è costoso (può scaricarlo per intero), quindi si procede a livelli
/// e si legge l'EXIF solo quando i dati già disponibili non bastano:
///   1. data di creazione (iPhone) oppure data nel nome file → decisione immediata;
///   2. data di modifica precedente al periodo → scartata (una foto non è scattata dopo l'ultima modifica);
///   3. altrimenti EXIF, con la data di modifica come ultimo fallback.
/// </summary>
internal static class PhotoDateResolver
{
    // L'EXIF sta all'inizio del file: non serve leggerlo tutto
    private const int MaxMetadataBytes = 512 * 1024;

    private static readonly DateTime MinValidDate = new(1995, 1, 1);

    private static readonly int[] ExifDateTags =
    {
        ExifDirectoryBase.TagDateTimeOriginal,  // momento dello scatto
        ExifDirectoryBase.TagDateTimeDigitized,
        ExifDirectoryBase.TagDateTime           // ultima modifica del file immagine
    };

    // Formati riconosciuti nei nomi file: IMG_20240315_101500.jpg, photo_2024-03-15 10.15.00.jpg
    private static readonly (int Length, string Format)[] FileNameFormats =
    {
        (8, "yyyyMMdd"),
        (10, "yyyy-MM-dd")
    };

    public static bool IsInPeriod(MediaFileInfo file, Period period)
    {
        // Livello 1: dati già disponibili, nessuna lettura del file
        // (su Android CreationTime è sempre null e viene semplicemente saltata)
        if ((ValidDate(file.CreationTime) ?? DateFromFileName(file.Name)) is DateTime known)
            return period.Contains(known);

        // Livello 2: se è stata modificata prima del periodo, non può esserci scattata dentro
        DateTime? modified = ValidDate(file.LastWriteTime);
        if (modified is DateTime m && m < period.Start) return false;

        // Livello 3: solo i casi rimasti incerti richiedono l'apertura del file
        DateTime? taken = ReadExifDate(file) ?? modified;
        return taken is DateTime t && period.Contains(t);
    }

    private static DateTime? ReadExifDate(MediaFileInfo file)
    {
        try
        {
            using var source = file.OpenRead();
            using var buffer = new MemoryStream();
            CopyUpTo(source, buffer, MaxMetadataBytes);
            buffer.Position = 0;

            var exifDirs = ImageMetadataReader.ReadMetadata(buffer)
                .OfType<ExifDirectoryBase>()
                .ToList();

            foreach (int tag in ExifDateTags)
                foreach (var dir in exifDirs)
                    if (dir.TryGetDateTime(tag, out DateTime value) && ValidDate(value) is DateTime valid)
                        return valid;

            return null;
        }
        catch
        {
            // Formato non supportato, file senza metadati o lettura MTP fallita: si usa il fallback
            return null;
        }
    }

    private static void CopyUpTo(Stream source, Stream destination, int maxBytes)
    {
        var chunk = new byte[64 * 1024];
        int total = 0;
        while (total < maxBytes)
        {
            int read = source.Read(chunk, 0, Math.Min(chunk.Length, maxBytes - total));
            if (read <= 0) break;
            destination.Write(chunk, 0, read);
            total += read;
        }
    }

    /// <summary>Cerca una data (yyyyMMdd o yyyy-MM-dd) nel nome file, senza regex.</summary>
    private static DateTime? DateFromFileName(string fileName)
    {
        ReadOnlySpan<char> name = Path.GetFileNameWithoutExtension(fileName).AsSpan();

        foreach (var (length, format) in FileNameFormats)
        {
            for (int i = 0; i + length <= name.Length; i++)
            {
                // La data non deve far parte di un numero più lungo (es. un ID)
                if (i > 0 && char.IsAsciiDigit(name[i - 1])) continue;
                if (i + length < name.Length && char.IsAsciiDigit(name[i + length])) continue;

                if (DateTime.TryParseExact(name.Slice(i, length), format, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime parsed) && ValidDate(parsed) is DateTime valid)
                    return valid;
            }
        }
        return null;
    }

    private static DateTime? ValidDate(DateTime? date) =>
        date is DateTime d && d >= MinValidDate && d <= DateTime.Now.AddDays(2) ? d : null;
}