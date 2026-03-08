using server.DataAccess;

namespace server.Utils;

public class SaveTaskToHistory(MyDbContext ctx)
{
    public async Task OnCreate(TaskItem task, Guid createdBy)
    {
        var userIdCheck = ctx.Users.Where(u => u.Id == createdBy)
            .Select(u => u.Id)
            .FirstOrDefault();
        if (userIdCheck == Guid.Empty)
        {
            throw new Exception("User not found.");
        }

        var history = new TaskHistory
        {
            TaskId = task.Id,
            FromStatusId = null,
            ToStatusId = task.StatusId,
            ChangedBy = createdBy,
        };
        await ctx.TaskHistories.AddAsync(history);
        ctx.SaveChanges();
    }
    
    
    public async Task OnStatusChange(TaskItem task, Guid fromStatusId, Guid toStatusId, Guid changedBy)
    {
        var userIdCheck = ctx.Users.Where(u => u.Id == changedBy)
            .Select(u => u.Id)
            .FirstOrDefault();
        if (userIdCheck == Guid.Empty)
        {
            throw new Exception("User not found.");
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