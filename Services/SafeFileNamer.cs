namespace DodosPhotoSaver.Services;

/// <summary>Decide il percorso di destinazione di ogni file, restando sempre dentro la cartella scelta.</summary>
public class SafeFileNamer
{
    private readonly string _root;
    private readonly string _rootWithSeparator;
    private readonly HashSet<string> _usedNames = new(StringComparer.OrdinalIgnoreCase);

    public SafeFileNamer(string destDir)
    {
        _root = Path.GetFullPath(destDir);
        _rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public string GetTargetPath(string deviceFileName)
    {
        // Si usa solo il nome del file, mai percorsi provenienti dal dispositivo
        string safeName = Path.GetFileName(deviceFileName);
        if (string.IsNullOrWhiteSpace(safeName) || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException("Nome file non valido.");

        // Due foto con lo stesso nome in cartelle diverse dell'iPhone: non sovrascrivere l'una con l'altra
        string finalName = safeName;
        int n = 2;
        while (!_usedNames.Add(finalName))
            finalName = $"{Path.GetFileNameWithoutExtension(safeName)}_{n++}{Path.GetExtension(safeName)}";

        string target = Path.GetFullPath(Path.Combine(_root, finalName));
        if (!target.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Percorso di destinazione non valido.");

        return target;
    }
}
