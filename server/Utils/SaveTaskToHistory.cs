using Microsoft.EntityFrameworkCore;
using server.DataAccess;

namespace server.Utils;

public class SaveTaskToHistory(MyDbContext ctx)
{
    public async Task OnCreate(TaskItem task, Guid createdBy)
    {
        var userIdCheck = await ctx.Users.Where(u => u.Id == createdBy)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (userIdCheck == Guid.Empty)
        {
            throw new KeyNotFoundException("User not found.");
        }

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
    
    
    public async Task OnStatusChange(TaskItem task, Guid fromStatusId, Guid toStatusId, Guid changedBy)
    {
        var userIdCheck = await ctx.Users.Where(u => u.Id == changedBy)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (userIdCheck == Guid.Empty)
        {
            throw new KeyNotFoundException("User not found.");
        }

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

}