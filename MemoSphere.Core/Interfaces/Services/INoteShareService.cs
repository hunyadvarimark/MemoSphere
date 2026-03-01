using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface INoteShareService
    {
        // Exportálja az adott jegyzetet a megadott fájlútvonalra
        Task ExportNoteToFileAsync(int noteId, string filePath);

        // Importál egy jegyzetet a fájlból, és beteszi a kiválasztott témakörbe (Topic)
        Task ImportNoteFromFileAsync(string filePath, int targetTopicId);
    }
}