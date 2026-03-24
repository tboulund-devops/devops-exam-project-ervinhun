using server.Dto;

namespace server.Services;

public interface ITaskService
{
    Task<List<TaskDto>> GetTasksAsync(GetTasksQuery query);
}