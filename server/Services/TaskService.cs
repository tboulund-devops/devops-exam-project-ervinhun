using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;

namespace server.Services;

public class TaskService(MyDbContext ctx) : ITaskService
{
    public async Task<List<TaskDto>> GetTasksByQueryAsync(TaskQueryParameters query)
    {
        var tasksQuery = ctx.TaskItems
            .AsNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.DeletedAt == null);
        
        // Filter by status
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            tasksQuery = tasksQuery.Where(t => t.Status.Name.ToLower() == status.ToLower());
        }

        // Filter by assignee id
        if (query.AssigneeId.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.AssigneeId == query.AssigneeId.Value);
        }
        else if (query.HasAssignee.HasValue)
        {
            tasksQuery = query.HasAssignee.Value
                ? tasksQuery.Where(t => t.AssigneeId != null)
                : tasksQuery.Where(t => t.AssigneeId == null);
        }
        
        // Sort
        var sortBy = query.SortBy?.Trim().ToLower();
        var sortOrder = query.SortOrder?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(sortOrder) && sortOrder is not ("asc" or "desc"))
        {
            throw new ArgumentException("Invalid sortOrder. Use 'asc' or 'desc'.");
        }

        var descending = sortOrder != "asc";

        tasksQuery = sortBy switch
        {
            "updatedat" => descending
                ? tasksQuery.OrderByDescending(t => t.UpdatedAt)
                : tasksQuery.OrderBy(t => t.UpdatedAt),

            "createdat" or null or "" => descending
                ? tasksQuery.OrderByDescending(t => t.CreatedAt)
                : tasksQuery.OrderBy(t => t.CreatedAt),

            _ => throw new ArgumentException("Invalid sortBy. Use 'createdAt' or 'updatedAt'.")
        };

        return await tasksQuery
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                Status = t.Status.Name,
                Assignee = t.Assignee == null
                    ? null
                    : new UserDto
                    {
                        Id = t.Assignee.Id,
                        Username = t.Assignee.Username
                    }
            })
            .ToListAsync();
    }
}