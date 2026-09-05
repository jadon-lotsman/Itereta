using Mnemo.Data.Entities;

namespace Mnemo.Services.RepetitionService.Strategies
{
    public interface IRepetitionTaskStrategy
    {
        Task<List<RepetitionTask>> GetTasksAsync(int userId);
    }
}
