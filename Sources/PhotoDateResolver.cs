using System.Globalization;
using MediaDevices;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace DodosPhotoSaver.Sources;

/// <summary>
/// Ricava la data di una foto con la stessa logica per iPhone e Android, in ordine di affidabilità:
/// EXIF → data di creazione → data nel nome file → data di modifica.
/// </summary>
internal static class PhotoDateResolver
{
    // L'EXIF sta all'inizio del file: non serve scaricarlo tutto
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

    public static DateTime? Resolve(MediaFileInfo file) =>
        ReadExifDate(file)
        ?? ValidDate(file.CreationTime)        // Android: sempre null, quindi viene saltata
        ?? DateFromFileName(file.Name)         // prima della data di modifica, che cambia con copie/ripristini
        ?? ValidDate(file.LastWriteTime);

    private static DateTime? ReadExifDate(MediaFileInfo file)
    {
        try
        {
            using var source = file.OpenRead();
            using var buffer = new MemoryStream();
            CopyUpTo(source, buffer, MaxMetadataBytes);
            buffer.Position = 0;

            var directories = ImageMetadataReader.ReadMetadata(buffer);
            var exifDirs = directories.OfType<ExifDirectoryBase>().ToList();

            foreach (int tag in ExifDateTags)
                foreach (var dir in exifDirs)
                    if (dir.TryGetDateTime(tag, out DateTime value) && ValidDate(value) is DateTime valid)
                        return valid;

            return null;
        }
        catch
        {
            // Formato non supportato, file senza metadati o lettura MTP fallita: si passa ai fallback
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