namespace DodosPhotoSaver.Services;

public static class FolderNameValidator
{
    private static readonly string[] ReservedNames =
        { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "LPT1", "LPT2", "LPT3" };

    /// <summary>Restituisce un messaggio d'errore, oppure null se tutto è valido.</summary>
    public static string? Validate(string parentDir, string folderName, out string fullPath)
    {
        fullPath = "";

        if (folderName.Length == 0) return "Inserisci un nome per la cartella.";
        if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Il nome della cartella contiene caratteri non validi (\\ / : * ? \" < > |).";
        if (folderName.EndsWith('.')) return "Il nome della cartella non può terminare con un punto.";
        if (ReservedNames.Contains(folderName.ToUpperInvariant()))
            return "Questo nome è riservato da Windows. Scegline un altro.";

        if (!Directory.Exists(parentDir)) return "La cartella di destinazione scelta non esiste.";

        string parentFull = Path.GetFullPath(parentDir).TrimEnd('\\');
        string candidate = Path.GetFullPath(Path.Combine(parentDir, folderName));
        if (!string.Equals(Path.GetDirectoryName(candidate), parentFull, StringComparison.OrdinalIgnoreCase))
            return "Nome cartella non valido.";

        if (!CanWrite(parentDir))
            return "Non hai i permessi per scrivere nella cartella di destinazione.";

        fullPath = candidate;
        return null;
    }

    private static bool CanWrite(string dir)
    {
        try
        {
            string probe = Path.Combine(dir, $".dps_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
