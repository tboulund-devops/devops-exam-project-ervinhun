using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;

namespace server.Utils;

public class SaveTaskToHistory(MyDbContext ctx)
{
    public async Task OnCreate(TaskItem task, Guid createdBy)
    {
        await UserIdCheck(createdBy);

        var history = new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = null,
            ToStatusId = task.StatusId,
            ChangedBy = createdBy,
        };
        await ctx.TaskHistories.AddAsync(history);
        await ctx.SaveChangesAsync();
    }

    public async Task OnUpdate(TaskItem oldTask, UpdateTaskRequest updateRequest, Guid updatedBy)
    {
        await UserIdCheck(updatedBy);
        
        var timeNowUtc = DateTime.UtcNow;

        var newTitle = updateRequest.Title.Trim();
        var oldTitleNormalized = oldTask.Title.Trim();
        var oldTaskDescription = oldTask.Description?.Trim();
        var newDescription = updateRequest.Description?.Trim();
        
        if (!oldTitleNormalized.Equals(newTitle, StringComparison.Ordinal))
        {
            var entry = new TaskDetailHistory()
            {
                TaskId = oldTask.Id,
                FieldName = "Title",
                OldValue = oldTask.Title,
                NewValue = newTitle,
                ChangedBy = updatedBy,
                ChangedAt = timeNowUtc
            };
            ctx.TaskDetailHistories.Add(entry);
        }

        if (oldTaskDescription != newDescription)
        {
            var entry = new TaskDetailHistory()
            {
                TaskId = oldTask.Id,
                FieldName = "Description",
                OldValue = oldTask.Description,
                NewValue = newDescription,
                ChangedBy = updatedBy,
                ChangedAt = timeNowUtc
            };
            ctx.TaskDetailHistories.Add(entry);
        }

        if (oldTask.AssigneeId != updateRequest.AssigneeId)
        {
            if (updateRequest.AssigneeId != null)
            {
                await UserIdCheck(updateRequest.AssigneeId.Value);
            }
            var entry = new TaskDetailHistory()
            {
                TaskId = oldTask.Id,
                FieldName = "AssigneeId",
                OldValue = oldTask.AssigneeId?.ToString(),
                NewValue = updateRequest.AssigneeId?.ToString(),
                ChangedBy = updatedBy,
                ChangedAt = timeNowUtc
            };
            ctx.TaskDetailHistories.Add(entry);
        }
        
        await ctx.SaveChangesAsync();
    }
    
    
    public async Task OnStatusChange(TaskItem task, Guid fromStatusId, Guid toStatusId, Guid changedBy)
    {
        await UserIdCheck(changedBy);

        var entry = new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedBy = changedBy
        };

        ctx.TaskHistories.Add(entry);
        await ctx.SaveChangesAsync();
    }

    private async Task UserIdCheck(Guid userId)
    {
        var userExists = await ctx.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new KeyNotFoundException("User not found.");
        }
    }

    public async Task OnDelete(Guid taskId, Guid systemUserId, DateTime deletedAt)
    {
        await UserIdCheck(systemUserId);

        var entry = new TaskDetailHistory
        {
            TaskId = taskId,
            FieldName = "DeletedAt",
            OldValue = null,
            NewValue = deletedAt.ToString("o"),
            ChangedBy = systemUserId,
            ChangedAt = DateTime.UtcNow
        };
        ctx.TaskDetailHistories.Add(entry);
        await ctx.SaveChangesAsync();
    }
}