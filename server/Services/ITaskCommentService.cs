using server.Dto;

namespace server.Services;

public interface ITaskCommentService
{
    Task<List<TaskCommentDto>> GetCommentsByTaskIdAsync(Guid taskId);
    Task<TaskCommentDto> CreateCommentAsync(Guid taskId, CreateTaskCommentRequest request);
}