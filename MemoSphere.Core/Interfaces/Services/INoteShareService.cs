using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface INoteShareService
    {
        // Exportálja az adott jegyzetet a megadott fájlútvonalra
        Task ExportNoteToFileAsync(int noteId, string filePath);

        // Importál egy jegyzetet a fájlból, és beteszi a kiválasztott témakörbe (Topic)
        Task ImportNoteFromFileAsync(string filePath, int targetTopicId);

        // Importál egy témakört a fájlból, és beteszi a kiválasztott tantárgyba (Subject)
        Task ImportTopicFromFileAsync(string filePath, int targetSubjectId);

        // Exportálja az adott témakört a megadott fájlútvonalra
        Task ExportTopicToFileAsync(int topicId, string filePath);

        // Importál egy tantárgyat a fájlból
        Task ImportSubjectFromFileAsync(string filePath);

        // Exportálja az adott tantárgyat a megadott fájlútvonalra
        Task ExportSubjectToFileAsync(int subjectId, string filePath);
    }
}