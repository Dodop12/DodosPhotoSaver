namespace DodosPhotoSaver.Models;

public enum DevicePlatform { IOS, Android }

public record DeviceInfo(string Id, string Name, DevicePlatform Platform);
public record DownloadResult(int Copied, List<string> Errors);
public record PhotoItem(string DevicePath, string Name, ulong Size);

/// <summary>Opzioni di ricerca. IncludeOtherMedia ha effetto solo su Android (WhatsApp, Telegram, ecc.).</summary>
public record ScanOptions(bool IncludeOtherMedia = false, bool IncludeVideos = false);

public record Period(int Year, int? Month = null)
{
    public static readonly string[] MonthNames =
    {
        "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno",
        "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre"
    };

    // Es. "2026" oppure "Marzo 2026"
    public string Label => Month is >= 1 and <= 12 ? $"{MonthNames[Month.Value - 1]} {Year}" : $"{Year}";

    // Es. "Foto 2026" oppure "Foto Marzo 2026"
    public string DefaultFolderName => $"Foto {Label}";

    // Primo giorno del periodo (serve a scartare in fretta i file troppo vecchi)
    public DateTime Start => new(Year, Month is >= 1 and <= 12 ? Month.Value : 1, 1);

    public bool Contains(DateTime date) => date.Year == Year && (Month is null || date.Month == Month);
}