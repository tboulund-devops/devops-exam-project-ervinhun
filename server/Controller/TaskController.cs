using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.DataAccess;
using server.Dto;
using server.Utils;

namespace server.Controller;

[ApiController]
[Route("api/[controller]")]
public class TaskController(MyDbContext ctx) : ControllerBase
{
    [HttpGet(nameof(GetTasks))]
    public async Task<List<TaskDto>> GetTasks()
    {
        var tasks = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.DeletedAt == null)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
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
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest("Invalid task id.");
        }
        var task = await ctx.TaskItems
            .Include(t => t.Assignee)
            .Include(t => t.Status)
            .Where(t => t.Id == taskId && t.DeletedAt == null)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
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
            return NotFound($"Task not found with id: '{id}'");
        }

        return task;
    }

    [HttpPost(nameof(CreateTask))]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var defaultStatus = await ctx.TodoTaskStatuses.Where(s => s.Name == "Backlog")
            .FirstOrDefaultAsync();
        if (defaultStatus == null)
        {
            var backlogStatus = new TodoTaskStatus()
            {
                Name = "Backlog",
                CreatedAt = DateTime.UtcNow,
            };
            await ctx.TodoTaskStatuses.AddAsync(backlogStatus);
            await ctx.SaveChangesAsync();
            defaultStatus = backlogStatus;
        }
        
        User? user = null;
        if (request.AssigneeId != null)
        {
            user = await ctx.Users.Where(u => u.Id == request.AssigneeId).FirstOrDefaultAsync() ?? null;
            if (user == null)
            {
                return NotFound($"Assignee not found with id: '{request.AssigneeId}'");
            }
        }
        

        var newTask = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            AssigneeId = request.AssigneeId,
            StatusId = defaultStatus.Id,
            Status = defaultStatus,
            Assignee = user
        };
        
        //For the history set the uploading user for 'system' at the moment, as we don't have auth yet
        var systemUser = await ctx.Users.FirstOrDefaultAsync(u => u.Username == "system");
        if (systemUser == null)        {
            var addSystemUser = new User
            {
                Username = "system",
                Email = "no.reply@system.com",
                CreatedAt = DateTime.UtcNow,
            };
            await ctx.Users.AddAsync(addSystemUser);
            await ctx.SaveChangesAsync();
            systemUser = addSystemUser;
        }
        
        await ctx.TaskItems.AddAsync(newTask);
        await ctx.SaveChangesAsync();
        var saveHistory = new SaveTaskToHistory(ctx);
        await saveHistory.OnCreate(newTask, systemUser.Id);
        return CreatedAtAction(nameof(GetTaskById), new { id = newTask.Id }, MapToTaskDto(newTask));
    }

    private TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            Status = task.Status.Name,
            Assignee = task.Assignee == null
                ? null
                : new UserDto
                {
                    Id = task.Assignee.Id,
                    Username = task.Assignee.Username
                }
        };
    }
}