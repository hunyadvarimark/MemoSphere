using Core.Entities;

namespace MemoSphere.Core.Interfaces
{
    public interface ITopicService
    {
        Task<Topic>GetTopicByIdAsync(int id);
        Task<IEnumerable<Topic>>GetTopicBySubjectIdAsync(int subjectId);
        Task AddTopicAsync(string name, int subjectId);
        Task DeleteTopicAsync(int id);
        Task UpdateTopicAsync(int id, string newName);
    }
}
