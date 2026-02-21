using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;

namespace server.Controller;

public class TaskController(MyDbContext ctx) : ControllerBase
{
    [HttpGet(nameof(GetTasks))]
    public async Task<List<TaskDto>> GetTasks()
    {
        var tasks = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
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
        return tasks;
    }

    [HttpGet(nameof(GetTaskById))]
    public async Task<ActionResult<TaskDto>> GetTaskById([FromQuery] string id)
    {
        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.Id == Guid.Parse(id))
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.Name,
                Assignee = t.Assignee == null
                    ? null
                    : new UserDto
                    {
                        Id = t.Assignee.Id,
                        Username = t.Assignee.Username
                    }
            })
            .FirstOrDefaultAsync();
        if (task == null)
        {
            return NotFound();
        }

        return task;
    }

    [HttpPost(nameof(CreateTask))]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var toDoStatusId = await ctx.TodoTaskStatuses.Where(s => s.Name == "To-do").Select(s => s.Id)
            .FirstOrDefaultAsync();
        var newTask = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            AssigneeId = request.AssigneeId,
            StatusId = toDoStatusId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await ctx.TaskItems.AddAsync(newTask);
        await ctx.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTaskById), new { id = newTask.Id }, newTask);
    }
}