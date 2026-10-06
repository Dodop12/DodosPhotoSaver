namespace DodosPhotoSaver.Services;

public static class FolderNameValidator
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>Restituisce un messaggio d'errore, oppure null se tutto è valido.</summary>
    public static string? Validate(string parentDir, string folderName, out string fullPath)
    {
        fullPath = string.Empty;

        if (folderName.Length == 0) return "Inserisci un nome per la cartella.";
        if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Il nome della cartella contiene caratteri non validi (\\ / : * ? \" < > |).";
        if (folderName.EndsWith('.')) return "Il nome della cartella non può terminare con un punto.";
        
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(folderName);
        if (ReservedNames.Contains(nameWithoutExtension))
            return "Questo nome è riservato dal sistema operativo. Scegline un altro.";

        if (!Directory.Exists(parentDir)) return "La cartella di destinazione scelta non esiste.";

        string parentFull = Path.GetFullPath(parentDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string candidate = Path.GetFullPath(Path.Combine(parentDir, folderName));

        string? candidateParent = Path.GetDirectoryName(candidate)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!string.Equals(candidateParent, parentFull, StringComparison.OrdinalIgnoreCase))
            return "Nome cartella non valido o tentato accesso a un percorso non consentito.";

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
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
