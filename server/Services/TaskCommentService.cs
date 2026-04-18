using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;

namespace server.Services;

public class TaskCommentService(MyDbContext ctx) : ITaskCommentService
{
    public async Task<List<TaskCommentDto>> GetCommentsByTaskIdAsync(Guid taskId)
    {
        var taskExist = await ctx.TaskItems
            .AnyAsync(t => t.Id == taskId && t.DeletedAt == null);

        if (!taskExist)
        {
            throw new KeyNotFoundException("Task not found");
        }

        return await ctx.TaskComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TaskCommentDto
            {
                Id = c.Id,
                TaskId = c.TaskId,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                Username = c.User == null ? null : c.User.Username
            })
            .ToListAsync();
    }

    public async Task<TaskCommentDto> CreateCommentAsync(Guid taskId, CreateTaskCommentRequest request)
    {
        var task = await ctx.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId && t.DeletedAt == null);

        if (task == null)
        {
            throw new KeyNotFoundException("Task not found");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Comment content is required.");
        }

        User? user = null;
        if (request.UserId.HasValue)
        {
            user = await ctx.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId.Value && u.DeletedAt == null);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
        }

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = request.UserId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await ctx.TaskComments.AddAsync(comment);
        await ctx.SaveChangesAsync();

        return new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UserId = comment.UserId,
            Username = user?.Username
        };
    }
}