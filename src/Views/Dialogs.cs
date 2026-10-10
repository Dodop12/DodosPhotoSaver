using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Views;

/// <summary>Tutti i messaggi all'utente in un solo posto.</summary>
public static class Dialogs
{
    public static void Warning(IWin32Window owner, string text) =>
        Show(owner, "Controlla i dati", text, MessageBoxIcon.Warning);

    public static void Error(IWin32Window owner, string details) =>
        Show(owner, "Errore",
            $"Si è verificato un errore:\n\n{details}\n\n" +
            "Se il problema persiste, controlla che il telefono sia collegato e sbloccato.",
            MessageBoxIcon.Error);

    public static void NoPhotos(IWin32Window owner, Period period) =>
        Show(owner, "Nessun file trovato", $"Nessun file trovato per: {period.Label}.", MessageBoxIcon.Information);

    public static void NotEnoughSpace(IWin32Window owner) =>
        Show(owner, "Spazio insufficiente", "Spazio insufficiente sul disco di destinazione.", MessageBoxIcon.Error);

    public static void SpaceCheckFailed(IWin32Window owner) =>
        Show(owner, "Verifica spazio non riuscita",
            "Impossibile verificare lo spazio disponibile sul disco di destinazione.\n\n" +
            "Controlla che la cartella sia accessibile e riprova.",
            MessageBoxIcon.Error);

    public static void Cancelled(IWin32Window owner) =>
        Show(owner, "Annullato", "Operazione annullata.", MessageBoxIcon.Information);


    public static bool ConfirmDownload(IWin32Window owner, int count, Period period, string folderName, string parentDir) =>
        Ask(owner, "Conferma",
            $"Trovati {count} file ({period.Label}).\n\n" +
            $"Nome cartella: {folderName}\nPosizione: {parentDir}\n\nVuoi procedere?");
 
    public static bool ConfirmOverwrite(IWin32Window owner, string folderName, string parentDir) =>
        Ask(owner, "Cartella già esistente",
             $"La cartella \"{folderName}\" esiste già in:\n{parentDir}\n\n" +
            "I file con lo stesso nome verranno SOSTITUITI.\nVuoi continuare?",
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
 
    public static bool ConfirmExit(IWin32Window owner) =>
        Ask(owner, "Copia in corso",
            "Una copia è in corso. Vuoi interromperla e chiudere il programma?",
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

    public static void Completed(IWin32Window owner, DownloadResult result, int total, string fullPath)
    {
        string msg = $"Copiati {result.Copied} file su {total}.\nCartella: {fullPath}";
        if (result.Errors.Count > 0)
            msg += $"\n\n{result.Errors.Count} errori, ad esempio:\n" + string.Join("\n", result.Errors.Take(5));

        MessageBox.Show(owner, msg, "Completato", MessageBoxButtons.OK,
            result.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }


    private static void Show(IWin32Window owner, string title, string text, MessageBoxIcon icon) =>
        MessageBox.Show(owner, text, title, MessageBoxButtons.OK, icon);

    private static bool Ask(IWin32Window owner, string title, string text,
            MessageBoxIcon icon = MessageBoxIcon.Question,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1) =>
        MessageBox.Show(owner, text, title, MessageBoxButtons.YesNo, icon, defaultButton) == DialogResult.Yes;
}